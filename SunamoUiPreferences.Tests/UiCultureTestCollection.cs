namespace SunamoUiPreferences.Tests;

/// <summary>Collection of the tests that change the UI culture of the process, so they never run in parallel.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class UiCultureTestCollection
{
    /// <summary>Name of the collection.</summary>
    public const string Name = "UiCulture";
}
