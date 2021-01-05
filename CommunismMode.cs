using System.IO;
using System.Media;
using TEKLauncher.Controls;
using TEKLauncher.Net;
using TEKLauncher.Windows;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.UI.Notifications;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher
{
    internal static class CommunismMode
    {
        private static MainWindow MWindow;
        private static Notification PreparingNotification;
        private static SoundPlayer Player;
        internal static bool IsImageAvailable = false;
        private static readonly string AnthemPath = $@"{AppDataFolder}\Communism\Anthem.wav";
        internal static readonly string ImagePath = $@"{AppDataFolder}\Communism\Image.jpg";
        private static void CreateNotification()
        {
            if (PreparingNotification is null)
                PreparingNotification = AddLoading($"{LocString(LocCode.PrepCommunismMode)} ");
        }
        private static void HideNotification()
        {
            PreparingNotification?.Hide();
            PreparingNotification = null;
        }
        internal static async void Set(bool State)
        {
            MWindow = Instance.MWindow;
            MWindow.TitleBlock.Text = MWindow.Title = State ? "Lenin Launcher" : "TEK Launcher";
            MWindow.Menu.Communism = State;
            if (State)
            {
                Directory.CreateDirectory($@"{AppDataFolder}\Communism");
                if (GetFileSize(AnthemPath) != 4552936L)
                {
                    CreateNotification();
                    if (!await new Downloader().TryDownloadFileAsync(AnthemPath, CA))
                    {
                        Current.Dispatcher.Invoke(HideNotification);
                        return;
                    }
                }
                if (GetFileSize(ImagePath) != 413348L)
                {
                    CreateNotification();
                    if (!await new Downloader().TryDownloadFileAsync(ImagePath, CI))
                    {
                        Current.Dispatcher.Invoke(HideNotification);
                        return;
                    }
                }
                Current.Dispatcher.Invoke(HideNotification);
                try { (Player = new SoundPlayer(AnthemPath)).Play(); }
                catch { }
                IsImageAvailable = true;
            }
            else
            {
                Current.Dispatcher.Invoke(HideNotification);
                Player?.Stop();
                IsImageAvailable = false;
            }
        }
    }
}