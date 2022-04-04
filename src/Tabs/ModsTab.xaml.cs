using System.Windows.Controls;
using TEKLauncher.Controls;
using TEKLauncher.Windows;

namespace TEKLauncher.Tabs;

/// <summary>Tab that displays the list of mods.</summary>
partial class ModsTab : ContentControl
{
    /// <summary>Initializes a new instance of Mods tab.</summary>
    public ModsTab()
    {
        InitializeComponent();
        ReloadList();
    }
    /// <summary>Navigates main window to Mod installer tab.</summary>
    void InstallMod(object sender, RoutedEventArgs e) => ((MainWindow)Application.Current.MainWindow).Navigate(new ModInstallerTab());
    /// <summary>Reloads the displayed mod list.</summary>
    public void ReloadList()
    {
        Mods.Children.Clear();
        lock (Mod.List)
            foreach (var mod in Mod.List)
                Mods.Children.Add(new ModItem(mod));
    }
    /// <summary>Updates displayed mod details for all present mod items.</summary>
    public void UpdateDetails()
    {
        foreach (ModItem item in Mods.Children)
            item.LoadDetails();
    }
}