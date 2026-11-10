using System.Globalization;
using System.Windows;

namespace DK2MapEditor.App.Localization;

public static class LocalizationService
{
    public const string English = "en";
    public const string Russian = "ru";
    private const string DictionaryPrefix = "Localization/Strings.";
    private static readonly Dictionary<string, ResourceDictionary> lookupDictionaries = [];

    public static string CurrentLanguage { get; private set; } = English;
    public static event EventHandler? LanguageChanged;

    public static string GetDefaultLanguage() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals(Russian, StringComparison.OrdinalIgnoreCase)
            ? Russian
            : English;

    public static void SetLanguage(string? language)
    {
        var normalized = language?.Equals(Russian, StringComparison.OrdinalIgnoreCase) == true
            ? Russian
            : English;
        var resources = Application.Current.Resources.MergedDictionaries;
        var current = resources.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.Contains(DictionaryPrefix, StringComparison.OrdinalIgnoreCase) == true);
        var replacement = new ResourceDictionary
        {
            Source = new Uri($"/DK2MapEditor.App;component/{DictionaryPrefix}{normalized}.xaml", UriKind.Relative)
        };

        if (current is not null)
        {
            resources[resources.IndexOf(current)] = replacement;
        }
        else
        {
            resources.Insert(0, replacement);
        }

        CurrentLanguage = normalized;
        var culture = CultureInfo.GetCultureInfo(normalized == Russian ? "ru-RU" : "en-US");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string Get(string key, string? fallback = null) =>
        Application.Current.TryFindResource(key) as string ?? fallback ?? key;

    // Search both languages without changing UI resources or firing language
    // events. These dictionaries contain immutable translated strings.
    public static string GetForLanguage(string key, string language)
    {
        var normalized = language == Russian ? Russian : English;
        if (normalized == CurrentLanguage) return Get(key);
        if (!lookupDictionaries.TryGetValue(normalized, out var dictionary))
            lookupDictionaries.Add(normalized, dictionary = new ResourceDictionary { Source = new Uri($"/DK2MapEditor.App;component/{DictionaryPrefix}{normalized}.xaml", UriKind.Relative) });
        return dictionary[key] as string ?? key;
    }
}
