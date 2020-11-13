using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using TEKLauncher.UI;
using static System.TimeSpan;
using static System.Threading.Tasks.Task;
using static System.Windows.EventManager;
using static System.Windows.DependencyProperty;

namespace TEKLauncher.Controls
{
    public partial class PathSelector : UserControl
    {
        public PathSelector() => InitializeComponent();
        public static readonly DependencyProperty PickFoldersProperty = Register("PickFolders", typeof(bool), typeof(PathSelector), new PropertyMetadata(false)),
            TextProperty = Register("Text", typeof(string), typeof(PathSelector));
        public static readonly RoutedEvent ReselectEvent = RegisterRoutedEvent("Reselect", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PathSelector)),
            SelectEvent = RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PathSelector));
        public bool PickFolders { get => (bool)GetValue(PickFoldersProperty); set => SetValue(PickFoldersProperty, value); }
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public RoutedEventHandler Reselect { set => AddHandler(ReselectEvent, value); }
        public RoutedEventHandler Select { set => AddHandler(SelectEvent, value); }
        private void RemoveAnimation() => BeginAnimation(WidthProperty, null);
        private void SelectHandler(object Sender, RoutedEventArgs Args)
        {
            bool Result;
            if (PickFolders)
            {
                FolderSelectDialog Dialog = new FolderSelectDialog();
                if (Result = Dialog.Show())
                    SetPath(Dialog.Path);
            }
            else
            {
                OpenFileDialog Dialog = new OpenFileDialog();
                if (Result = Dialog.ShowDialog() ?? false)
                    SetPath(Dialog.FileName);
            }
            if (Result)
                RaiseEvent(new RoutedEventArgs(Sender == Reselector ? ReselectEvent : SelectEvent));
        }
        private void StretchAlignment() => HorizontalAlignment = HorizontalAlignment.Stretch;
        internal async void SetPath(string Path)
        {
            Text = Path;
            if (Reselector.Visibility == Visibility.Collapsed)
            {
                bool Tagged = (string)Tag == "D";
                Root.Click -= SelectHandler;
                if (!Tagged)
                    Root.IsEnabled = false;
                Root.Template = (ControlTemplate)Resources["NoTriggersTemplate"];
                Root.IsEnabled = true;
                TextPresenter.Foreground = (SolidColorBrush)FindResource("DarkestDarkBrush");
                Reselector.Visibility = Visibility.Visible;
                if (Tagged)
                    Loaded += (Sender, Args) => ((Border)Root.Template.FindName("ElementBorder", Root)).Background = (SolidColorBrush)FindResource("BrightestBrightBrush");
                else
                {
                    BeginAnimation(WidthProperty, new DoubleAnimation
                    {
                        From = ActualWidth,
                        To = ((Grid)Parent).ColumnDefinitions[1].ActualWidth,
                        Duration = FromMilliseconds(300D)
                    });
                    await Delay(300);
                    Dispatcher.Invoke(RemoveAnimation);
                }
                Dispatcher.Invoke(StretchAlignment);
            }
        }
    }
}