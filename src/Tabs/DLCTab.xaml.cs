using System.Windows.Controls;
using TEKLauncher.Controls;

namespace TEKLauncher.Tabs;

/// <summary>Tab that displays the list of DLCs.</summary>
partial class DLCTab : ContentControl
{
    /// <summary>Initializes a new instance of DLC tab.</summary>
    public DLCTab()
    {
        InitializeComponent();
        int numRows = DLC.List.Length / 3, mod = DLC.List.Length % 3;
        for (int i = 0; i < DLC.List.Length; i++)
        {
            int column = i % 3;
            if (i / 3 == numRows && mod != 0)
            {
                if (mod == 1)
                    column = 1;
                else if (column == 1)
                    column = 2;
            }
            ((StackPanel)Root.Children[column]).Children.Add(new DLCItem(DLC.List[i]));
        }
    }
}