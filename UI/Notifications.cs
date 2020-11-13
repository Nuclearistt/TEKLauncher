using System.Windows.Controls;
using TEKLauncher.Controls;
using TEKLauncher.Windows;
using static System.Threading.Tasks.Task;
using static TEKLauncher.Controls.Notification;

namespace TEKLauncher.UI
{
    internal static class Notifications
    {
        private static UIElementCollection Stack;
        internal static void Add(string Message, string ButtonText, AcceptEventHandler AcceptHandler) => Stack.Insert(0, new Notification(Message, ButtonText, AcceptHandler, 0));
        internal static async void AddImage(string Message, string Source)
        {
            Notification Notification = new Notification(Message, Source, null, 2);
            Stack.Insert(0, Notification);
            await Delay(7000);
            Notification.Hide();
        }
        internal static void Initialize(MainWindow Window) => Stack = Window.NotificationsStack.Children;
        internal static Notification AddLoading(string Message, string ButtonText, AcceptEventHandler AcceptHandler)
        {
            Notification Notification = new Notification(Message, ButtonText, AcceptHandler, 1);
            Stack.Insert(0, Notification);
            return Notification;
        }
        internal static Notification AddLoading(string Message) => AddLoading(Message, null, null);
    }
}