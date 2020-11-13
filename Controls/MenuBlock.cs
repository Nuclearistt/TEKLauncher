using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static System.Math;
using static System.TimeSpan;
using static System.Threading.Tasks.Task;
using static System.Windows.EventManager;
using static TEKLauncher.App;

namespace TEKLauncher.Controls
{
    public partial class MenuBlock : UserControl
    {
        public MenuBlock() => InitializeComponent();
        private int ActiveItemIndex = 0;
        public static readonly RoutedEvent NavigatedEvent = RegisterRoutedEvent("Navigated", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuBlock));
        internal bool Communism
        {
            set
            {
                ItemsStack.Children.RemoveAt(3);
                MenuRadioButton NewItem = new MenuRadioButton { Name = "Mods", GroupName = "Menu", Source = value ? "CommunismMods" : "Mods" };
                NewItem.Checked += NavigatedHandler;
                ItemsStack.Children.Insert(3, NewItem);
            }
        }
        internal MenuRadioButton ActiveItem
        {
            get
            {
                foreach (MenuRadioButton Item in ItemsStack.Children)
                    if (Item.IsChecked ?? false)
                        return Item;
                return null;
            }
        }
        public RoutedEventHandler Navigated { set => AddHandler(NavigatedEvent, value); }
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            foreach (RadioButton Item in ItemsStack.Children)
            {
                Item.GroupName = "Menu";
                Item.Checked += NavigatedHandler;
            }
            if (InstallMode)
                Settings.IsChecked = true;
            else
                Play.IsChecked = true;
        }
        private async void NavigatedHandler(object Sender, RoutedEventArgs Args)
        {
            RaiseEvent(new RoutedEventArgs(NavigatedEvent));
            int NewItemIndex = ItemsStack.Children.IndexOf((UIElement)Sender), Difference = NewItemIndex - ActiveItemIndex;
            double Height = Play.ActualHeight;
            if (Difference != 0)
            {
                Line.BeginAnimation(HeightProperty, new DoubleAnimation(Line.ActualHeight, Height * (Abs(Difference) + 1), FromMilliseconds(200D)));
                if (Difference < 0)
                {
                    Line.BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(3D, Height * NewItemIndex, 3D, 0D), FromMilliseconds(200D)));
                    await Delay(200);
                }
                else
                {
                    await Delay(200);
                    Line.BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(3D, Height * NewItemIndex, 3D, 0D), FromMilliseconds(200D)));
                }
                Line.BeginAnimation(HeightProperty, new DoubleAnimation(Line.ActualHeight, Height, FromMilliseconds(200D)));
                ActiveItemIndex = NewItemIndex;
            }
        }
    }
}