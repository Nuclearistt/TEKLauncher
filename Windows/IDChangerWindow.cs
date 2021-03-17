using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TEKLauncher.ARK;
using TEKLauncher.UI;
using static System.IO.Directory;
using static System.Windows.DataObject;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.ARK.ModManager;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.SteamInterop.SteamworksAPI;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Windows
{
    public partial class IDChangerWindow : Window
    {
        internal IDChangerWindow()
        {
            InitializeComponent();
            if (LocCulture == "ar")
                StatusStack.FlowDirection = FlowDirection.RightToLeft;
        }
        private async void Change(object Sender, RoutedEventArgs Args)
        {
            if (Game.IsRunning)
            {
                SetStatus(LocString(LocCode.CantChangeSIDGameRunning), DarkRed);
                return;
            }
            NewID.IsReadOnly = OldID.IsReadOnly = true;
            ulong OID = ulong.Parse(OldID.Text), NID = ulong.Parse(NewID.Text);
            Mod Mod = Mods.Find(M => M.ID == OID);
            if (Mod is null)
            {
                SetStatus(LocString(LocCode.OIDNotFound), DarkRed);
                NewID.IsReadOnly = OldID.IsReadOnly = false;
                return;
            }
            if (!(Mods.Find(M => M.ID == NID) is null))
            {
                SetStatus(LocString(LocCode.NIDAlrInUse), DarkRed);
                NewID.IsReadOnly = OldID.IsReadOnly = false;
                return;
            }
            SetStatus(LocString(LocCode.SIDChangerRequesting), YellowBrush);
            ModDetails[] Response = await GetModsDetailsAsync(NID);
            ModDetails Details = (Response?.Length ?? 0) == 0 ? default : Response[0];
            if (Details.Status != 1 || Details.AppID != 480)
            {
                SetStatus(LocString(LocCode.SIDChangerReqFailed + Details.Status), DarkRed);
                NewID.IsReadOnly = OldID.IsReadOnly = false;
                return;
            }
            string OldPath = Mod.Path, OldModFilePath = Mod.ModFilePath, OldModsPath = Mod.ModsPath;
            Mod.Path = Mod.Path.Replace(OID.ToString(), NID.ToString());
            Mod.ModFilePath = $"{Mod.ModsPath = $@"{Game.Path}\ShooterGame\Content\Mods\{Mod.ID = NID}"}.mod";
            Move(OldPath, Mod.Path);
            if (FileExists(OldModFilePath))
                File.Move(OldModFilePath, Mod.ModFilePath);
            if (Exists(OldModsPath))
                Move(OldModsPath, Mod.ModsPath);
            if (!(Mod.ImageFile is null))
                Mod.ImageFile = Mod.ImageFile.Replace(OID.ToString(), NID.ToString());
            Mod.Details = Details;
            if (await TryDeployAsync())
            {
                await SteamAPI.UnsubscribeModAsync(OID);
                if ((Mod.IsSubscribed = (await SteamAPI.SubscribeModAsync(NID)) ? (bool?)true : null) is null)
                {
                    Message.Show("Info", LocString(LocCode.FailedToSub));
                    Execute($"{SteamWorkshop}{NID}");
                }
            }
            else
            {
                Mod.IsSubscribed = null;
                Message.Show("Info", LocString(LocCode.FailedToSub));
                Execute($"{SteamWorkshop}{NID}");
            }
            NewID.IsReadOnly = OldID.IsReadOnly = false;
            SetStatus(LocString(LocCode.SIDChangerSuccess), DarkGreen);
        }
        private void Close(object Sender, RoutedEventArgs Args) => Close();
        private void CopyingHandler(object Sender, DataObjectCopyingEventArgs Args)
        {
            if (Args.IsDragDrop)
                Args.CancelCommand();
        }
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
        private void Minimize(object Sender, RoutedEventArgs Args) => WindowState = WindowState.Minimized;
        private void PastingHandler(object Sender, DataObjectPastingEventArgs Args)
        {
            if (((string)Args.DataObject.GetData(typeof(string))).Any(Symbol => !char.IsDigit(Symbol)))
                Args.CancelCommand();
        }
        private void SetStatus(string Text, SolidColorBrush Color)
        {
            Status.Foreground = Color;
            Status.Text = Text;
        }
        private void TextBoxLoadedHandler(object Sender, RoutedEventArgs Args)
        {
            AddCopyingHandler(OldID, CopyingHandler);
            AddCopyingHandler(NewID, CopyingHandler);
            AddPastingHandler(OldID, PastingHandler);
            AddPastingHandler(NewID, PastingHandler);
        }
        private void TextChangedHandler(object Sender, TextChangedEventArgs Args) => ChangeButton.IsEnabled = (OldID.Text?.Length ?? 0) > 0 && (NewID.Text?.Length ?? 0) > 0;
        private void TextInputHandler(object Sender, TextCompositionEventArgs Args) => Args.Handled = !char.IsDigit(Args.Text[0]);
    }
}