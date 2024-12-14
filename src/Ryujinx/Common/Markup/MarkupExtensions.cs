using Avalonia.Markup.Xaml.MarkupExtensions;
using Projektanker.Icons.Avalonia;
using Ryujinx.Ava.Common.Locale;

namespace Ryujinx.Ava.Common.Markup
{
    internal class IconExtension(string iconString) : BasicMarkupExtension<Icon>
    {
        protected override Icon Value => new() { Value = iconString };
    }
    
    internal class SpinningIconExtension(string iconString) : BasicMarkupExtension<Icon>
    {
        protected override Icon Value => new() { Value = iconString, Animation = IconAnimation.Spin };
    }
    
    internal class LocaleExtension(LocaleKeys key) : BasicMarkupExtension<string>
    {
        protected override string Value => LocaleManager.Instance[key];

        protected override void ConfigureBindingExtension(CompiledBindingExtension bindingExtension) 
            => bindingExtension.Source = LocaleManager.Instance;
    }
}
