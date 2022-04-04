using System.Windows.Controls;
using System.Windows.Media;

namespace TEKLauncher.Windows;

/// <summary>Window that displays launcher changelog.</summary>
partial class WhatsNewWindow : TEKWindow
{
    /// <summary>Initializes a new What's new window.</summary>
    public WhatsNewWindow()
    {
        InitializeComponent();
        using var resourceStream = Application.GetResourceStream(new("pack://application:,,,/res/Changelog.txt")).Stream;
        using var reader = new StreamReader(resourceStream);
        for (string? version; (version = reader.ReadLine()) is not null;)
        {
            Root.Children.Add(new Border
            {
                Margin = new(0, 10, 0, 10),
                Padding = new(5, 0, 5, 0),
                Background = new SolidColorBrush(Color.FromArgb(0x80, 0x21, 0x25, 0x2B)),
                BorderBrush = Brushes.Gray,
                BorderThickness = new(2),
                HorizontalAlignment = HorizontalAlignment.Center,
                CornerRadius = new(10),
                Child = new TextBlock
                { 
                    Foreground = new SolidColorBrush(Color.FromRgb(0xC5, 0xD4, 0xE3)),
                    FontSize = 24,
                    Text = version
                }
            });
            for (int i = int.Parse(reader.ReadLine()!); i > 0; i--)
            {
                Root.Children.Add(new TextBlock
                {
                    Margin = new(20, 0, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0xD0, 0xE0)),
                    Text = reader.ReadLine()
                });
                var builder = new StringBuilder();
                for (int j = int.Parse(reader.ReadLine()!); j > 0; j--)
                    builder.AppendLine(reader.ReadLine());
                Root.Children.Add(new TextBlock
                {
                    Margin = new(50, 0, 0, 0),
                    FontSize = 18,
                    TextWrapping = TextWrapping.Wrap,
                    Text = builder.ToString()
                });
            }
        }
    }
}