namespace SunamoUiPreferences;

/// <summary>Language of an app: follows Windows (default) or is one of the languages the app supports; sets the UI culture and tells the app when it changed.</summary>
public sealed class AppLanguageService
{
    /// <summary>Choice that follows the display language of Windows.</summary>
    public const string FollowSystem = "System";

    /// <summary>Language used when Windows asks for none of the supported languages.</summary>
    public const string English = "en";

    private readonly UiPreferencesStore store;
    private readonly List<string> supportedLanguages;
    private readonly Func<IReadOnlyList<string>> preferredLanguagesProvider;
    private string? systemLanguage;
    private string currentLanguage;

    /// <summary>Creates the service; call <see cref="Initialize"/> at startup to apply the saved choice.</summary>
    /// <param name="store">Store that keeps the choice.</param>
    /// <param name="supportedLanguages">Languages the app has texts for, as two-letter codes (e.g. "cs", "en"); the order is the order of cycling.</param>
    /// <param name="log">Receives failures of reading the Windows display languages.</param>
    /// <param name="preferredLanguagesProvider">Source of the preferred culture names; the Windows display languages when omitted.</param>
    public AppLanguageService(UiPreferencesStore store, IEnumerable<string> supportedLanguages, Action<string, Exception?> log, Func<IReadOnlyList<string>>? preferredLanguagesProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(supportedLanguages);
        ArgumentNullException.ThrowIfNull(log);
        this.store = store;
        this.supportedLanguages = supportedLanguages.Select(ToLanguageCode).Distinct().ToList();
        if (this.supportedLanguages.Count == 0)
        {
            throw new ArgumentException("At least one supported language is required.", nameof(supportedLanguages));
        }

        foreach (var language in this.supportedLanguages)
        {
            // Throws CultureNotFoundException for a code that is not a language, before the app starts to rely on it.
            CultureInfo.CreateSpecificCulture(language);
        }

        this.preferredLanguagesProvider = preferredLanguagesProvider ?? (() => WindowsPreferredUiLanguages.Get(log));
        currentLanguage = FallbackLanguage;
    }

    /// <summary>Raised after the language changed by <see cref="Set"/>; the app refreshes its texts here.</summary>
    public event Action? Changed;

    /// <summary>Languages the app has texts for, as two-letter codes.</summary>
    public IReadOnlyList<string> SupportedLanguages => supportedLanguages;

    /// <summary>All choices in the order of cycling: <see cref="FollowSystem"/> and then the supported languages.</summary>
    public IReadOnlyList<string> Modes => new[] { FollowSystem }.Concat(supportedLanguages).ToList();

    /// <summary>Language used when Windows asks for none of the supported ones: English when supported, otherwise the first supported language.</summary>
    public string FallbackLanguage => supportedLanguages.Contains(English) ? English : supportedLanguages[0];

    /// <summary>The choice of the user: <see cref="FollowSystem"/> or one of the supported languages.</summary>
    public string Mode
    {
        get
        {
            var saved = store.Language;
            if (string.IsNullOrWhiteSpace(saved) || saved == FollowSystem)
            {
                return FollowSystem;
            }

            var language = ToLanguageCode(saved);
            return supportedLanguages.Contains(language) ? language : FollowSystem;
        }
    }

    /// <summary>Language the texts are shown in right now.</summary>
    public string Current => currentLanguage;

    /// <summary>Language Windows asks for: the first supported one of its display languages, otherwise <see cref="FallbackLanguage"/>.</summary>
    public string SystemLanguage => systemLanguage ??= DetectSystemLanguage();

    /// <summary>Applies the saved choice at startup; without one the language of Windows decides.</summary>
    public void Initialize()
    {
        systemLanguage ??= DetectSystemLanguage();
        ApplyCulture(Resolve());
    }

    /// <summary>Switches the language choice, saves it, sets the UI culture and raises <see cref="Changed"/>.</summary>
    /// <param name="mode"><see cref="FollowSystem"/> or one of the supported languages.</param>
    public void Set(string mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);
        if (mode != FollowSystem && !supportedLanguages.Contains(ToLanguageCode(mode)))
        {
            throw new ArgumentException("Language '" + mode + "' is not supported by the app.", nameof(mode));
        }

        store.Language = mode == FollowSystem ? FollowSystem : ToLanguageCode(mode);
        ApplyCulture(Resolve());
        Changed?.Invoke();
    }

    /// <summary>The choice that follows the current one when the user cycles through <see cref="Modes"/>.</summary>
    /// <returns>The next choice.</returns>
    public string NextMode()
    {
        var modes = Modes;
        var currentIndex = modes.ToList().IndexOf(Mode);
        return modes[(currentIndex + 1) % modes.Count];
    }

    /// <summary>Language to show for the current choice.</summary>
    private string Resolve()
    {
        var mode = Mode;
        return mode == FollowSystem ? SystemLanguage : mode;
    }

    /// <summary>First language of the preferred list that the app supports; the fallback when the list has none of them.</summary>
    private string DetectSystemLanguage()
    {
        foreach (var name in preferredLanguagesProvider())
        {
            var language = ToLanguageCode(name);
            if (supportedLanguages.Contains(language))
            {
                return language;
            }
        }

        return FallbackLanguage;
    }

    /// <summary>Sets the UI culture of this and of all later threads; number and date formats are left as they are.</summary>
    private void ApplyCulture(string language)
    {
        var culture = CultureInfo.CreateSpecificCulture(language);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        currentLanguage = language;
    }

    /// <summary>Two-letter language code of a language or culture name ("cs-CZ" gives "cs").</summary>
    private static string ToLanguageCode(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim().Split('-')[0].ToLowerInvariant();
    }
}
