using TEKLauncher.Windows;

namespace TEKLauncher.UI;

/// <summary>Manages message windows.</summary>
class Messages
{
    /// <summary>Displays a message window with OK button.</summary>
    /// <param name="type">Type of the message as well as name of the icon and title.</param>
    /// <param name="message">Message text displayed in the window.</param>
    public static void Show(string type, string message) => new MessageWindow(type, message, false).ShowDialog();
    /// <summary>Displays a message window with two options (Yes and No) and returns the result of user's choice.</summary>
    /// <param name="type">Type of the message as well as name of the icon and title.</param>
    /// <param name="message">Message text displayed in the window.</param>
    public static bool ShowOptions(string type, string message) => new MessageWindow(type, message, true).ShowDialog() ?? false;
    public static void ShowDownloadErr(string name, string url) => new MessageWindow(name, url).ShowDialog();
}