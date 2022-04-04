using System.Windows.Controls;

namespace TEKLauncher.Controls;

/// <summary>Game path selector control.</summary>
partial class PathSelector : UserControl
{
    /// <summary>Initializes a new path selector.</summary>
    public PathSelector() => InitializeComponent();
    /// <summary>Creates a dialog to select new path.</summary>
    void Select(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog(); //The only reason to reference WinForms assembly :/
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            Path.Text = dialog.SelectedPath;
            PathChanged?.Invoke(dialog.SelectedPath);
        }
    }
    /// <summary>Sets the path displayed in the path selector.</summary>
    /// <param name="newPath">New path to be displayed in the path selector.</param>
    public void SetPath(string newPath) => Path.Text = newPath;
    /// <summary>Occurs when new path has been selected through a dialog; new path is passed as the argument.</summary>
    public event Action<string>? PathChanged;
}