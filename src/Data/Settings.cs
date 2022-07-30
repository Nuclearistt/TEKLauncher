namespace TEKLauncher.Data;

/// <summary>Manages launcher settings.</summary>
static class Settings
{
    /// <summary>Gets or sets a value that indicates whether the launcher should check for game and DLC updates when it starts.</summary>
    public static bool CheckForUpdates { get; set; } = true;
    /// <summary>Gets or sets a value that indicates whether the launcher should close itself after launching the game.</summary>
    public static bool CloseOnGameLaunch { get; set; }
    /// <summary>Gets or sets a value that indicates whether communism mode is enabled.</summary>
    public static bool CommunismMode { get; set; }
    /// <summary>Gets or sets a value that indicates whether settings file should be deleted rather than saved when shutting down the app.</summary>
    public static bool Delete { get; set; }
    /// <summary>Loads settings from the file and assigns their values to appropriate properties in static classes.</summary>
    public static void Load()
    {
        string settingsFile = $@"{App.AppDataFolder}\Settings.json";
        if (!File.Exists(settingsFile))
            return;
        using var stream = File.OpenRead(settingsFile);
        Json json;
        try { json = JsonSerializer.Deserialize<Json>(stream)!; }
        catch { return; }
        CheckForUpdates = json.CheckForUpdates;
        CloseOnGameLaunch = json.CloseOnGameLaunch;
        CommunismMode = json.CommunismMode;
        Game.HighProcessPriority = json.HighProcessPriority;
        Game.RunAsAdmin = json.RunAsAdmin;
        Game.UseBattlEye = json.UseBattlEye;
        Game.Language = json.GameLanguage;
        Steam.Client.NumberOfDownloadThreads = json.NumberOfDownloadThreads;
        Game.Path = json.ARKPath;
        if (json.LaunchParameters is not null)
            foreach (string parameter in json.LaunchParameters)
                if (!Game.LaunchParameters.Contains(parameter))
                    Game.LaunchParameters.Add(parameter);
        LocManager.CurrentLanguage = json.LauncherLanguage;
        if (json.CurrentManifestIds is not null)
            foreach (var item in json.CurrentManifestIds)
                Steam.Client.CurrentManifestIds.Add(new(item.Key), item.Value);
    }
    /// <summary>Saves settings into the JSON file.</summary>
    public static void Save()
    {
        string settingsFile = $@"{App.AppDataFolder}\Settings.json";
        if (Delete)
        {
            if (File.Exists(settingsFile))
                File.Delete(settingsFile);
            return;
        }
        Dictionary<string, ulong>? currentManifestIds = null;
        if (Steam.Client.CurrentManifestIds.Count > 0)
        {
            currentManifestIds = new(Steam.Client.CurrentManifestIds.Count);
            foreach (var item in Steam.Client.CurrentManifestIds)
                currentManifestIds.Add(item.Key.ToString(), item.Value);
        }
        var json = new Json(CheckForUpdates, CloseOnGameLaunch, CommunismMode, Game.HighProcessPriority, Game.RunAsAdmin, Game.UseBattlEye, Game.Language, Steam.Client.NumberOfDownloadThreads, Game.Path, LocManager.CurrentLanguage, Game.LaunchParameters, currentManifestIds);
        using var stream = File.Create(settingsFile);
        JsonSerializer.Serialize(stream, json, new JsonSerializerOptions() { WriteIndented = true });
    }
    /// <summary>Represents settings' JSON object.</summary>
    readonly record struct Json(bool CheckForUpdates, bool CloseOnGameLaunch, bool CommunismMode, bool HighProcessPriority, bool RunAsAdmin, bool UseBattlEye, int GameLanguage, int NumberOfDownloadThreads, string? ARKPath, string? LauncherLanguage, List<string>? LaunchParameters, Dictionary<string, ulong>? CurrentManifestIds);
}