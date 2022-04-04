using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TEKLauncher.Windows;

namespace TEKLauncher.Controls;

/// <summary>Represents a mod in Mods tab GUI.</summary>
partial class ModItem : UserControl
{
    /// <summary>Initializes a new item to represent specified mod.</summary>
    /// <param name="mod">Mod that will be represented by the item.</param>
    internal ModItem(Mod mod)
    {
        InitializeComponent();
        DataContext = mod;
        Id.Text = mod.Id.ToString();
        LoadDetails();
        SetStatus();
        mod.Item = this;
    }
    /// <summary>Copies ID of the mod to clipboard.</summary>
    void CopyId(object sender, RoutedEventArgs e)
    {
        var mod = (Mod)DataContext;
        Clipboard.SetText(mod.Id.ToString());
        Notifications.Add(LocManager.GetString(LocCode.ModIdCopied), "NSuccess");
    }
    /// <summary>Prompts uninstall of the mod.</summary>
    async void Delete(object sender, RoutedEventArgs e)
    {
        if (Game.IsRunning)
        { 
            Messages.Show("Error", LocManager.GetString(LocCode.ModDeleteFail));
            return;
        }
        if (!Messages.ShowOptions("Info", LocManager.GetString(LocCode.ModDeletePrompt)))
            return;
        IsEnabled = false;
        var mod = (Mod)DataContext;
        await Task.Run(mod.Delete);
        ((VirtualizingStackPanel)Parent).Children.Remove(this);
    }
    /// <summary>Opens mod's workshop page in Steam app browser.</summary>
    void Follow(object sender, RoutedEventArgs e)
    {
        var mod = (Mod)DataContext;
        Process.Start(new ProcessStartInfo($"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={mod.Id}") { UseShellExecute = true });
    }
    /// <summary>Creates an <see cref="UpdaterWindow"/> for updating the mod.</summary>
    void Update(object sender, RoutedEventArgs e)
    {
        var mod = (Mod)DataContext;
        var details = mod.Details.Status == 0 ? new() { Id = mod.Id, Name = mod.Name } : mod.Details;
        new UpdaterWindow(in details, false).Show();
    }
    /// <summary>Creates an <see cref="UpdaterWindow"/> for validating the mod.</summary>
    void Validate(object sender, RoutedEventArgs e)
    {
        var mod = (Mod)DataContext;
        var details = mod.Details.Status == 0 ? new() { Id = mod.Id, Name = mod.Name } : mod.Details;
        new UpdaterWindow(in details, true).Show();
    }
    /// <summary>Updates the item accordingly to new mod status.</summary>
    internal void SetStatus()
    {
        var status = ((Mod)DataContext).CurrentStatus;
        Status.Foreground = status == Mod.Status.Installed ? new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E)) : Brushes.Yellow;
        Status.Text = LocManager.GetString(status switch
        {
            Mod.Status.Installed => LocCode.Installed,
            Mod.Status.UpdateAvailable => LocCode.UpdateAvailable,
            Mod.Status.Updating => LocCode.Updating,
            Mod.Status.Deleting => LocCode.Deleting,
            _ => (LocCode)status
        });
        UpdateButton.Visibility = status == Mod.Status.UpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
        DeleteButton.Visibility = ValidateButton.Visibility = status == Mod.Status.Installed || status == Mod.Status.UpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
    }
    /// <summary>Updates the item accordingly to new mod details.</summary>
    public void LoadDetails()
    {
        var mod = (Mod)DataContext;
        bool modInfoFileMissing = mod.Name.Length == 0;
        string modInternalName = modInfoFileMissing ? LocManager.GetString(LocCode.ModInfoFileMissing) : mod.Name;
        if (mod.Details.Status == 1)
        {
            Preview.Source = new BitmapImage(new(mod.Details.PreviewUrl.Length == 0 ? "pack://application:,,,/res/img/SteamWorkshopDefaultImage.png" : mod.Details.PreviewUrl));
            MainName.Text = mod.Details.Name;
            SecondaryName.Text = string.Concat("\n", modInternalName);
            if (modInfoFileMissing)
                SecondaryName.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
        }
        else
        {
            Preview.Source = new BitmapImage(new("pack://application:,,,/res/img/SteamWorkshopDefaultImage.png"));
            MainName.Text = modInternalName;
            if (modInfoFileMissing)
                MainName.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
        }
    }
}