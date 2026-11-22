namespace SunamoUiPreferences;

/// <summary>Theme choice of an app: follows the Windows app mode (default) or is light or dark; knows no UI library, the app applies the resolved theme itself.</summary>
public sealed class AppThemeChoiceService : IDisposable
{
    /// <summary>Choice that follows the app mode of Windows.</summary>
    public const string FollowSystem = "System";

    /// <summary>Light theme.</summary>
    public const string Light = "Light";

    /// <summary>Dark theme.</summary>
    public const string Dark = "Dark";

    private readonly UiPreferencesStore store;
    private readonly Func<bool> systemUsesDarkProvider;
    private string? lastResolved;
    private bool isWatchingSystem;

    /// <summary>Creates the service over the store that keeps the choice.</summary>
    /// <param name="store">Store that keeps the choice.</param>
    /// <param name="log">Receives failures of reading the Windows app mode.</param>
    /// <param name="systemUsesDarkProvider">Source of the system mode (true for dark); the Windows app mode when omitted.</param>
    public AppThemeChoiceService(UiPreferencesStore store, Action<string, Exception?> log, Func<bool>? systemUsesDarkProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(log);
        this.store = store;
        this.systemUsesDarkProvider = systemUsesDarkProvider ?? (() => WindowsAppMode.UsesDarkApps(log));
    }

    /// <summary>Raised after the choice changed by <see cref="Set"/>; the app applies <see cref="Resolve"/> to its UI library here.</summary>
    public event Action? Changed;

    /// <summary>Raised when the Windows app mode changed while the app follows Windows. Comes from a system thread, so the handler must switch to the UI thread.</summary>
    public event Action? SystemChanged;

    /// <summary>All choices in the order of cycling.</summary>
    public static IReadOnlyList<string> Modes { get; } = new[] { FollowSystem, Light, Dark };

    /// <summary>The choice of the user: <see cref="FollowSystem"/>, <see cref="Light"/> or <see cref="Dark"/>.</summary>
    public string Mode => store.Theme is Light or Dark ? store.Theme : FollowSystem;

    /// <summary>True when the theme to show is dark.</summary>
    public bool IsDark => Resolve() == Dark;

    /// <summary>Theme to show for the current choice; the result is remembered to detect a later change of the Windows app mode.</summary>
    /// <returns><see cref="Light"/> or <see cref="Dark"/>.</returns>
    public string Resolve()
    {
        lastResolved = Mode switch
        {
            Dark => Dark,
            Light => Light,
            _ => systemUsesDarkProvider() ? Dark : Light,
        };
        return lastResolved;
    }

    /// <summary>Switches the theme choice, saves it and raises <see cref="Changed"/>.</summary>
    /// <param name="mode"><see cref="FollowSystem"/>, <see cref="Light"/> or <see cref="Dark"/>.</param>
    public void Set(string mode)
    {
        if (!Modes.Contains(mode))
        {
            throw new ArgumentException("Theme choice '" + mode + "' is not one of System, Light, Dark.", nameof(mode));
        }

        store.Theme = mode;
        Resolve();
        Changed?.Invoke();
    }

    /// <summary>The choice that follows the current one when the user cycles through the three options.</summary>
    /// <returns>The next choice.</returns>
    public string NextMode()
    {
        return Mode switch
        {
            FollowSystem => Light,
            Light => Dark,
            _ => FollowSystem,
        };
    }

    /// <summary>Starts to watch the Windows app mode; <see cref="SystemChanged"/> is raised while the app follows Windows.</summary>
    public void StartWatchingSystem()
    {
        if (isWatchingSystem)
        {
            return;
        }

        lastResolved ??= Resolve();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        isWatchingSystem = true;
    }

    /// <summary>Stops to watch the Windows app mode.</summary>
    public void StopWatchingSystem()
    {
        if (!isWatchingSystem)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        isWatchingSystem = false;
    }

    /// <summary>Compares the Windows app mode with the last resolved theme and raises <see cref="SystemChanged"/> when the app follows Windows and the theme differs.</summary>
    /// <returns>True when <see cref="SystemChanged"/> was raised.</returns>
    public bool RefreshFromSystem()
    {
        if (Mode != FollowSystem)
        {
            return false;
        }

        var previous = lastResolved;
        if (Resolve() == previous)
        {
            return false;
        }

        SystemChanged?.Invoke();
        return true;
    }

    /// <summary>Stops to watch the Windows app mode; the static system event would otherwise keep this instance alive.</summary>
    public void Dispose()
    {
        StopWatchingSystem();
    }

    /// <summary>Reacts to a change of the general Windows preferences, which include the app mode.</summary>
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs args)
    {
        if (args.Category == UserPreferenceCategory.General)
        {
            RefreshFromSystem();
        }
    }
}
