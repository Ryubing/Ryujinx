using Avalonia.Markup.Xaml.MarkupExtensions;
using Projektanker.Icons.Avalonia;
using Ryujinx.Ava.Common.Locale;

namespace Ryujinx.Ava.Common.Markup
{
    internal class IconExtension(string iconString) : BasicMarkupExtension<Icon>
    {
        public override string Name => "Icon";
        protected override Icon Value => new() { Value = iconString };
    }
    
    internal class SpinningIconExtension(string iconString) : BasicMarkupExtension<Icon>
    {
        public override string Name => "SIcon";
        protected override Icon Value => new() { Value = iconString, Animation = IconAnimation.Spin };
    }
    
    internal class LocaleExtension(LocaleKeys key) : BasicMarkupExtension<string>
    {
        public override string Name => "Translation";
        protected override string Value => LocaleManager.Instance[key];

        protected override void ConfigureBindingExtension(CompiledBindingExtension bindingExtension) 
            => bindingExtension.Source = LocaleManager.Instance;
    }
    
    internal class WindowTitleExtension(LocaleKeys key, bool includeVersion) : BasicMarkupExtension<string>
    {
        public WindowTitleExtension(LocaleKeys key) : this(key, true)
        {
        }
        
        public override string Name => "WindowTitleTranslation";
        protected override string Value => RyujinxApp.FormatTitle(key, includeVersion);

        protected override void ConfigureBindingExtension(CompiledBindingExtension bindingExtension) 
            => bindingExtension.Source = LocaleManager.Instance;
    }
}
