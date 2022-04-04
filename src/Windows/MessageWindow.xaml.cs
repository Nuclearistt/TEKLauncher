using System.Windows.Controls;

namespace TEKLauncher.Windows;

/// <summary>Window that displays error, warning and info messages.</summary>
partial class MessageWindow : TEKWindow
{
    /// <summary>Initializes a new Message window with specified parameters.</summary>
    /// <param name="type">Type of the message as well as name of the icon and title.</param>
    /// <param name="message">Message text displayed in the window.</param>
    /// <param name="twoOptions">Indicates whether the window will have two option buttons (Yes and No) or only OK button.</param>
    public MessageWindow(string type, string message, bool twoOptions)
    {
        InitializeComponent();
        Title = LocManager.GetString(Enum.Parse<LocCode>(type));
        MessageIcon.Template = (ControlTemplate)FindResource(type);
        Message.Text = message;
        if (twoOptions)
        {
            var yesButton = new Button
            {
                MinWidth = 70,
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Left,
                Content = LocManager.GetString(LocCode.Yes)
            };
            var noButton = new Button
            {
                MinWidth = 70,
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = LocManager.GetString(LocCode.No),
                Tag = 1
            };
            yesButton.Click += Close;
            noButton.Click += Close;
            Grid.SetRow(yesButton, 1);
            Grid.SetRow(noButton, 1);
            ContentGrid.Children.Add(yesButton);
            ContentGrid.Children.Add(noButton);
        }
        else
        {
            var okButton = new Button
            {
                MinWidth = 70,
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = LocManager.GetString(LocCode.OK)
            };
            okButton.Click += Close;
            Grid.SetRow(okButton, 1);
            ContentGrid.Children.Add(okButton);
        }
    }
    /// <summary>Saves dialog result and closes the window.</summary>
    void Close(object sender, RoutedEventArgs e)
    {
        DialogResult = ((Button)sender).Tag is null;
        Close();
    }
}