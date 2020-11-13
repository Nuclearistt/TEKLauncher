using System.Windows;
using System.Windows.Controls;
using static System.Windows.DependencyProperty;

namespace TEKLauncher.Controls
{
    public partial class MenuRadioButton : RadioButton
    {
        public MenuRadioButton() => InitializeComponent();
        public static readonly DependencyProperty SourceProperty = Register("Source", typeof(string), typeof(MenuRadioButton));
        public string Source { get => (string)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    }
}