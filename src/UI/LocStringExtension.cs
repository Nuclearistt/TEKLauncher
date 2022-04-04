using System.Windows.Markup;

namespace TEKLauncher.UI;

/// <summary>Markup extension for extracting localized strings in XAML by their <see cref="LocCode"/>.</summary>
class LocStringExtension : MarkupExtension
{
    /// <summary>Identifier of the localized string.</summary>
    readonly LocCode _code;
    /// <summary>Initializes a new instance of <see cref="LocStringExtension"/> with specified localized string code.</summary>
    /// <param name="code">String representation of localized string identifier.</param>
    public LocStringExtension(string code) => _code = Enum.Parse<LocCode>(code);
    public override object ProvideValue(IServiceProvider serviceProvider) => LocManager.GetString(_code);
}