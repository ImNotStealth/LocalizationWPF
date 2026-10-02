using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using Microsoft.Win32;
namespace LocalizationWPF
{
    class FileEntry : INotifyPropertyChanged
    {
        private string _output;
        public string FilePath { get; set; }
        public string Output
        {
            get => _output;
            set
            {
                _output = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Output)));
            }
        }

        public Dictionary<string, string> Custom { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public FileEntry(string filePath, string output)
        {
            FilePath = filePath;
            _output = output;
        }
    }



    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            _table.Columns.Add("ID", typeof(string));
            _table.Columns.Add("English", typeof(string));
            _table.Columns.Add("French", typeof(string));
            _table.Columns.Add("Spanish", typeof(string));

            EnsureTrailingEmptyRow();
            RefreshGrid();
        }
        private readonly DataTable _table = new();
        private DataGridColumn? RightClickedColumn;

        private void EnsureTrailingEmptyRow()
        {
            // Supprime les lignes vides qui ne sont pas la dernière
            for (int i = _table.Rows.Count - 2; i >= 0; i--)
            {
                if (IsRowEmpty(_table.Rows[i]))
                    _table.Rows.RemoveAt(i);
            }

            // Ajoute une ligne vierge si la dernière contient quelque chose (ou s'il n'y a aucune ligne)
            if (_table.Rows.Count == 0 || !IsRowEmpty(_table.Rows[_table.Rows.Count - 1]))
                _table.Rows.Add(_table.NewRow());
        }

        private void RefreshGrid()
        {
            LanguageGrid.ItemsSource = null;
            LanguageGrid.ItemsSource = _table.DefaultView;
            RightClickedColumn = null;
        }

        private void LanguageGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // On attend que la valeur soit écrite dans la source avant de toucher aux lignes
            Dispatcher.BeginInvoke(new Action(EnsureTrailingEmptyRow), DispatcherPriority.Background);
        }
        private static bool IsRowEmpty(DataRow row)
        {
            foreach (var item in row.ItemArray)
            {
                if (item != null && item != DBNull.Value && !string.IsNullOrWhiteSpace(item.ToString()))
                    return false;
            }
            return true;
        }
        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var d = e.OriginalSource as DependencyObject;
            while (d != null && d is not DataGridColumnHeader)
                d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);

            RightClickedColumn = (d as DataGridColumnHeader)?.Column;
        }

        private void AddColumnClick(object sender, RoutedEventArgs e)
        {
            int n = _table.Columns.Count + 1;
            string name = $"Column{n}";
            while (_table.Columns.Contains(name))
                name = $"Column{++n}";

            _table.Columns.Add(name, typeof(string));
            RefreshGrid();
        }
        private void RenameColumnClick(object sender, RoutedEventArgs e)
        {
            if (RightClickedColumn == null)
                return;

            string oldName = RightClickedColumn.Header?.ToString() ?? "";
            if (!_table.Columns.Contains(oldName))
                return;

            string? newName = Prompt("Rename column", oldName);
            if (string.IsNullOrWhiteSpace(newName) || _table.Columns.Contains(newName))
                return;

            _table.Columns[oldName]!.ColumnName = newName;
            RefreshGrid();
        }

        private void DeleteColumnClick(object sender, RoutedEventArgs e)
        {
            if (RightClickedColumn == null)
                return;

            string name = RightClickedColumn.Header?.ToString() ?? "";
            if (!_table.Columns.Contains(name))
                return;

            _table.Columns.Remove(name);
            EnsureTrailingEmptyRow();
            RefreshGrid();
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void FileMenuItem_Click_1(object sender, RoutedEventArgs e)
        {
		}

        private string? Prompt(string title, string initial)
        {
            var box = new TextBox { Text = initial, Margin = new Thickness(10) };
            var ok = new Button
            {
                Content = "OK",
                IsDefault = true,
                Width = 70,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var win = new Window
            {
                Title = title,
                Width = 300,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel { Children = { box, ok } }
            };
            ok.Click += (_, _) => win.DialogResult = true;
            box.Loaded += (_, _) => { box.Focus(); box.SelectAll(); };

            return win.ShowDialog() == true ? box.Text : null;
        }


        private void ExportMenuItem_Click_CSV(object sender, RoutedEventArgs e)
        {
            // Valide la cellule/ligne en cours d'édition pour ne pas perdre la dernière saisie
            LanguageGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var dialog = new SaveFileDialog
            {
                Title = "Export CSV",
                Filter = "CSV file (*.csv)|*.csv",
                DefaultExt = ".csv",
                FileName = "export.csv"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                ExportToCsv(dialog.FileName, ';');
                MessageBox.Show("Export terminé.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur pendant l'export :\n{ex.Message}", "Export", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string path, char separator)
        {
            string Escape(string? value)
            {
                value ??= "";
                bool mustQuote = value.Contains(separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
                value = value.Replace("\"", "\"\"");
                return mustQuote ? $"\"{value}\"" : value;
            }

            var sb = new StringBuilder();

            // En-têtes
            sb.AppendLine(string.Join(separator, _table.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName))));

            // Lignes (on saute les lignes entièrement vides, donc la ligne vierge du bas)
            foreach (DataRow row in _table.Rows)
            {
                if (IsRowEmpty(row))
                    continue;

                sb.AppendLine(string.Join(separator, row.ItemArray.Select(v => Escape(v == DBNull.Value ? null : v?.ToString()))));
            }

            // UTF-8 avec BOM pour qu'Excel affiche correctement les accents (é, ñ...)
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }
    }
}