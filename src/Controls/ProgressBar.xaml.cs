using System.Windows.Controls;

namespace TEKLauncher.Controls;

/// <summary>Circular progress bar.</summary>
partial class ProgressBar : UserControl
{
    /// <summary>Value that indicates whether the progress should be displayed as amount of bytes or discrete numbers.</summary>
    bool _binaryMode;
    /// <summary>Current value of the progress.</summary>
    long _current;
    /// <summary>Timestamp of the last time the progress display was updated.</summary>
    long _lastRecordedTime;
    /// <summary>The progress value saved last time the progress display was updated.</summary>
    long _lastRecordedValue;
    /// <summary>Timestamp of the moment the progress started.</summary>
    long _startTime;
    /// <summary>Total value of the progress.</summary>
    long _total;
    /// <summary>Gets or sets the quotient of dividing current progress value by total.</summary>
    public double Ratio { get; private set; }
    /// <summary>Initializes a new progress bar.</summary>
    public ProgressBar() => InitializeComponent();
    /// <summary>Clears previous progress data and sets up new one based on its type and total</summary>
    /// <param name="binaryMode">Value that indicates whether the progress should be displayed as amount of bytes or discrete numbers.</param>
    /// <param name="total">Total value of the new progress.</param>
    public void Initialize(bool binaryMode, long total)
    {
        _binaryMode = binaryMode;
        _lastRecordedValue = _current = 0;
        _total = total;
        Bar.StrokeDashOffset = 311;
        ProgressValue.Text = "0";
        if (binaryMode)
        {
            _startTime = _lastRecordedTime = Environment.TickCount64;
            Progress.FontSize = 20;
            SpeedUnit.Text = ProgressUnit.Text = LocManager.GetString(LocCode.KB);
            TotalValue.Text = LocManager.BytesToString(total, out string unit);
            TotalUnit.Text = unit;
            Percentage.Text = "0%";
            SpeedValue.Text = "0";
            ETA.Visibility = Speed.Visibility = Percentage.Visibility = Visibility.Visible;
            ETAValue.Text = null;
        }
        else
        {
            Progress.FontSize = 34;
            ProgressUnit.Text = null;
            TotalValue.Text = total.ToString();
            TotalUnit.Text = null;
            ETA.Visibility = Speed.Visibility = Percentage.Visibility = Visibility.Collapsed;
        }
    }
    /// <summary>Updates the value of progress and, if necessary, the displayed values.</summary>
    /// <param name="increment">The increment of progress value relatively to the last one. May be negative.</param>
    public void Update(long increment)
    {
        _current += increment;
        Ratio = (double)_current / _total;
        Bar.StrokeDashOffset = 311 * (1 - Ratio);
        if (_binaryMode)
        {
            ProgressValue.Text = LocManager.BytesToString(_current, out string unit);
            ProgressUnit.Text = unit;
            string percentageText = (Ratio * 100).ToString(_total >= 104857600 ? "0.##" : "0");
            Percentage.Text = string.Concat(percentageText, "%");
            long timeDifference = Environment.TickCount64 - _lastRecordedTime;
            if (timeDifference >= 1000)
            {
                _lastRecordedTime += timeDifference;
                long speed = _current - _lastRecordedValue;
                SpeedValue.Text = LocManager.BytesToString(speed, out unit);
                SpeedUnit.Text = unit;
                _lastRecordedValue = _current;
                long eta = (long)((_total - _current) / ((_current == 0 ? 1 : _current) / ((_lastRecordedTime - _startTime) / 1000.0)));
                if (eta >= 0) //eta may be negative when progress value is decreased, so this check is needed to avoid visual bugs
                    ETAValue.Text = LocManager.SecondsToString(eta);
            }
        }
        else
            ProgressValue.Text = _current.ToString();
    }
}