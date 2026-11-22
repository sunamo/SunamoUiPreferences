# SunamoUiPreferences

Language and theme choice for Windows desktop apps (WPF, WinForms), independent of any UI library.

- `UiPreferencesStore` keeps the two choices in `ui-settings.json` in a folder given by the app.
- `AppLanguageService` follows the Windows display languages or an explicit language, sets the UI culture and raises `Changed`.
- `AppThemeChoiceService` follows the Windows app mode or is light or dark, resolves the choice to `Light`/`Dark` and raises `Changed` and `SystemChanged`.

The app applies the resolved theme to its own UI library in the event handlers.

## Usage

```csharp
var store = new UiPreferencesStore(settingsFolder, (message, exception) => AppLog.Write(message, exception));

var language = new AppLanguageService(store, new[] { "cs", "en" }, AppLog.Write);
language.Changed += RebuildViews;
language.Initialize();

var theme = new AppThemeChoiceService(store, AppLog.Write);
theme.Changed += () => ApplyThemeToUiLibrary(theme.IsDark);
theme.SystemChanged += () => dispatcher.BeginInvoke(() => ApplyThemeToUiLibrary(theme.IsDark));
theme.StartWatchingSystem();

// Cycle the choices from a button.
language.Set(language.NextMode());
theme.Set(theme.NextMode());
```

## Notes

- Windows only (registry, `SystemEvents`, `GetUserPreferredUILanguages`).
- When Windows asks for none of the supported languages, English is used when supported, otherwise the first supported language.
- `SystemChanged` comes from a system thread; switch to the UI thread in the handler.
