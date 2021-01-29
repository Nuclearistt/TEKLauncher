using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static System.Math;
using static System.Windows.Input.Mouse;
using static System.Windows.Media.Color;
using static TEKLauncher.Data.Settings;

namespace TEKLauncher.Controls
{
    public partial class Slider : UserControl
    {
        public Slider()
        {
            Value = DwThreadsCount;
            InitializeComponent();
            Text.Text = Value.ToString();
        }
        private int Value;
        private double CapturedX, CurrentMarginLeft, UnitWidth;
        private void AdjustSize()
        {
            UnitWidth = (MainLine.Width = Root.ActualWidth - 30D) / 16D;
            Mark.Margin = new Thickness(UnitWidth * (Value - 4D), 0D, 0D, 0D);
            for (int Iterator = 0; Iterator < 17; Iterator++)
                Root.Children.Insert(2, new Rectangle
                {
                    Width = 2D,
                    Height = 15D,
                    Margin = new Thickness(14.5D + Iterator * UnitWidth, 0D, 0D, 0D),
                    Fill = new SolidColorBrush(FromRgb(0xBB, 0xBB, 0xBB)),
                    HorizontalAlignment = HorizontalAlignment.Left
                });
        }
        private void LoadedHandler(object Sender, RoutedEventArgs Args) => AdjustSize();
        private void MouseDownHandler(object Sender, MouseButtonEventArgs Args)
        {
            Capture(Mark);
            CapturedX = GetPosition(null).X;
            CurrentMarginLeft = Mark.Margin.Left;
            Mark.PreviewMouseMove += MouseMoveHandler;
        }
        private void MouseMoveHandler(object Sender, MouseEventArgs Args)
        {
            double Offset = CurrentMarginLeft + GetPosition(null).X - CapturedX;
            if (Offset < 0D)
            {
                Text.Text = "4";
                Mark.Margin = new Thickness(0D);
                DwThreadsCount = Value = 4;
            }
            else
            {
                Offset = Round(Offset / UnitWidth) * UnitWidth;
                Mark.Margin = new Thickness(Offset > MainLine.Width ? MainLine.Width : Offset, 0D, 0D, 0D);
                Text.Text = (DwThreadsCount = Value = (int)Round(Mark.Margin.Left / UnitWidth + 4D)).ToString();
            }
        }
        private void MouseUpHandler(object Sender, MouseButtonEventArgs Args)
        {
            Capture(null);
            Mark.PreviewMouseMove -= MouseMoveHandler;
        }
        private void SizeChangedHandler(object Sender, SizeChangedEventArgs Args)
        {
            if (IsLoaded)
            {
                for (int Indexer = 0; Indexer < 17; Indexer++)
                    Root.Children.RemoveAt(2);
                AdjustSize();
            }
        }
    }
}