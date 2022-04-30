using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TEKLauncher.Controls;

/// <summary>Main window notification control.</summary>
partial class Notification : UserControl
{
    /// <summary>Value indicating whether the notification has been hidden.</summary>
    bool _hidden;
    /// <summary>Value indicating whether the notification is prevented from automatically hiding itself after 7 seconds.</summary>
    bool _preventHiding;
    /// <summary>Creates a new notification with text message and icon.</summary>
    /// <param name="messageText">Message to be displayed in the notification.</param>
    /// <param name="iconName">Name of the icon to load.</param>
    public Notification(string messageText, string iconName)
    {
        InitializeComponent();
        Message.Text = messageText;
        Root.Orientation = Orientation.Horizontal;
        Root.Children.Insert(0, new ContentControl { Template = (ControlTemplate)FindResource(iconName), Width = 30, Margin = new(10, 0, 0, 0) });
    }
    /// <summary>Creates a new notification with text message and button.</summary>
    /// <param name="messageText">Message to be displayed in the notification.</param>
    /// <param name="buttonText">Text to be displayed on the button.</param>
    /// <param name="buttonHandler">Function to be executed when the button is clicked.</param>
    /// <param name="preventHiding">When <see langword="true"/>, prevents the notification from automatically hiding itself after 7 seconds.</param>
    public Notification(string messageText, string buttonText, Action buttonHandler, bool preventHiding)
    {
        _preventHiding = preventHiding;
        InitializeComponent();
        Message.Text = messageText;
        var button = new Button
        {
            Template = (ControlTemplate)FindResource("TextButton"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = buttonText
        };
        button.Click += delegate
        {
            buttonHandler();
            Hide();
        };
        Root.Children.Add(button);
    }
    /// <summary>Hides the notification and removes it from the stack.</summary>
    async void Hide()
    {
        _hidden = true;
        var ease = new QuadraticEase();
        var timeSpan = TimeSpan.FromMilliseconds(300);
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, timeSpan) { EasingFunction = ease });
        BeginAnimation(MarginProperty, new ThicknessAnimation(new(0, 0, -250, 0), timeSpan) { EasingFunction = ease });
        await Task.Delay(300);
        if (Parent is not null)
            ((StackPanel)Parent).Children.Remove(this);
    }
    /// <summary>Initiates hiding the notification.</summary>
    void Hide(object sender, RoutedEventArgs e) => Hide();
    /// <summary>Runs the task to hide the notification in 7 seconds after its creation, unless <see cref="_preventHiding"/> is <see langword="true"/>.</summary>
    void LoadedHandler(object sender, RoutedEventArgs e)
    {
        if (!_preventHiding)
            Task.Delay(7000).ContinueWith(delegate
            {
                if (!_hidden)
                    Dispatcher.Invoke(Hide);
            });
    }
}