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
        }
        private DataGridColumn? RightClickedColumn;

        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var d = e.OriginalSource as DependencyObject;
            while (d != null && d is not DataGridColumnHeader)
                d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);

            RightClickedColumn = (d as DataGridColumnHeader)?.Column;
        }

        private void AddColumnClick(object sender, RoutedEventArgs e)
        {
            string name = $"Column{LanguageGrid.Columns.Count + 1}";
            LanguageGrid.Columns.Add(new DataGridTextColumn
            {
                Header = name,
                Binding = new Binding($"Custom[{name}]") { Mode = BindingMode.TwoWay }
            });
        }
        private void RenameColumnClick(object sender, RoutedEventArgs e)
        {
            if (RightClickedColumn == null || RightClickedColumn.Header.ToString() == "ID")
                return;

            string? newName = Prompt("Rename column", RightClickedColumn.Header?.ToString() ?? "");
            if (!string.IsNullOrWhiteSpace(newName))
                RightClickedColumn.Header = newName;
        }

        private void DeleteColumnClick(object sender, RoutedEventArgs e)
        {
            if (RightClickedColumn == null || RightClickedColumn.Header.ToString() == "ID")
                return;

            LanguageGrid.Columns.Remove(RightClickedColumn);
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
    }
}