using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TEKLauncher.Tabs;

/// <summary>Tab that provides information about the application.</summary>
partial class AboutTab : ContentControl
{
    /// <summary>Initializes a new instance of About tab.</summary>
    public AboutTab() => InitializeComponent();
    /// <summary>Follows sender hyperlink's URL.</summary>
    void FollowLink(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo(((Hyperlink)sender).NavigateUri.ToString()) { UseShellExecute = true });
}