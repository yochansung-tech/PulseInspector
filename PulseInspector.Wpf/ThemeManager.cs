using System.Windows;

namespace PulseInspector.Wpf;

public enum AppTheme
{
    Light,
    Dark
}

internal static class ThemeManager
{
    private const string ThemeDictionaryKey = "ActiveTheme";

    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public static void Apply(AppTheme theme)
    {
        var application = Application.Current;
        if (application is null) return;

        var dictionaries = application.Resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(d => string.Equals(d[ThemeDictionaryKey] as string, "true", StringComparison.Ordinal));
        if (existing is not null) dictionaries.Remove(existing);

        var source = theme == AppTheme.Dark ? "Themes/DarkColors.xaml" : "Themes/Colors.xaml";
        var dictionary = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };
        dictionary[ThemeDictionaryKey] = "true";
        dictionaries.Insert(0, dictionary);
        CurrentTheme = theme;
    }
}
