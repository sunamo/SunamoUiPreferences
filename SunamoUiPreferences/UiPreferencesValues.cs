namespace SunamoUiPreferences;

/// <summary>Content of the settings file written by <see cref="UiPreferencesStore"/>.</summary>
internal sealed class UiPreferencesValues
{
    /// <summary>Saved theme choice; null when the user has not chosen yet.</summary>
    public string? Theme { get; set; }

    /// <summary>Saved language choice; null when the user has not chosen yet.</summary>
    public string? Language { get; set; }
}
