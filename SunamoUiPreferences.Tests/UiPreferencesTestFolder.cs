namespace SunamoUiPreferences.Tests;

/// <summary>Temporary settings folder of one test with the log messages the tested code produced; the folder is deleted on dispose.</summary>
public sealed class UiPreferencesTestFolder : IDisposable
{
    /// <summary>Creates an empty temporary folder.</summary>
    public UiPreferencesTestFolder()
    {
        FolderPath = Path.Combine(Path.GetTempPath(), "SunamoUiPreferences.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(FolderPath);
    }

    /// <summary>Path of the temporary folder.</summary>
    public string FolderPath { get; }

    /// <summary>Messages passed to <see cref="Log"/>.</summary>
    public List<string> Messages { get; } = new();

    /// <summary>Exceptions passed to <see cref="Log"/>.</summary>
    public List<Exception> Exceptions { get; } = new();

    /// <summary>Log delegate for the tested code.</summary>
    /// <param name="message">Logged message.</param>
    /// <param name="exception">Logged exception, if any.</param>
    public void Log(string message, Exception? exception)
    {
        Messages.Add(message);
        if (exception is not null)
        {
            Exceptions.Add(exception);
        }
    }

    /// <summary>Creates a store over the temporary folder.</summary>
    /// <returns>The store.</returns>
    public UiPreferencesStore CreateStore()
    {
        return new UiPreferencesStore(FolderPath, Log);
    }

    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose()
    {
        if (Directory.Exists(FolderPath))
        {
            Directory.Delete(FolderPath, true);
        }
    }
}
