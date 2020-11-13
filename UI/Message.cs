using TEKLauncher.Windows;

namespace TEKLauncher.UI
{
    internal static class Message
    {
        internal static void Show(string Type, string Message) => new MessageWindow(Type, Message, false).ShowDialog();
        internal static bool ShowOptions(string Type, string Message)
        {
            MessageWindow Window = new MessageWindow(Type, Message, true);
            Window.ShowDialog();
            return Window.Result;
        }
    }
}