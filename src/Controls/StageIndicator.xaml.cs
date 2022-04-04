using System.Windows.Controls;
using System.Windows.Media;

namespace TEKLauncher.Controls;

/// <summary>Displays the name and status of a Steam client task stage.</summary>
partial class StageIndicator : UserControl
{
    /// <summary>Initializes a new indicator for specified stage.</summary>
    /// <param name="nameCode">Localization code of the stage name to be displayed.</param>
    internal StageIndicator(LocCode nameCode)
    {
        InitializeComponent();
        StageName.Text = LocManager.GetString(nameCode);
    }
    /// <summary>Changes the status of the stage to either succeeded or failed.</summary>
    /// <param name="success"><see langword="true"/> if the stage has been completed successfully; otherwise, <see langword="false"/>.</param>
    public void Finish(bool success)
    {
        if (success)
        {
            Icon.Stroke = new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
            Icon.Data = new PathGeometry(new PathFigure[] { new(new(2, 9), new LineSegment[] { new(new(8, 15), true), new(new(15, 3), true) }, false) });
        }
        else
        {
            Icon.Stroke = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
            Icon.Data = new PathGeometry(new PathFigure[] { new(new(2, 2), new LineSegment[] { new(new(14, 14), true) }, false), new(new(2, 14), new LineSegment[] { new(new(14, 2), true) }, false) });
        }
    }
}