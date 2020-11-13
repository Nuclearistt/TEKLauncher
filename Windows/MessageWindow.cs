using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Windows.Controls.Grid;

namespace TEKLauncher.Windows
{
    public partial class MessageWindow : Window
    {
        internal MessageWindow(string Type, string Message, bool TwoOptions)
        {
            InitializeComponent();
            Image.Source = TitleBlock.Text = Title = Type;
            MessageBlock.Text = Message;
            if (TwoOptions)
            {
                Button YesButton = new Button { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Content = "Yes" },
                    NoButton = new Button { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Content = "No" };
                YesButton.Click += Close;
                NoButton.Click += Close;
                SetColumn(YesButton, 1);
                SetColumn(NoButton, 1);
                MainGrid.Children.Add(YesButton);
                MainGrid.Children.Add(NoButton);
            }
            else
            {
                Button OKButton = new Button { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Content = "OK" };
                OKButton.Click += Close;
                SetColumn(OKButton, 1);
                MainGrid.Children.Add(OKButton);
            }
        }
        internal bool Result;
        private void Close(object Sender, RoutedEventArgs Args)
        {
            Result = (string)((Button)Sender).Content != "No";
            Close();
        }
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
    }
}