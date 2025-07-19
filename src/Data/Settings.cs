namespace TEKLauncher.Data;

/// <summary>Manages launcher settings.</summary>
static class Settings
{
	/// <summary>Gets or sets a value that indicates whether the launcher should close itself after launching the game.</summary>
	public static bool CloseOnGameLaunch { get; set; }
    /// <summary>Gets or sets a value that indicates whether communism mode is enabled.</summary>
    public static bool CommunismMode { get; set; }
    /// <summary>Gets or sets a value that indicates whether settings file should be deleted rather than saved when shutting down the app.</summary>
    public static bool Delete { get; set; }
	public static bool PreAquatica { get; set; }
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
        CloseOnGameLaunch = json.CloseOnGameLaunch;
        CommunismMode = json.CommunismMode;
        Game.HighProcessPriority = json.HighProcessPriority;
        PreAquatica = json.PreAquatica;
        Game.RunAsAdmin = json.RunAsAdmin;
        Game.UseSpacewar = json.UseSpacewar;
        Game.Language = json.GameLanguage;
        Game.Path = json.ARKPath;
        if (json.LaunchParameters is not null)
            foreach (string parameter in json.LaunchParameters)
                if (!Game.LaunchParameters.Contains(parameter))
                    Game.LaunchParameters.Add(parameter);
        LocManager.CurrentLanguage = json.LauncherLanguage;
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
        var json = new Json(CloseOnGameLaunch, CommunismMode, Game.HighProcessPriority, PreAquatica, Game.RunAsAdmin, Game.UseSpacewar, Game.Language, Game.Path, LocManager.CurrentLanguage, Game.LaunchParameters);
        using var stream = File.Create(settingsFile);
        JsonSerializer.Serialize(stream, json, new JsonSerializerOptions() { WriteIndented = true });
    }
    /// <summary>Represents settings' JSON object.</summary>
    readonly record struct Json(bool CloseOnGameLaunch, bool CommunismMode, bool HighProcessPriority, bool PreAquatica, bool RunAsAdmin, bool UseSpacewar, int GameLanguage, string? ARKPath, string? LauncherLanguage, List<string>? LaunchParameters);
}