namespace SunamoUiPreferences.Tests;

/// <summary>Tests of <see cref="AppLanguageService"/>; the UI culture of the process is restored after each test.</summary>
[Collection(UiCultureTestCollection.Name)]
public sealed class AppLanguageServiceTests : IDisposable
{
    private static readonly string[] CzechAndEnglish = { "cs", "en" };

    private readonly UiPreferencesTestFolder folder = new();
    private readonly CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
    private readonly CultureInfo? originalDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

    /// <summary>Restores the UI culture and deletes the temporary folder.</summary>
    public void Dispose()
    {
        CultureInfo.CurrentUICulture = originalUiCulture;
        CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUiCulture;
        folder.Dispose();
    }

    /// <summary>Without a saved choice the first supported language of the preferred list wins.</summary>
    [Fact]
    public void InitializeFollowsFirstSupportedPreferredLanguageTest()
    {
        var service = CreateService(CzechAndEnglish, "de-DE", "cs-CZ", "en-US");

        service.Initialize();

        Assert.Equal(AppLanguageService.FollowSystem, service.Mode);
        Assert.Equal("cs", service.Current);
        Assert.Equal("cs-CZ", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("cs-CZ", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    /// <summary>English is used when the preferred list has no supported language.</summary>
    [Fact]
    public void InitializeFallsBackToEnglishTest()
    {
        var service = CreateService(CzechAndEnglish, "de-DE", "fr-FR");

        service.Initialize();

        Assert.Equal("en", service.Current);
        Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
    }

    /// <summary>Without English among the supported languages the first supported one is the fallback.</summary>
    [Fact]
    public void FallbackIsFirstSupportedLanguageWithoutEnglishTest()
    {
        var service = CreateService(new[] { "cs", "de" }, "fr-FR");

        Assert.Equal("cs", service.FallbackLanguage);
        Assert.Equal("cs", service.SystemLanguage);
    }

    /// <summary>The saved choice wins over the language of Windows.</summary>
    [Fact]
    public void InitializeUsesSavedChoiceTest()
    {
        folder.CreateStore().Language = "en";
        var service = CreateService(CzechAndEnglish, "cs-CZ");

        service.Initialize();

        Assert.Equal("en", service.Mode);
        Assert.Equal("en", service.Current);
    }

    /// <summary>A saved language the app does not support is treated as following Windows.</summary>
    [Fact]
    public void UnsupportedSavedChoiceFollowsSystemTest()
    {
        folder.CreateStore().Language = "de";
        var service = CreateService(CzechAndEnglish, "cs-CZ");

        Assert.Equal(AppLanguageService.FollowSystem, service.Mode);
    }

    /// <summary>Set saves the choice, switches the culture and raises Changed once.</summary>
    [Fact]
    public void SetSavesChoiceAndRaisesChangedTest()
    {
        var service = CreateService(CzechAndEnglish, "en-US");
        service.Initialize();
        var raisedCount = 0;
        service.Changed += () => raisedCount++;

        service.Set("cs");

        Assert.Equal(1, raisedCount);
        Assert.Equal("cs", service.Current);
        Assert.Equal("cs-CZ", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("cs", folder.CreateStore().Language);
    }

    /// <summary>Going back to following Windows restores the language detected at startup.</summary>
    [Fact]
    public void SetFollowSystemRestoresSystemLanguageTest()
    {
        var service = CreateService(CzechAndEnglish, "cs-CZ");
        service.Initialize();
        service.Set("en");

        service.Set(AppLanguageService.FollowSystem);

        Assert.Equal(AppLanguageService.FollowSystem, service.Mode);
        Assert.Equal("cs", service.Current);
    }

    /// <summary>A language the app does not support is refused.</summary>
    [Fact]
    public void SetRefusesUnsupportedLanguageTest()
    {
        var service = CreateService(CzechAndEnglish, "cs-CZ");

        Assert.Throws<ArgumentException>(() => service.Set("de"));
    }

    /// <summary>Cycling goes through System and all supported languages and back.</summary>
    [Fact]
    public void NextModeCyclesThroughAllModesTest()
    {
        var service = CreateService(CzechAndEnglish, "en-US");
        service.Initialize();
        var visited = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            service.Set(service.NextMode());
            visited.Add(service.Mode);
        }

        Assert.Equal(new[] { "cs", "en", AppLanguageService.FollowSystem }, visited);
        Assert.Equal(new[] { AppLanguageService.FollowSystem, "cs", "en" }, service.Modes);
    }

    /// <summary>An empty list of supported languages is refused.</summary>
    [Fact]
    public void EmptySupportedLanguagesAreRefusedTest()
    {
        Assert.Throws<ArgumentException>(() => CreateService(Array.Empty<string>(), "en-US"));
    }

    /// <summary>The display languages of Windows give at least one culture name without a logged failure.</summary>
    [Fact]
    public void WindowsPreferredUiLanguagesAreReadTest()
    {
        var names = WindowsPreferredUiLanguages.Get(folder.Log);

        Assert.NotEmpty(names);
        Assert.Empty(folder.Messages);
    }

    /// <summary>Creates a service over the temporary store with a fixed preferred list.</summary>
    private AppLanguageService CreateService(IEnumerable<string> supportedLanguages, params string[] preferredLanguages)
    {
        return new AppLanguageService(folder.CreateStore(), supportedLanguages, folder.Log, () => preferredLanguages);
    }
}
