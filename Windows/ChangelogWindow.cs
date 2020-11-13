using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Application;

namespace TEKLauncher.Windows
{
    public partial class ChangelogWindow : Window
    {
        internal ChangelogWindow()
        {
            InitializeComponent();
            string DisplayVersion = App.Version.EndsWith(".0") ? App.Version.Substring(0, App.Version.Length - 2) : App.Version,
                VersionType = App.Version.EndsWith(".0") ? "Update" : "Patch";
            VersionBlock.Text = $"{VersionType} {DisplayVersion}";
            using (Stream ResourceStream = GetResourceStream(new Uri($"pack://application:,,,/Resources/Changelog.txt")).Stream)
            using (StreamReader Reader = new StreamReader(ResourceStream))
                ChangelogBlock.Text = Reader.ReadToEnd();
        }
        private void Close(object Sender, RoutedEventArgs Args) => Close();
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
        private void Minimize(object Sender, RoutedEventArgs Args) => WindowState = WindowState.Minimized;
    }
}