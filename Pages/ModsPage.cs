﻿using System.Windows;
using System.Windows.Controls;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.SteamInterop;
using static TEKLauncher.App;
using static TEKLauncher.ARK.ModManager;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.UI.Message;

namespace TEKLauncher.Pages
{
    public partial class ModsPage : Page
    {
        public ModsPage() => InitializeComponent();
        private void InstallMod(object Sender, RoutedEventArgs Args)
        {
            if (Steam.IsSpacewarInstalled)
            {
                Instance.MWindow.ModInstallerMode = true;
                Instance.MWindow.PageFrame.Content = Instance.MWindow.ModInstallerPage = new ModInstallerPage();
            }
            else
                Show("Warning", LocString(LocCode.MINoSpacewar));
        }
        private async void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            await InitializeModsListAsync();
            foreach (Mod Mod in Mods)
                ModsList.Children.Add(new ModItem(Mod));
        }
        internal ModItem FindItem(Mod Mod)
        {
            foreach (ModItem Item in ModsList.Children)
                if (Item.Mod == Mod)
                    return Item;
            return null;
        }
    }
}