using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace TEKLauncher.Controls;

/// <summary>Represents a Steam workshop mod in mod installer tab GUI.</summary>
partial class WorkshopItem : UserControl
{
    /// <summary>Steam published file ID of the mod.</summary>
    readonly ulong _id;
    /// <summary>Occurs when Select button is clicked; ID of the mod is passed as the argument.</summary>
    readonly Action<ulong> _selectedHandler;
    /// <summary>Initializes a new item to represent specified mod.</summary>
    /// <param name="details">Steam workshop details of the mod that will be represented by the item.</param>
    /// <param name="selectedHandler">Function to be invoked when Select button of the item is clicked.</param>
    internal WorkshopItem(in Mod.ModDetails details, Action<ulong> selectedHandler)
    {
        _id = details.Id;
        _selectedHandler = selectedHandler;
        InitializeComponent();
        ModName.Text = details.Name;
        Id.Text = details.Id.ToString();
        Preview.Source = new BitmapImage(new(details.PreviewUrl.Length == 0 ? "pack://application:,,,/res/img/SteamWorkshopDefaultImage.png" : details.PreviewUrl));
        bool installed;
        lock (Mod.List)
            installed = Mod.List.Find(m => m.Id == _id) is not null;
        if (installed)
            InstalledText.Visibility = Visibility.Visible;
        else
            SelectButton.Visibility = Visibility.Visible;
    }
    /// <summary>Opens mod's workshop page in Steam app browser.</summary>
    void Follow(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo($"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={_id}") { UseShellExecute = true });
    /// <summary>Selects the mod represented by the item.</summary>
    void Select(object sender, RoutedEventArgs e) => _selectedHandler(_id);
}