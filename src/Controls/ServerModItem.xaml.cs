using System.Diagnostics;
using System.Windows.Controls;
using TEKLauncher.Windows;

namespace TEKLauncher.Controls;

/// <summary>Represents a mod in server item GUI.</summary>
partial class ServerModItem : UserControl
{
    /// <summary>Steam workshop details of the mod.</summary>
    readonly Mod.ModDetails _details;
    /// <summary>Steam published file ID of the mod.</summary>
    public readonly ulong Id;
    /// <summary>Initializes a new item to represent specified mod.</summary>
    /// <param name="details">Steam workshop details of the mod that will be represented by the item.</param>
    internal ServerModItem(in Mod.ModDetails details)
    {
        InitializeComponent();
        _details = details;
        Id = details.Id;
        ModName.Text = _details.Name;
        bool installed;
        lock (Mod.List)
            installed = Mod.List.Find(m => m.Id == Id) is not null;
        if (installed)
            InstalledText.Visibility = Visibility.Visible;
        else
            InstallButton.Visibility = Visibility.Visible;
    }
    /// <summary>Opens mod's workshop page in Steam app browser.</summary>
    void Follow(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo($"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={Id}") { UseShellExecute = true });
    /// <summary>Creates an <see cref="UpdaterWindow"/> for installing the mod.</summary>
    void Install(object sender, RoutedEventArgs e)
    {
        foreach (var window in Application.Current.Windows)
            if (window is UpdaterWindow updaterWindow && updaterWindow.Item is Mod.ModDetails details && details.Id == Id)
                return;
        new UpdaterWindow(in _details, false).Show();
    }
    /// <summary>Updates the item to display mod's status as installed.</summary>
    public void SetInstalled()
    {
        InstallButton.Visibility = Visibility.Collapsed;
        InstalledText.Visibility = Visibility.Visible;
    }
}