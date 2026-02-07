using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace clinicApp.Services
{
    public class LanguageManager
    {
        private static LanguageManager? _instance;
        public static LanguageManager Instance => _instance ??= new LanguageManager();

        private const string SettingsFileName = "appsettings.json";
        private string SettingsFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);

        public string CurrentLanguage { get; private set; } = "English";

        private readonly string[] _resourceFiles =
        {
            "MainWindow.xaml",
            "HomePage.xaml",
            "PatientsPage.xaml",
            "AppointmentsPage.xaml",
            "Settings.xaml",
            "Introduction.xaml",
            "PrescriptionPage.xaml",
            "IntroductionWindow.xaml",
            "PatientHistoryWindow.xaml",
            "QuickActionWindow.xaml",
            "PatientCard.xaml",
            "AddPatientPage.xaml",
            "AddApointmentPage.xaml",
            "AddSessionPage.xaml",
            "PasswordEntry.xaml"
        };

        public static readonly Dictionary<string, int> LanguageIndexMap = new()
        {
            { "English", 0 },
            { "French", 1 },
            { "Arabic", 2 }
        };

        public static readonly Dictionary<int, string> IndexLanguageMap = new()
        {
            { 0, "English" },
            { 1, "French" },
            { 2, "Arabic" }
        };

        private LanguageManager()
        {
            LoadSettings();
        }

        public void SetLanguage(string language)
        {
            if (CurrentLanguage == language) return;

            CurrentLanguage = language;
            ApplyLanguage();
            SaveSettings();
        }

        public void SetLanguageByIndex(int index)
        {
            if (IndexLanguageMap.TryGetValue(index, out var language))
            {
                SetLanguage(language);
            }
        }

        public int GetCurrentLanguageIndex()
        {
            return LanguageIndexMap.TryGetValue(CurrentLanguage, out var index) ? index : 0;
        }

        public void ApplyLanguage()
        {
            var app = Application.Current;
            if (app == null) return;

            // Remove existing language dictionaries
            var toRemove = new List<ResourceDictionary>();
            foreach (var dict in app.Resources.MergedDictionaries)
            {
                if (dict.Source?.OriginalString.Contains("/Languages/") == true)
                {
                    toRemove.Add(dict);
                }
            }
            foreach (var dict in toRemove)
            {
                app.Resources.MergedDictionaries.Remove(dict);
            }

            // Add new language dictionaries
            string basePath = $"Resources/Languages/{CurrentLanguage}/";
            foreach (var file in _resourceFiles)
            {
                try
                {
                    var uri = new Uri($"pack://application:,,,/{basePath}{file}", UriKind.Absolute);
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load {file}: {ex.Message}");
                }
            }

            // Notify about FlowDirection change for RTL languages
            LanguageChanged?.Invoke(this, CurrentLanguage);
        }

        public FlowDirection GetFlowDirection()
        {
            return CurrentLanguage == "Arabic" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        public event EventHandler<string>? LanguageChanged;

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null && !string.IsNullOrEmpty(settings.Language))
                    {
                        CurrentLanguage = settings.Language;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings { Language = CurrentLanguage };

                // Try to preserve existing settings
                if (File.Exists(SettingsFilePath))
                {
                    try
                    {
                        var existingJson = File.ReadAllText(SettingsFilePath);
                        var existing = JsonSerializer.Deserialize<AppSettings>(existingJson);
                        if (existing != null)
                        {
                            existing.Language = CurrentLanguage;
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
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private class AppSettings
        {
            public string Language { get; set; } = "English";
            public string? Theme { get; set; }
        }
    }
}