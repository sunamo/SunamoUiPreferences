namespace SunamoUiPreferences;

/// <summary>The two UI choices of the user that survive a restart (theme, language), stored as JSON in a folder given by the app.</summary>
public sealed class UiPreferencesStore
{
    /// <summary>Name of the settings file inside the settings folder.</summary>
    public const string FileName = "ui-settings.json";

    private readonly Action<string, Exception?> log;
    private readonly object syncRoot = new();
    private UiPreferencesValues? values;

    /// <summary>Creates the store over a settings folder; the file is read on first use.</summary>
    /// <param name="settingsFolder">Folder of the settings file, e.g. the Settings folder of the app from SunamoPlatformUwpInterop.</param>
    /// <param name="log">Receives every read or write failure with its exception, and the note that the file does not exist yet.</param>
    public UiPreferencesStore(string settingsFolder, Action<string, Exception?> log)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFolder);
        ArgumentNullException.ThrowIfNull(log);
        FilePath = Path.Combine(settingsFolder, FileName);
        this.log = log;
    }

    /// <summary>Full path of the settings file.</summary>
    public string FilePath { get; }

    /// <summary>Saved theme choice; null when the user has not chosen yet. Setting it writes the file at once.</summary>
    public string? Theme
    {
        get => Values.Theme;
        set
        {
            Values.Theme = value;
            Save();
        }
    }

    /// <summary>Saved language choice; null when the user has not chosen yet. Setting it writes the file at once.</summary>
    public string? Language
    {
        get => Values.Language;
        set
        {
            Values.Language = value;
            Save();
        }
    }

    /// <summary>Values of the settings file, read once.</summary>
    private UiPreferencesValues Values
    {
        get
        {
            lock (syncRoot)
            {
                return values ??= Load();
            }
        }
    }

    /// <summary>Reads the settings file; a missing or unreadable file gives empty settings.</summary>
    private UiPreferencesValues Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<UiPreferencesValues>(File.ReadAllText(FilePath)) ?? new UiPreferencesValues();
            }

            log("UI settings file does not exist yet, defaults are used: " + FilePath, null);
        }
        catch (Exception ex)
        {
            log("UI settings could not be read from " + FilePath + ", defaults are used.", ex);
        }

        return new UiPreferencesValues();
    }

    /// <summary>Writes the settings file right after a change.</summary>
    private void Save()
    {
        try
        {
            lock (syncRoot)
            {
                var folder = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllText(FilePath, JsonSerializer.Serialize(Values));
            }
        }
        catch (Exception ex)
        {
            log("UI settings could not be saved to " + FilePath, ex);
        }
    }
}
