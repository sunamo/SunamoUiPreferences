namespace SunamoUiPreferences;

/// <summary>Reads the app mode of Windows (Settings, Personalization, Colors).</summary>
public static class WindowsAppMode
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    /// <summary>True when Windows is set to the dark app mode; false for the light mode and when the mode cannot be read.</summary>
    /// <param name="log">Receives the reason when the mode could not be read.</param>
    /// <returns>True for the dark app mode.</returns>
    public static bool UsesDarkApps(Action<string, Exception?> log)
    {
        ArgumentNullException.ThrowIfNull(log);
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            if (key?.GetValue(AppsUseLightThemeValueName) is int value)
            {
                return value == 0;
            }

            log("The Windows app mode is not set in the registry, the light theme is used.", null);
        }
        catch (Exception ex)
        {
            log("The Windows app mode could not be read, the light theme is used.", ex);
        }

        return false;
    }
}
