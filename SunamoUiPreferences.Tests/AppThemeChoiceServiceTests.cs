namespace SunamoUiPreferences.Tests;

/// <summary>Tests of <see cref="AppThemeChoiceService"/>.</summary>
public sealed class AppThemeChoiceServiceTests : IDisposable
{
    private readonly UiPreferencesTestFolder folder = new();
    private bool systemUsesDark;

    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose()
    {
        folder.Dispose();
    }

    /// <summary>Without a saved choice the theme follows the system mode.</summary>
    [Fact]
    public void ResolveFollowsSystemWithoutChoiceTest()
    {
        var service = CreateService();

        Assert.Equal(AppThemeChoiceService.FollowSystem, service.Mode);
        Assert.Equal(AppThemeChoiceService.Light, service.Resolve());

        systemUsesDark = true;

        Assert.Equal(AppThemeChoiceService.Dark, service.Resolve());
        Assert.True(service.IsDark);
    }

    /// <summary>An explicit choice wins over the system mode, is saved and raises Changed once.</summary>
    [Fact]
    public void SetSavesChoiceAndRaisesChangedTest()
    {
        var service = CreateService();
        var raisedCount = 0;
        service.Changed += () => raisedCount++;
        systemUsesDark = true;

        service.Set(AppThemeChoiceService.Light);

        Assert.Equal(1, raisedCount);
        Assert.Equal(AppThemeChoiceService.Light, service.Resolve());
        Assert.Equal(AppThemeChoiceService.Light, folder.CreateStore().Theme);
    }

    /// <summary>A value that is not one of the three choices is refused.</summary>
    [Fact]
    public void SetRefusesUnknownChoiceTest()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.Set("Blue"));
    }

    /// <summary>Cycling goes System, Light, Dark and back to System.</summary>
    [Fact]
    public void NextModeCyclesThroughAllModesTest()
    {
        var service = CreateService();
        var visited = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            service.Set(service.NextMode());
            visited.Add(service.Mode);
        }

        Assert.Equal(new[] { AppThemeChoiceService.Light, AppThemeChoiceService.Dark, AppThemeChoiceService.FollowSystem }, visited);
    }

    /// <summary>A change of the system mode raises SystemChanged only while the app follows the system and only when the theme differs.</summary>
    [Fact]
    public void RefreshFromSystemRaisesSystemChangedOnlyWhenFollowingTest()
    {
        var service = CreateService();
        var raisedCount = 0;
        service.SystemChanged += () => raisedCount++;
        service.Resolve();

        Assert.False(service.RefreshFromSystem());

        systemUsesDark = true;

        Assert.True(service.RefreshFromSystem());
        Assert.False(service.RefreshFromSystem());
        Assert.Equal(1, raisedCount);

        service.Set(AppThemeChoiceService.Light);
        systemUsesDark = false;

        Assert.False(service.RefreshFromSystem());
        Assert.Equal(1, raisedCount);
    }

    /// <summary>Watching the system can be started and stopped repeatedly without an error.</summary>
    [Fact]
    public void WatchingSystemStartsAndStopsTest()
    {
        var service = CreateService();

        service.StartWatchingSystem();
        service.StartWatchingSystem();
        service.StopWatchingSystem();
        service.Dispose();

        Assert.Empty(folder.Exceptions);
    }

    /// <summary>The Windows app mode is read from the registry without a logged exception.</summary>
    [Fact]
    public void WindowsAppModeIsReadTest()
    {
        WindowsAppMode.UsesDarkApps(folder.Log);

        Assert.Empty(folder.Exceptions);
    }

    /// <summary>Creates a service over the temporary store with a controllable system mode.</summary>
    private AppThemeChoiceService CreateService()
    {
        return new AppThemeChoiceService(folder.CreateStore(), folder.Log, () => systemUsesDark);
    }
}
