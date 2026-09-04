using System.Windows;

namespace PulseInspector.Wpf;

public enum AppTheme
{
    Light,
    Dark
}

internal static class ThemeManager
{
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public static void Apply(AppTheme theme)
    {
        var application = System.Windows.Application.Current;
        if (application is null) return;

        var dictionaries = application.Resources.MergedDictionaries;
        foreach (var dictionary in dictionaries.ToArray())
        {
            var source = dictionary.Source?.OriginalString;
            if (source is not null && (source.EndsWith("Themes/Colors.xaml", StringComparison.OrdinalIgnoreCase) ||
                                       source.EndsWith("Themes/DarkColors.xaml", StringComparison.OrdinalIgnoreCase)))
            {
                dictionaries.Remove(dictionary);
            }
        }

        var sourceUri = theme == AppTheme.Dark ? "Themes/DarkColors.xaml" : "Themes/Colors.xaml";
        dictionaries.Insert(0, new ResourceDictionary { Source = new Uri(sourceUri, UriKind.Relative) });
        CurrentTheme = theme;
    }
}
