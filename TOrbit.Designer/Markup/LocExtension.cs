using Avalonia.Data;
using Avalonia.Markup.Xaml;
using TOrbit.Designer.Services;

namespace TOrbit.Designer.Markup;

public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var source = LocalizationService.Shared;
        return new Binding
        {
            Source = source,
            Path = $"[{Key}]",
            Mode = BindingMode.OneWay
        };
    }
}
