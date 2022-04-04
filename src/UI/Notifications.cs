using System.Windows.Controls;
using TEKLauncher.Controls;

namespace TEKLauncher.UI;

/// <summary>Manages main window notifications.</summary>
static class Notifications
{
    /// <summary>Main window's notification stack children collection.</summary>
    static UIElementCollection s_stack = null!;
    /// <summary>Creates a notification with text message and an icon.</summary>
    /// <param name="message">Message to be displayed in the notification.</param>
    /// <param name="iconName">Name of the icon to load.</param>
    public static void Add(string message, string iconName) => s_stack.Insert(0, new Notification(message, iconName));
    /// <summary>Creates a notification with text message and a button.</summary>
    /// <param name="message">Message to be displayed in the notification.</param>
    /// <param name="buttonText">Text to be displayed on the button.</param>
    /// <param name="buttonHandler">Function to be executed when the button is clicked.</param>
    public static void Add(string message, string buttonText, Action buttonHandler) => s_stack.Insert(0, new Notification(message, buttonText, buttonHandler));
    /// <summary>Initializes the manager with the element collection to put notifications to.</summary>
    /// <param name="stack">Notification stack children collection.</param>
    public static void Initialize(UIElementCollection stack) => s_stack = stack;
}