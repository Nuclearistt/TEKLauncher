using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TEKLauncher.Servers;
using TEKLauncher.Tabs;
using TEKLauncher.Windows;

namespace TEKLauncher.Controls;

/// <summary>Represents a server in cluster tab GUI.</summary>
partial class ServerItem : UserControl
{
    /// <summary>Initializes a new item to represent specified server.</summary>
    /// <param name="server">Server that will be represented by the item.</param>
    /// <param name="cluster">Cluster that the server belongs to.</param>
    internal ServerItem(Server server, Cluster cluster)
    {
        InitializeComponent();
        DataContext = server;
        ServerName.Text = cluster.IsSpecialCluster ? server.Name : server.DisplayName;
        if (server.IsPvE)
        {
            Mode.Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
            Mode.Text = "PvE";
        }
        else
        {
            Mode.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
            Mode.Text = "PvP";
        }
        Version.Text = server.Version ?? LocManager.GetString(LocCode.NA);
        FavoriteButton.IsChecked = Cluster.Favorites.Servers.Contains(server);
        FavoriteButton.Checked += AddFavorite;
        FavoriteButton.Unchecked += RemoveFavorite;
    }
    /// <summary>Adds the server to Steam favorites.</summary>
    void AddFavorite(object sender, RoutedEventArgs e) => ((Server)DataContext).AddFavorite();
    /// <summary>Executes the game with launch parameter to join the server.</summary>
    void Join(object sender, RoutedEventArgs e) => Game.Launch((Server)DataContext);
    /// <summary>Follows the server's Discord server join link.</summary>
    void JoinDiscord(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo(((Server)DataContext).Info!.Discord!) { UseShellExecute = true });
    /// <summary>Proceeds item initialization that cannot be done in constructor.</summary>
    async void LoadedHandler(object sender, RoutedEventArgs e)
    {
        var server = (Server)DataContext;
        var parent = Parent;
        while (parent is not ClusterTab)
            parent = ((FrameworkElement)parent).Parent;
        bool specialCluster = ((Cluster)((ClusterTab)parent).DataContext).IsSpecialCluster;
        bool detailsAvailable = false;
        if (server.Info is not null)
        {
            if (specialCluster)
            {
                if (server.Info.HosterName is not null)
                {
                    detailsAvailable = true;
                    Hoster.Visibility = Visibility.Visible;
                    Hoster.Text = string.Format(LocManager.GetString(LocCode.HostedBy), server.Info.HosterName);
                }
                if (server.Info.Discord is not null)
                {
                    detailsAvailable = true;
                    JoinDiscordButton.Visibility = Visibility.Visible;
                }
            }
            var description = server.Info.ServerDescription;
            if (specialCluster && description is null)
                description = server.Info.ClusterDescription;
            if (description is not null)
            {
                detailsAvailable = true;
                DescriptionBlock.Visibility = Visibility.Visible;
                void AddMultiplierIfSpecified<T>(T? multiplier, LocCode nameCode) where T : struct
                {
                    if (multiplier.HasValue)
                    {
                        Description.Inlines.Add(LocManager.GetString(nameCode));
                        Description.Inlines.Add($" {multiplier.Value}{(nameCode == LocCode.MaxWildDinoLevel ? null : "x")}\n");
                    }
                }
                AddMultiplierIfSpecified(description.MaxDinoLvl, LocCode.MaxWildDinoLevel);
                AddMultiplierIfSpecified(description.Taming, LocCode.Taming);
                AddMultiplierIfSpecified(description.Experience, LocCode.Experience);
                AddMultiplierIfSpecified(description.Harvesting, LocCode.Harvesting);
                AddMultiplierIfSpecified(description.Breeding, LocCode.Breeding);
                AddMultiplierIfSpecified(description.Stacks, LocCode.Stacks);
                if (description.Other is not null)
                    foreach (string line in description.Other)
                        Description.Inlines.Add($"{line}\n");
            }
        }
        if (server.ModIds.Length > 0)
        {
            detailsAvailable = true;
            ModsBlock.Visibility = Visibility.Visible;
            var details = await Task.Run(() => Steam.CM.Client.GetModDetails(server.ModIds));
            if (details.Length < server.ModIds.Length)
            {
                details = new Mod.ModDetails[server.ModIds.Length];
                for (int i = 0; i < details.Length; i++)
                    details[i] = new() { Id = server.ModIds[i], Name = server.ModIds[i].ToString() };
            }
            for (int i = 0; i < server.ModIds.Length; i++)
                Mods.Children.Add(new ServerModItem(in details[i]));
        }
        if (detailsAvailable)
            DetailsSwitch.Visibility = Visibility.Visible;
    }
    /// <summary>Removes the server from Steam favorites.</summary>
    void RemoveFavorite(object sender, RoutedEventArgs e) => ((Server)DataContext).RemoveFavorite();
    /// <summary>Switches the state of details' expandable block.</summary>
    void SwitchDetails(object sender, RoutedEventArgs e) => Details.Visibility = ((ToggleButton)sender).IsChecked ?? false ? Visibility.Visible : Visibility.Collapsed;
    /// <summary>Creates an <see cref="UpdaterWindow"/> for validating all server mods.</summary>
    void ValidateAllMods(object sender, RoutedEventArgs e) => new UpdaterWindow(((Server)DataContext).ModIds).Show();
}