using System;
using System.Windows.Markup;
using static System.Enum;
using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.UI
{
    internal class LocStringExtension : MarkupExtension
    {
        public LocStringExtension(string Code) => this.Code = (LocCode)Parse(typeof(LocCode), Code);
        private readonly LocCode Code;
        public override object ProvideValue(IServiceProvider Provider) => LocString(Code);
    }
}