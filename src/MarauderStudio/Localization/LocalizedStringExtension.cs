using System.Windows.Markup;

namespace MarauderStudio.Localization;

/// <summary>
/// Markup-расширение {loc:LocalizedString Key.Foo}. Возвращает локализованную строку.
/// Для обновления UI при смене языка вызывайте LocalizationService.RefreshUi():
/// MainWindow перезагрузит текущую страницу.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocalizedStringExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocalizedStringExtension(string key) => Key = key;

    public LocalizedStringExtension() { }

    public override object ProvideValue(IServiceProvider serviceProvider)
        => LocalizationService.Get(Key);
}
