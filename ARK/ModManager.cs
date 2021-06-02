using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TEKLauncher.Controls;
using TEKLauncher.Pages;
using TEKLauncher.SteamInterop.Network.CM.Messages.Bodies;
using static System.DateTimeOffset;
using static System.IO.Directory;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Net.Downloader;
using static TEKLauncher.SteamInterop.Steam;
using static TEKLauncher.SteamInterop.SteamworksAPI;
using static TEKLauncher.SteamInterop.Network.SteamClient;
using static TEKLauncher.Utils.TEKArchive;

namespace TEKLauncher.ARK
{
    internal static class ModManager
    {
        internal static ulong[] SpacewarIDs = new ulong[0];
        internal static List<Mod> Mods = new List<Mod>();
        private static readonly object Lock = new object();
        private static ModDetails[] GetModsDetails(object IDs) => GetModsDetails((ulong[])IDs);
        internal static void FetchSpacewarIDs(object State)
        {
            string List = TryDownloadString($"{Arkouda2}Extra/SpacewarIDs.txt", $"{FilesStorage}SpacewarIDs.txt");
            if (List is null)
            {
                using (Stream ResourceStream = GetResourceStream(new Uri("pack://application:,,,/Resources/SpacewarIDs.ta")).Stream)
                using (MemoryStream Stream = new MemoryStream())
                {
                    DecompressSingleFile(ResourceStream, Stream);
                    Stream.Position = 0L;
                    using (StreamReader Reader = new StreamReader(Stream))
                        List = Reader.ReadToEnd();
                }
            }
            SpacewarIDs = List.Split('\n').Select(ID => ulong.Parse(ID)).ToArray();
        }
        internal static void InitializeModsList()
        {
            lock (Lock)
                if (IsSpacewarInstalled && Exists(WorkshopPath))
                {
                    ulong[] SubscribedMods = TryDeploy() ? SteamAPI.GetSubscribedMods() : null;
                    foreach (string Path in EnumerateDirectories(WorkshopPath).Where(Mod => ulong.TryParse(Mod.Substring(Mod.LastIndexOf('\\') + 1), out _)))
                        if (Mods.Find(Mod => Mod.Path == Path) is null)
                            Mods.Add(new Mod(Path, SubscribedMods));
                }
        }
        internal static void LoadModsDetails(object State)
        {
            InitializeModsList();
            ulong[] IDs = Mods.Select(Mod => Mod.ID).ToArray(), OriginIDs = Mods.Select(Mod => Mod.OriginID).Where(ID => ID != 0UL).ToArray();
            ModDetails[] Details = null, OriginDetails = null;
            if (IDs.Length != 0)
                Details = GetModsDetails(IDs);
            if (OriginIDs.Length != 0)
                OriginDetails = GetModsDetails(OriginIDs);
            if (!(Details is null))
                for (int Iterator = 0; Iterator < Details.Length; Iterator++)
                    if (Details[Iterator].Status == 1)
                        Mods.Find(Mod => Mod.ID == IDs[Iterator]).Details = Details[Iterator];
            if (!(OriginDetails is null))
                for (int Iterator = 0; Iterator < OriginDetails.Length; Iterator++)
                    if (OriginDetails[Iterator].Status == 1)
                        Mods.Find(Mod => Mod.OriginID == OriginIDs[Iterator]).OriginDetails = OriginDetails[Iterator];
            foreach (Mod Mod in Mods)
                if (Mod.IsInstalled)
                {
                    long LocalUpdateTime = File.GetLastWriteTimeUtc(Mod.ModFilePath).Ticks;
                    if (Mod.OriginID == 0UL)
                    {
                        if (Mod.Details.LastUpdated > LocalUpdateTime)
                            Mod.UpdateAvailable = true;
                    }
                    else if (Mod.OriginDetails.LastUpdated > LocalUpdateTime)
                        Mod.UpdateAvailable = true;
                }
            Current.Dispatcher.Invoke(() =>
            {
                if (!(Details is null && OriginDetails is null) && Instance.CurrentPage is ModsPage Page)
                {
                    Page.ModsList.Children.Clear();
                    foreach (Mod Mod in Mods)
                        Page.ModsList.Children.Add(new ModItem(Mod));
                }
            });
        }
        internal static ModDetails[] GetModsDetails(params ulong[] IDs)
        {
            try
            {
                if (Connect())
                {
                    List<ItemDetails> Details = GetDetails(IDs);
                    if (Details is null)
                        return null;
                    ModDetails[] Result = new ModDetails[Details.Count];
                    for (int Iterator = 0; Iterator < Details.Count; Iterator++)
                    {
                        ItemDetails Item = Details[Iterator];
                        ref ModDetails ResultItem = ref Result[Iterator];
                        ResultItem.AppID = Item.AppID;
                        ResultItem.ID = Item.ID;
                        ResultItem.LastUpdated = FromUnixTimeSeconds(Item.LastUpdated).Ticks;
                        ResultItem.Name = Item.Name;
                        ResultItem.PreviewURL = Item.PreviewURL;
                        ResultItem.Status = Item.Result == 1 ? 1 : Item.Result == 9 ? 2 : 0;
                    }
                    return Result;
                }
                else
                    return null;
            }
            catch { return null; }
        }
        internal static Task InitializeModsListAsync() => Run(InitializeModsList);
        internal static Task<ModDetails[]> GetModsDetailsAsync(params ulong[] IDs) => Factory.StartNew(GetModsDetails, IDs);
    }
}