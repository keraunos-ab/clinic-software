using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace clinicApp.Services
{
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        private const string SettingsFileName = "appsettings.json";
        private string SettingsFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);

        public bool IsDarkTheme { get; private set; } = false;

        public event EventHandler<bool>? ThemeChanged;

        private ThemeManager()
        {
            LoadSettings();
        }

        public void SetTheme(bool isDark)
        {
            if (IsDarkTheme == isDark) return;

            IsDarkTheme = isDark;
            ApplyTheme();
            SaveSettings();
        }

        public void ToggleTheme()
        {
            SetTheme(!IsDarkTheme);
        }

        public void ApplyTheme()
        {
            var app = Application.Current;
            if (app == null) return;

            // Remove only theme dictionaries, keep language ones
            var toRemove = new System.Collections.Generic.List<ResourceDictionary>();
            foreach (var dict in app.Resources.MergedDictionaries)
            {
                if (dict.Source?.OriginalString.Contains("Themes/") == true)
                {
                    toRemove.Add(dict);
                }
            }
            foreach (var dict in toRemove)
            {
                app.Resources.MergedDictionaries.Remove(dict);
            }

            var themeUri = new Uri(IsDarkTheme ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
            app.Resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = themeUri });

            ThemeChanged?.Invoke(this, IsDarkTheme);
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null && !string.IsNullOrEmpty(settings.Theme))
                    {
                        IsDarkTheme = settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load theme settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings { Theme = IsDarkTheme ? "Dark" : "Light" };

                // Try to preserve existing settings (like Language)
                if (File.Exists(SettingsFilePath))
                {
                    try
                    {
                        var existingJson = File.ReadAllText(SettingsFilePath);
                        var existing = JsonSerializer.Deserialize<AppSettings>(existingJson);
                        if (existing != null)
                        {
                            existing.Theme = IsDarkTheme ? "Dark" : "Light";
                            settings = existing;
                        }
                    }
                    catch { /* Use new settings if parsing fails */ }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save theme settings: {ex.Message}");
            }
        }

        private class AppSettings
        {
            public string Language { get; set; } = "English";
            public string? Theme { get; set; }
        }
    }
}
