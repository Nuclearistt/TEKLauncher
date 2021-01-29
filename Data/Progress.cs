using System.Windows;
using static System.Environment;
using static System.Math;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Data
{
    internal class Progress
    {
        internal Progress(ProgressUpdatedEventHandler ProgressUpdated) => this.ProgressUpdated = ProgressUpdated;
        private int SpeedUpdated;
        private long Difference;
        internal long Current, ETA = -1L, Previous, Total;
        internal double Ratio;
        private readonly ProgressUpdatedEventHandler ProgressUpdated;
        internal string Percentage => $"{Round(Ratio * 100D)}%";
        internal string PrecisePercentage => $"{Round(Ratio * 100D, 2)}%";
        internal delegate void ProgressUpdatedEventHandler();
        internal void Increase()
        {
            Ratio = (double)(++Current) / Total;
            int CurrentTime = TickCount;
            if (CurrentTime - SpeedUpdated > 999)
            {
                if ((Difference = Current - Previous) == 0L)
                    Difference = 1L;
                Previous = Current;
                SpeedUpdated = CurrentTime;
                ETA = (Total - Current) / Difference;
            }
            Application.Current.Dispatcher.Invoke(ProgressUpdated);
        }
        internal void Increase(long Increment)
        {
            Ratio = (double)(Current += Increment) / Total;
            int CurrentTime = TickCount;
            if (CurrentTime - SpeedUpdated > 999)
            {
                if ((Difference = Current - Previous) == 0L)
                    Difference = 1L;
                Previous = Current;
                SpeedUpdated = CurrentTime;
                ETA = (Total - Current) / Difference;
            }
            try { Application.Current.Dispatcher.Invoke(ProgressUpdated); }
            catch { }
        }
        internal string GetSpeed(out string Unit)
        {
            string Value = ConvertBytesSep(Difference, out string ConversionUnit);
            Unit = $"{ConversionUnit}/{LocString(LocCode.s)}";
            return Value;
        }
    }
}