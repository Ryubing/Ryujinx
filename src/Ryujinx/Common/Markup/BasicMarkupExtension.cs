using Avalonia.Data.Core;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Gommon;
using System;
// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

#nullable enable

namespace Ryujinx.Ava.Common.Markup
{
    internal abstract class BasicMarkupExtension<T> : MarkupExtension
    {
        public abstract string Name { get; }
        public virtual Action<object, T?>? Setter => null;

        protected abstract T? Value { get; }

        protected virtual void ConfigureBindingExtension(CompiledBindingExtension _) { }

        private ClrPropertyInfo PropertyInfo =>
            new(Name,
                _ => Value,
                Setter as Action<object, object?>,
                typeof(T));

        public override object ProvideValue(IServiceProvider serviceProvider) 
            => new CompiledBindingExtension(
                    new CompiledBindingPathBuilder()
                        .Property(PropertyInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
                        .Build()
                    )
                .Apply(ConfigureBindingExtension)
                .ProvideValue(serviceProvider);
    }
}
