using System.Windows;
using System.Windows.Controls;
using static System.Windows.DependencyProperty;
using static System.Windows.EventManager;

namespace TEKLauncher.Controls
{
    public partial class WindowControlsBlock : UserControl
    {
        public WindowControlsBlock() => InitializeComponent();
        public static readonly DependencyProperty CodeProperty = Register("Code", typeof(int), typeof(WindowControlsBlock));
        public static readonly RoutedEvent CloseEvent = RegisterRoutedEvent("Close", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WindowControlsBlock)),
            MaximizeEvent = RegisterRoutedEvent("Maximize", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WindowControlsBlock)),
            MinimizeEvent = RegisterRoutedEvent("Minimize", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WindowControlsBlock));
        internal bool MaximizedMode
        {
            set
            {
                ElementBorder.CornerRadius = value ? new CornerRadius(15D) : new CornerRadius(15D, 25D, 0D, 15D);
                ((VectorImage)MaximizeButton.Template.FindName("Icon", MaximizeButton)).Source = value ? "Restore" : "Maximize";
            }
        }
        public int Code { get => (int)GetValue(CodeProperty); set => SetValue(CodeProperty, value); }
        public RoutedEventHandler Close { set => AddHandler(CloseEvent, value); }
        public RoutedEventHandler Maximize { set => AddHandler(MaximizeEvent, value); }
        public RoutedEventHandler Minimize { set => AddHandler(MinimizeEvent, value); }
        private void CloseHandler(object Sender, RoutedEventArgs Args) => RaiseEvent(new RoutedEventArgs(CloseEvent));
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            if (Code == 1)
                MaximizeButton.Visibility = MinimizeButton.Visibility = Visibility.Collapsed;
            else if (Code == 2)
                MaximizeButton.Visibility = Visibility.Collapsed;
        }
        private void MaximizeHandler(object Sender, RoutedEventArgs Args) => RaiseEvent(new RoutedEventArgs(MaximizeEvent));
        private void MinimizeHandler(object Sender, RoutedEventArgs Args) => RaiseEvent(new RoutedEventArgs(MinimizeEvent));
    }
}