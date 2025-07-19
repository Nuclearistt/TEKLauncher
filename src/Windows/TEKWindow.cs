using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace TEKLauncher.Windows;

/// <summary>Base class for TEK Launcher windows, supplying common elements like caption and control buttons.</summary>
public class TEKWindow : Window
{
    /// <summary>Caption border.</summary>
    Border _caption = null!;
    /// <summary>Maximize/restore button.</summary>
    Button? _maximizeRestoreButton;
    /// <summary>Root grid of the window.</summary>
    Grid _root = null!;
    /// <summary>Initializes a new TEK window.</summary>
    public TEKWindow()
    {
        Loaded += LoadedHandler;
        StateChanged += StateChangedHandler;
    }
    /// <summary>Closes the window.</summary>
    void Close(object sender, RoutedEventArgs e) => Close();
    /// <summary>Initializes window components and executes opening effect.</summary>
    void LoadedHandler(object sender, RoutedEventArgs e)
    {
        _caption = (Border)Template.FindName("Caption", this);
        _root = (Grid)Template.FindName("Root", this);
        var controlButtons = ((StackPanel)Template.FindName("ControlButtons", this)).Children;
        var contentBox = (ContentPresenter)Template.FindName("ContentBox", this);
        ((Button)controlButtons[0]).Click += Minimize;
        ((Button)controlButtons[2]).Click += Close;
        if (this is MainWindow)
        {
            _maximizeRestoreButton = (Button)controlButtons[1];
            _maximizeRestoreButton.Click += MaximizeRestore;
            WindowChrome.GetWindowChrome(this).ResizeBorderThickness = new(10);
        }
        else
            controlButtons.RemoveAt(1);
        WinAPI.SetWindowStyles(this, this is MainWindow);
        if (this is MessageWindow)
        {
            //Simplified opening effect (opacity animation only)
            Opacity = 0;
            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(400)) { EasingFunction = new QuadraticEase() });
            return;
        }
        //Opening effect
        contentBox.Opacity = _caption.Opacity = 0;
        var grid = new Grid();
        var effectBorder = new Border { Background = new SolidColorBrush(Colors.Transparent), CornerRadius = new(8), Child = grid };
        Grid.SetRowSpan(effectBorder, 2);
        _root.Children.Insert(0, effectBorder);
        var span = TimeSpan.FromMilliseconds(700);
        Span<byte> randomColors = stackalloc byte[30];
        Random.Shared.NextBytes(randomColors);
        effectBorder.Background.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Color.FromArgb(0x80, 0x32, 0x65, 0x6E), span));
        double diameter = Math.Min(ActualWidth, ActualHeight) - 5;
        var animation = new DoubleAnimation(diameter, span) { EasingFunction = new QuadraticEase() };
        var colorAnimation = new ColorAnimation(Colors.Transparent, span);
        for (int i = 0; i < 5; i++)
        {
            int colorIndex = i * 6;
            var circle = new Ellipse { Width = 15, Height = 15, Stroke = new SolidColorBrush(Color.FromRgb(randomColors[colorIndex], randomColors[++colorIndex], randomColors[++colorIndex])), StrokeThickness = 5 };
            grid.Children.Add(circle);
            animation.BeginTime = TimeSpan.FromMilliseconds(100 * i);
            circle.BeginAnimation(WidthProperty, animation);
            circle.BeginAnimation(HeightProperty, animation);
            colorAnimation.To = Color.FromRgb(randomColors[++colorIndex], randomColors[++colorIndex], randomColors[++colorIndex]);
            circle.Stroke.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }
        animation.To = 1;
        animation.Duration = TimeSpan.FromMilliseconds(500);
        animation.BeginTime = span;
        animation.EasingFunction = null;
        _caption.BeginAnimation(OpacityProperty, animation);
        contentBox.BeginAnimation(OpacityProperty, animation);
        Task.Delay(1200).ContinueWith(t => Dispatcher.Invoke(delegate
        {
            grid.Children.Clear();
            effectBorder.Child = null;
            _root.Children.RemoveAt(0);
        }));
    }
    /// <summary>Maximizes or restores the window.</summary>
    void MaximizeRestore(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    /// <summary>Minimizes the window.</summary>
    void Minimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    /// <summary>Processes maximize/restore state changes.</summary>
    protected virtual void StateChangedHandler(object? sender, EventArgs e)
    {
        if (_root is null || _caption is null)
        {
            return;
        }
        if (WindowState == WindowState.Normal)
        {
            _root.Margin = new(0);
            _caption.CornerRadius = new(8, 8, 0, 0);
            if (_maximizeRestoreButton is not null)
                _maximizeRestoreButton.Tag = "2"; //Return icon to Maximize one
        }
        else if (WindowState == WindowState.Maximized)
        {
            _root.Margin = new(8); //Fix window content going out of screen in maximized mode
            _caption.CornerRadius = new(0);
            if (_maximizeRestoreButton is not null)
                _maximizeRestoreButton.Tag = "3"; //Set Restore icon
		}
    }
}