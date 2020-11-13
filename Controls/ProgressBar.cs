using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TEKLauncher.Data;
using static System.Math;
using static System.Windows.Media.Color;
using static TEKLauncher.App;
using static TEKLauncher.Data.Progress;

namespace TEKLauncher.Controls
{
    public partial class ProgressBar : UserControl
    {
        public ProgressBar()
        {
            InitializeComponent();
            Progress = new Progress(UpdateProgress);
        }
        private bool NumericMode = false, UnknownTotalMode = false;
        private double HalfHeight, HalfWidth;
        internal ProgressUpdatedEventHandler ProgressUpdated;
        internal readonly Progress Progress;
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            HalfHeight = ActualHeight / 2D;
            HalfWidth = ActualWidth / 2D;
            Arc.Point = Figure.StartPoint = new Point(HalfWidth, 7D);
            Arc.Size = new Size(HalfWidth - 7D, HalfHeight - 7D);
        }
        private void UpdateProgress()
        {
            if (!UnknownTotalMode && Progress.Total < 1L)
                SetUnknownTotalMode();
            if (UnknownTotalMode)
            {
                Percentage.Text = Progress.BytesProgress;
                Speed.Text = Progress.Speed;
            }
            else
            {
                double Ratio = Progress.Ratio, Angle = 6.283185307179586D * Ratio;
                ElementBorder.Fill = new SolidColorBrush(FromRgb((byte)(21D + 139D * Ratio), (byte)(21D + 79D * Ratio), (byte)(21D * (1D - Ratio))));
                if (Ratio == 1D)
                    Angle -= 0.0000001D;
                Arc.IsLargeArc = Angle > PI;
                Arc.Point = new Point(HalfWidth + (HalfWidth - 7D) * Sin(Angle), HalfHeight - (HalfHeight - 7D) * Cos(Angle));
                if (NumericMode)
                    Percentage.Text = $"{Progress.Current}/{Progress.Total}";
                else
                {
                    BytesProgress.Text = Progress.BytesTotalProgress;
                    Percentage.Text = Progress.Total > 104857600L ? Progress.PrecisePercentage : Progress.Percentage;
                    Speed.Text = Progress.Speed;
                }
            }
            ProgressUpdated?.Invoke();
        }
        private void SetUnknownTotalMode()
        {
            if (IsLoaded)
                SetUnknownTotalMode(null, null);
            else
                Loaded += SetUnknownTotalMode;
        }
        private void SetUnknownTotalMode(object Sender, RoutedEventArgs Args)
        {
            UnknownTotalMode = true;
            ElementBorder.Fill = (SolidColorBrush)FindResource("DarkestDarkBrush");
            ProgressLine.Stroke = YellowBrush;
            Arc.IsLargeArc = true;
            double Angle = 6.283185207179586D;
            Arc.Point = new Point(HalfWidth + (HalfWidth - 7D) * Sin(Angle), HalfHeight - (HalfHeight - 7D) * Cos(Angle));
            BytesProgress.Visibility = Visibility.Hidden;
            Percentage.Text = "0 KB";
            Speed.Text = "0 KB/s";
        }
        internal void SetDownloadMode()
        {
            if (NumericMode)
            {
                NumericMode = false;
                ElementBorder.Fill = (SolidColorBrush)FindResource("DarkestDarkBrush");
                ProgressLine.Stroke = (SolidColorBrush)FindResource("CyanBrush");
                Speed.Visibility = BytesProgress.Visibility = Visibility.Visible;
            }
            Arc.IsLargeArc = false;
            Arc.Point = new Point(HalfWidth, 7D);
            BytesProgress.Text = "0 KB";
            Percentage.Text = "0%";
            Speed.Text = "0 KB/s";
            Progress.Current = 0L;
        }
        internal void SetNumericMode()
        {
            if (!NumericMode)
            {
                NumericMode = true;
                ElementBorder.Fill = (SolidColorBrush)FindResource("DarkestDarkBrush");
                ProgressLine.Stroke = (SolidColorBrush)FindResource("OrangeBrush");
                Speed.Visibility = BytesProgress.Visibility = Visibility.Hidden;
            }
            Arc.IsLargeArc = false;
            Arc.Point = new Point(HalfWidth, 7D);
            Percentage.Text = $"0/{Progress.Total}";
            Progress.Current = 0L;
        }
    }
}