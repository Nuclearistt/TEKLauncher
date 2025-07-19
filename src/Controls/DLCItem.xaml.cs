using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using TEKLauncher.Windows;

namespace TEKLauncher.Controls;

/// <summary>Represents a DLC in DLC tab GUI.</summary>
partial class DLCItem : UserControl
{
    /// <summary>Single animation object for animating content grid's opacity and blur radius.</summary>
    readonly DoubleAnimation _animation = new(1, TimeSpan.FromMilliseconds(500)) { EasingFunction = new QuadraticEase() };
    /// <summary>Initializes a new item to represent specified DLC.</summary>
    /// <param name="dlc">DLC that will be represented by the item.</param>
    internal DLCItem(DLC dlc)
    {
        InitializeComponent();
        DataContext = dlc;
        Image.Source = new BitmapImage(new($"pack://application:,,,/res/img/DLC/{dlc.Code}.jpg"));
        SetStatus();
        dlc.Item = this;
    }
    /// <summary>Prompts uninstall of the DLC.</summary>
    void Delete(object sender, RoutedEventArgs e)
    {
        var dlc = (DLC)DataContext;
        if (Messages.ShowOptions("Warning", string.Format(LocManager.GetString(LocCode.DLCDeletePrompt), dlc.Name)))
            Task.Run(dlc.Delete);
    }
    /// <summary>Creates an <see cref="UpdaterWindow"/> for installing/updating the DLC.</summary>
    void Install(object sender, RoutedEventArgs e)
	{
		if (TEKSteamClient.Ctx == null)
		{
			Messages.Show("Error", "tek-steamclient library hasn't been downloaded yet, try again later");
			return;
		}
		new UpdaterWindow((DLC)DataContext, false).Show();
    }
    /// <summary>Triggers an animation to make content grid visible.</summary>
    void MouseEnterHandler(object sender, MouseEventArgs e)
    {
        _animation.To = 1;
        Root.BeginAnimation(OpacityProperty, _animation);
        _animation.To = 5;
        Blur.BeginAnimation(BlurEffect.RadiusProperty, _animation);
    }
    /// <summary>Triggers an animation to make content grid invisible.</summary>
    void MouseLeaveHandler(object sender, MouseEventArgs e)
    {
        _animation.To = 0;
        Root.BeginAnimation(OpacityProperty, _animation);
        Blur.BeginAnimation(BlurEffect.RadiusProperty, _animation);
    }
    /// <summary>Creates an <see cref="UpdaterWindow"/> for validating the DLC.</summary>
    void Validate(object sender, RoutedEventArgs e)
	{
		if (TEKSteamClient.Ctx == null)
		{
			Messages.Show("Error", "tek-steamclient library hasn't been downloaded yet, try again later");
			return;
		}
		new UpdaterWindow((DLC)DataContext, true).Show();
    }
    /// <summary>Updates the item accordingly to new DLC status.</summary>
    internal void SetStatus()
    {
        var status = ((DLC)DataContext).CurrentStatus;
        Status.Foreground = status switch
        {
            DLC.Status.NotInstalled => new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13)),
            DLC.Status.Installed => new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E)),
            _ => Brushes.Yellow
        };
        Status.Text = LocManager.GetString(status switch
        {
            DLC.Status.NotInstalled => LocCode.NotInstalled,
            DLC.Status.Installed => LocCode.Installed,
            DLC.Status.CheckingForUpdates => LocCode.CheckingForUpdates,
            DLC.Status.UpdateAvailable => LocCode.UpdateAvailable,
            DLC.Status.Updating => LocCode.Updating,
            DLC.Status.Deleting => LocCode.Deleting,
            _ => (LocCode)status
        });
        NameBlock.Foreground = status == DLC.Status.UpdateAvailable ? Brushes.Yellow : new SolidColorBrush(Color.FromRgb(0x9F, 0xD6, 0xD2));
        InstallButton.Visibility = (int)status % 3 == 0 ? Visibility.Visible : Visibility.Collapsed;
        DeleteButton.Visibility = ValidateButton.Visibility = status == DLC.Status.Installed || status == DLC.Status.UpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
    }
}