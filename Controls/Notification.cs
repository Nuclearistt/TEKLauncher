using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static System.TimeSpan;
using static System.Threading.Tasks.Task;
using static System.Windows.Controls.Grid;
using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.Controls
{
    public partial class Notification : UserControl
    {
        internal Notification(string Message, string ButtonText, AcceptEventHandler AcceptHandler, int Mode)
        {
            InitializeComponent();
            Accept = AcceptHandler;
            MessagePresenter.Text = Message;
            if (Mode != 0)
                ButtonsGrid.Visibility = Visibility.Collapsed;
            if (Mode == 1)
            {
                LoadingSpinner Spinner = new LoadingSpinner { Margin = new Thickness(0D, 0D, 25D, 0D), HorizontalAlignment = HorizontalAlignment.Center };
                SetColumn(Spinner, 1);
                ContentGrid.Children.Add(Spinner);
            }
            else if (Mode == 2)
            {
                VectorImage Image = new VectorImage { Width = 30D, Height = 30D, Margin = new Thickness(0D, 0D, 5D, 0D), VerticalAlignment = VerticalAlignment.Center, Source = ButtonText };
                SetColumn(Image, 1);
                ContentGrid.Children.Add(Image);
            }
            if (!(ButtonText is null) && LocCulture != "el" && LocCulture != "ar")
                ButtonText = ButtonText.ToUpper();
            AcceptButton.Content = ButtonText;
        }
        private readonly AcceptEventHandler Accept;
        internal delegate void AcceptEventHandler();
        private void AcceptHandler(object Sender, RoutedEventArgs Args)
        {
            Hide();
            Accept();
        }
        private void DismissHandler(object Sender, RoutedEventArgs Args) => Hide();
        internal void FinishLoading(string NewMessage, bool ShowButtons)
        {
            ContentGrid.Children.RemoveAt(1);
            MessagePresenter.Text = NewMessage;
            if (ShowButtons)
                ButtonsGrid.Visibility = Visibility.Visible;
        }
        internal async void Hide()
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(1D, 0D, FromMilliseconds(300D)));
            await Delay(300);
            ((Panel)Parent)?.Children?.Remove(this);
        }
    }
}