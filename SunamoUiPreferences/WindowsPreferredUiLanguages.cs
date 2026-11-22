namespace SunamoUiPreferences;

/// <summary>Reads the display languages the user has set in Windows.</summary>
public static class WindowsPreferredUiLanguages
{
    private const uint MuiLanguageName = 0x8;

    /// <summary>Display languages of Windows as culture names (e.g. "cs-CZ"), in order of preference; falls back to the UI culture of the process.</summary>
    /// <param name="log">Receives the reason when the list could not be read.</param>
    /// <returns>At least one culture name.</returns>
    public static IReadOnlyList<string> Get(Action<string, Exception?> log)
    {
        ArgumentNullException.ThrowIfNull(log);
        try
        {
            uint size = 0;
            if (GetUserPreferredUILanguages(MuiLanguageName, out _, null, ref size) && size > 0)
            {
                var buffer = new char[size];
                if (GetUserPreferredUILanguages(MuiLanguageName, out _, buffer, ref size))
                {
                    var names = new string(buffer).Split('\0', StringSplitOptions.RemoveEmptyEntries);
                    if (names.Length > 0)
                    {
                        return names;
                    }
                }
            }

            log("Preferred UI languages of Windows could not be read (error " + Marshal.GetLastWin32Error() + "), the UI culture of the process is used.", null);
        }
        catch (Exception ex)
        {
            log("Reading the preferred UI languages of Windows failed, the UI culture of the process is used.", ex);
        }

        return new[] { CultureInfo.CurrentUICulture.Name };
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetUserPreferredUILanguages(uint flags, out uint languageCount, char[]? languages, ref uint bufferLength);
}
