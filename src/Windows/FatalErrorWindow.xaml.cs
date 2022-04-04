using System.Diagnostics;
using System.Windows.Documents;

namespace TEKLauncher.Windows;

/// <summary>Window that displays fatal errors.</summary>
partial class FatalErrorWindow : TEKWindow
{
    /// <summary>Initializes a new Fatal error window for specified exception.</summary>
    /// <param name="e">Exception to display in the window.</param>
    public FatalErrorWindow(Exception e)
    {
        InitializeComponent();
        ExceptionData.Text = e.ToString();
    }
    /// <summary>Follows sender hyperlink's URL.</summary>
    void FollowLink(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo(((Hyperlink)sender).NavigateUri.ToString()) { UseShellExecute = true });
}