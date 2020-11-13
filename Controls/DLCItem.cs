using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TEKLauncher.ARK;
using TEKLauncher.Windows;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.UI.Message;

namespace TEKLauncher.Controls
{
    public partial class DLCItem : UserControl
    {
        internal DLCItem(DLC DLC)
        {
            this.DLC = DLC;
            InitializeComponent();
            Image.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/Images/{DLC.SpacelessName}.jpg"));
            NameBlock.Text = DLC.Name;
            SetStatus(DLC.Status);
        }
        internal readonly DLC DLC;
        private void Install(object Sender, RoutedEventArgs Args) => new ValidatorWindow(DLC, false).Show();
        private void SizeChangedHandler(object Sender, SizeChangedEventArgs Args) => Border.Height = Border.ActualWidth * 215D / 460D;
        private void Uninstall(object Sender, RoutedEventArgs Args)
        {
            if (ShowOptions("Warning", $"Are you sure you want to uninstall {DLC.Name} DLC?"))
                DLC.Uninstall();
        }
        private void Validate(object Sender, RoutedEventArgs Args) => new ValidatorWindow(DLC, true).Show();
        internal void SetStatus(Status Status)
        {
            switch (Status)
            {
                case Status.NotInstalled: StatusBlock.Foreground = DarkRed; StatusBlock.Text = "Not installed"; break;
                case Status.Installed: StatusBlock.Foreground = DarkGreen; StatusBlock.Text = "Installed"; break;
                case Status.CheckingForUpdates: StatusBlock.Foreground = YellowBrush; StatusBlock.Text = "Checking for updates..."; break;
                case Status.UpdateAvailable: StatusBlock.Foreground = YellowBrush; StatusBlock.Text = "Update available"; break;
                case Status.Updating: StatusBlock.Foreground = YellowBrush; StatusBlock.Text = "Downloading..."; break;
            }
            NameBlock.Foreground = Status == Status.UpdateAvailable ? YellowBrush : (SolidColorBrush)FindResource("BrightestBrightBrush");
            DownloadButton.Visibility = (int)Status % 3 == 0 ? Visibility.Visible : Visibility.Collapsed;
            UninstallButton.Visibility = ((int)Status + 1) % 2 == 0 ? Visibility.Visible : Visibility.Collapsed;
            ValidateButton.Visibility = Status == Status.CheckingForUpdates || Status == Status.Updating ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}