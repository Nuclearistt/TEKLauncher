using System.Windows;
using System.Windows.Controls;
using static System.Enum;
using static System.Windows.DependencyProperty;
using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.Controls
{
    public partial class MenuRadioButton : RadioButton
    {
        public MenuRadioButton() => InitializeComponent();
        public static readonly DependencyProperty SourceProperty = Register("Source", typeof(string), typeof(MenuRadioButton));
        public string Source { get => (string)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
        private void LoadedHandler(object Sender, RoutedEventArgs Args) => NameBlock.Text = LocString((LocCode)Parse(typeof(LocCode), $"{Name}Page"));
    }
}