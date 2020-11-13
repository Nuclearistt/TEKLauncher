using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using static System.Windows.DependencyProperty;
using static System.Windows.Media.Color;

namespace TEKLauncher.Controls
{
    public partial class VectorImage : UserControl
    {
        public VectorImage() => InitializeComponent();
        public static readonly DependencyProperty SourceProperty = Register("Source", typeof(string), typeof(VectorImage));
        public string Source
        {
            get => (string)GetValue(SourceProperty);
            set
            {
                SetValue(SourceProperty, value);
                Image.Child = (UIElement)Resources.MergedDictionaries[0][value];
                if (Image.Child is Panel Canvas)
                    foreach (Shape Child in Canvas.Children)
                    {
                        if (Child.Stroke is null)
                            Child.SetBinding(Shape.StrokeProperty, new Binding("Foreground") { Source = this });
                    }
                else if (((Shape)Image.Child).Stroke is null)
                    ((FrameworkElement)Image.Child).SetBinding(Shape.StrokeProperty, new Binding("Foreground") { Source = this });
            }
        }
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            Color ImageColor = ((SolidColorBrush)Foreground).Color;
            if (ImageColor.R == 0 && ImageColor.G == 0 && ImageColor.B == 0)
                Foreground = new SolidColorBrush(FromRgb(0xFF, 0xA0, 0));
            Source = Source;
        }
    }
}