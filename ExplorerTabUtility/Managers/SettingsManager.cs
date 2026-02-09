using System;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ExplorerTabUtility.Models;
using ExplorerTabUtility.Helpers;

namespace ExplorerTabUtility.Managers;

public static class SettingsManager
{
    private static readonly AppSettings Settings;
    private static WindowRecord[]? _closedWindows;
    public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;

    private static readonly object _settingsLock = new();
    private static readonly object _windowsLock = new();

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Constants.AppName);

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, Constants.SettingsFileName);
    private static readonly string WindowsFilePath = Path.Combine(SettingsDirectory, Constants.WindowsFileName);

    static SettingsManager()
    {
        Directory.CreateDirectory(SettingsDirectory);

        Settings = ReadJsonFile<AppSettings>(SettingsFilePath) ?? new AppSettings();
        _closedWindows = ReadJsonFile<WindowRecord[]>(WindowsFilePath);

        // One-time migration: old settings.json may contain ClosedWindows
        if (_closedWindows == null)
        {
            var legacy = TryDeserializeFile<LegacyClosedWindows>(SettingsFilePath);
            if (legacy?.ClosedWindows is { Length: > 0 })
            {
                _closedWindows = legacy.ClosedWindows;
                SaveWindowRecords();
            }
        }
    }

    private static void NotifyStaticPropertyChanged([CallerMemberName] string propertyName = "")
    {
        StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
    }

    public static bool IsMouseHookActive
    {
        get => Settings.MouseHook;
        set
        {
            Settings.MouseHook = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static bool IsKeyboardHookActive
    {
        get => Settings.KeyboardHook;
        set
        {
            Settings.KeyboardHook = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static bool IsWindowHookActive
    {
        get => Settings.WindowHook;
        set
        {
            Settings.WindowHook = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static bool ReuseTabs
    {
        get => Settings.ReuseTabs;
        set
        {
            Settings.ReuseTabs = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static string HotKeyProfiles
    {
        get => Settings.HotKeyProfiles;
        set
        {
            Settings.HotKeyProfiles = value;
            SaveSettings();
        }
    }

    public static Size FormSize
    {
        get => Settings.FormSize;
        set
        {
            Settings.FormSize = value;
            SaveSettings();
        }
    }

    public static bool SaveProfilesOnExit
    {
        get => Settings.SaveProfilesOnExit;
        set
        {
            Settings.SaveProfilesOnExit = value;
            SaveSettings();
        }
    }

    public static bool IsFirstRun
    {
        get => Settings.IsFirstRun;
        set
        {
            Settings.IsFirstRun = value;
            SaveSettings();
        }
    }

    public static bool IsTrayIconHidden
    {
        get => Settings.IsTrayIconHidden;
        set
        {
            Settings.IsTrayIconHidden = value;
            SaveSettings();
        }
    }

    public static bool HaveThemeIssue
    {
        get => Settings.HaveThemeIssue;
        set
        {
            Settings.HaveThemeIssue = value;
            SaveSettings();
        }
    }

    public static bool AutoUpdate
    {
        get => Settings.AutoUpdate;
        set
        {
            Settings.AutoUpdate = value;
            SaveSettings();
        }
    }

    public static bool SaveClosedHistory
    {
        get => Settings.SaveClosedWindows;
        set
        {
            Settings.SaveClosedWindows = value;
            SaveSettings();
        }
    }

    public static bool RestorePreviousWindows
    {
        get => Settings.RestorePreviousWindows;
        set
        {
            Settings.RestorePreviousWindows = value;
            SaveSettings();
        }
    }

    public static WindowRecord[]? ClosedWindows
    {
        get => _closedWindows;
        set
        {
            _closedWindows = value;
            SaveWindowRecords();
        }
    }

    public static void SaveSettings()
    {
        AtomicWriteJson(SettingsFilePath, Settings, _settingsLock);
    }

    private static void SaveWindowRecords()
    {
        AtomicWriteJson(WindowsFilePath, _closedWindows, _windowsLock);
    }

    private static T? ReadJsonFile<T>(string filePath) where T : class
    {
        // Try primary file first, then backup
        var backupPath = Path.ChangeExtension(filePath, ".bak");

        var result = TryDeserializeFile<T>(filePath);
        if (result != null) return result;

        result = TryDeserializeFile<T>(backupPath);
        return result;
    }

    private static T? TryDeserializeFile<T>(string filePath) where T : class
    {
        try
        {
            if (!File.Exists(filePath)) return null;

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json)) return null;

            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void AtomicWriteJson<T>(string filePath, T data, object lockObj)
    {
        lock (lockObj)
        {
            var tmpPath = filePath + ".tmp";
            try
            {
                var json = JsonSerializer.Serialize(data);
                var backupPath = Path.ChangeExtension(filePath, ".bak");

                File.WriteAllText(tmpPath, json);

                if (File.Exists(filePath))
                    File.Replace(tmpPath, filePath, backupPath);
                else
                    File.Move(tmpPath, filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save {filePath}: {ex.Message}");
                try { File.Delete(tmpPath); } catch { /* best-effort cleanup */ }
            }
        }
    }
}

internal class AppSettings
{
    public bool MouseHook { get; set; }
    public bool KeyboardHook { get; set; } = true;
    public bool WindowHook { get; set; } = true;
    public bool ReuseTabs { get; set; } = true;
    public Size FormSize { get; set; } = new(852, 402);
    public bool SaveProfilesOnExit { get; set; } = true;
    public bool IsFirstRun { get; set; } = true;
    public bool IsTrayIconHidden { get; set; }
    public bool HaveThemeIssue { get; set; }
    public bool AutoUpdate { get; set; }
    public string HotKeyProfiles { get; set; } = Constants.DefaultHotKeyProfiles;
    public bool SaveClosedWindows { get; set; }
    public bool RestorePreviousWindows { get; set; }
}

// Used only for one-time migration from old settings.json format
internal class LegacyClosedWindows
{
    public WindowRecord[]? ClosedWindows { get; set; }
}
