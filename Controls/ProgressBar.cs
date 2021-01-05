using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TEKLauncher.Data;
using static System.Math;
using static System.Windows.Media.Color;
using static TEKLauncher.App;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Data.Progress;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Controls
{
    public partial class ProgressBar : UserControl
    {
        public ProgressBar()
        {
            InitializeComponent();
            if (LocCulture == "ar")
                SpeedStack.FlowDirection = BytesProgressStack.FlowDirection = FlowDirection.RightToLeft;
            BytesProgress.Text = LocString(LocCode.KB);
            Speed.Text = $"{LocString(LocCode.KB)}/{LocString(LocCode.Second)}";
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
            if (NumericMode)
            {
                BytesProgressValue.Text = Progress.Current.ToString();
                BytesTotalValue.Text = Progress.Total.ToString();
            }
            else
            {
                BytesProgressValue.Text = ConvertBytesSep(Progress.Current, out string Unit);
                BytesProgress.Text = Unit;
                SpeedValue.Text = Progress.GetSpeed(out Unit);
                Speed.Text = Unit;
            }
            if (!UnknownTotalMode)
            {
                double Ratio = Progress.Ratio, Angle = 6.283185307179586D * Ratio;
                ElementBorder.Fill = new SolidColorBrush(FromRgb((byte)(21D + 139D * Ratio), (byte)(21D + 79D * Ratio), (byte)(21D * (1D - Ratio))));
                if (Ratio == 1D)
                    Angle -= 0.0000001D;
                Arc.IsLargeArc = Angle > PI;
                Arc.Point = new Point(HalfWidth + (HalfWidth - 7D) * Sin(Angle), HalfHeight - (HalfHeight - 7D) * Cos(Angle));
                if (!NumericMode)
                {
                    BytesTotalValue.Text = ConvertBytesSep(Progress.Total, out string Unit);
                    BytesTotal.Text = Unit;
                    Percentage.Text = Progress.Total > 104857600L ? Progress.PrecisePercentage : Progress.Percentage;
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
            BytesProgress.FontSize = BytesProgressValue.FontSize = 38D;
            BytesTotal.Text = BytesTotalValue.Text = Slash.Text = string.Empty;
            Percentage.Visibility = Visibility.Collapsed;
            SpeedValue.Text = "0";
            Speed.Text = $"{LocString(LocCode.KB)}/{LocString(LocCode.Second)}";
        }
        internal void SetDownloadMode()
        {
            if (NumericMode || UnknownTotalMode)
            {
                UnknownTotalMode = NumericMode = false;
                ElementBorder.Fill = (SolidColorBrush)FindResource("DarkestDarkBrush");
                ProgressLine.Stroke = (SolidColorBrush)FindResource("CyanBrush");
                BytesTotalValue.FontSize = Slash.FontSize = BytesProgress.FontSize = BytesProgressValue.FontSize = 20D;
                SpeedStack.Visibility = Percentage.Visibility = BytesTotal.Visibility = BytesProgress.Visibility = Visibility.Visible;
            }
            Arc.IsLargeArc = false;
            Arc.Point = new Point(HalfWidth, 7D);
            SpeedValue.Text = BytesTotalValue.Text = BytesProgressValue.Text = "0";
            BytesTotal.Text = BytesProgress.Text = LocString(LocCode.KB);
            Slash.Text = "/";
            Percentage.Text = "0%";
            Speed.Text = $"{LocString(LocCode.KB)}/{LocString(LocCode.Second)}";
            Progress.Current = 0L;
        }
        internal void SetNumericMode()
        {
            if (!NumericMode)
            {
                NumericMode = true;
                ElementBorder.Fill = (SolidColorBrush)FindResource("DarkestDarkBrush");
                ProgressLine.Stroke = (SolidColorBrush)FindResource("OrangeBrush");
                BytesTotalValue.FontSize = Slash.FontSize = BytesProgressValue.FontSize = 38D;
                SpeedStack.Visibility = Percentage.Visibility = BytesTotal.Visibility = BytesProgress.Visibility = Visibility.Collapsed;
            }
            Arc.IsLargeArc = false;
            Arc.Point = new Point(HalfWidth, 7D);
            BytesProgressValue.Text = "0";
            Slash.Text = "/";
            BytesTotalValue.Text = Progress.Total.ToString();
            Progress.Current = 0L;
        }
    }
}