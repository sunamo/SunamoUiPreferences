namespace SunamoUiPreferences.Tests;

/// <summary>Tests of <see cref="UiPreferencesStore"/>.</summary>
public class UiPreferencesStoreTests
{
    /// <summary>A missing file gives empty choices and is reported to the log.</summary>
    [Fact]
    public void MissingFileGivesEmptyChoicesTest()
    {
        using var folder = new UiPreferencesTestFolder();
        var store = folder.CreateStore();

        Assert.Null(store.Theme);
        Assert.Null(store.Language);
        Assert.Single(folder.Messages);
        Assert.Empty(folder.Exceptions);
    }

    /// <summary>Saved choices are read back by a new store over the same folder.</summary>
    [Fact]
    public void SavedChoicesSurviveNewStoreTest()
    {
        using var folder = new UiPreferencesTestFolder();
        var store = folder.CreateStore();
        store.Theme = "Dark";
        store.Language = "cs";

        var reopened = folder.CreateStore();

        Assert.Equal("Dark", reopened.Theme);
        Assert.Equal("cs", reopened.Language);
        Assert.Equal(Path.Combine(folder.FolderPath, UiPreferencesStore.FileName), reopened.FilePath);
    }

    /// <summary>The file written by the apps before the package existed is read as it is.</summary>
    [Fact]
    public void ReadsFileOfFormerAppCopiesTest()
    {
        using var folder = new UiPreferencesTestFolder();
        File.WriteAllText(Path.Combine(folder.FolderPath, UiPreferencesStore.FileName), "{\"Theme\":\"Light\",\"Language\":\"en\"}");

        var store = folder.CreateStore();

        Assert.Equal("Light", store.Theme);
        Assert.Equal("en", store.Language);
        Assert.Empty(folder.Messages);
    }

    /// <summary>A damaged file gives empty choices and the exception goes to the log.</summary>
    [Fact]
    public void DamagedFileIsLoggedTest()
    {
        using var folder = new UiPreferencesTestFolder();
        File.WriteAllText(Path.Combine(folder.FolderPath, UiPreferencesStore.FileName), "{ not json");

        var store = folder.CreateStore();

        Assert.Null(store.Theme);
        Assert.Single(folder.Exceptions);
    }

    /// <summary>The settings folder is created when it does not exist yet.</summary>
    [Fact]
    public void SaveCreatesMissingFolderTest()
    {
        using var folder = new UiPreferencesTestFolder();
        var store = new UiPreferencesStore(Path.Combine(folder.FolderPath, "Settings"), folder.Log);

        store.Theme = "Dark";

        Assert.True(File.Exists(store.FilePath));
        Assert.Empty(folder.Exceptions);
    }
}
