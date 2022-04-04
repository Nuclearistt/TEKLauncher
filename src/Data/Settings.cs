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
    /// <summary>Loads settings from legacy format settings file.</summary>
    /// <param name="filePath">Path to the Settings.bin file.</param>
    static void LoadLegacy(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        Span<byte> buffer = stackalloc byte[2]; //Buffer for string lengths (ushorts)
        while (stream.Read(buffer) == 2)
        {
            //Read setting name
            byte[] stringBuffer = new byte[BitConverter.ToUInt16(buffer)];
            stream.Read(stringBuffer);
            string name = Encoding.UTF8.GetString(stringBuffer);
            //Read setting value
            stream.Read(buffer);
            stringBuffer = new byte[BitConverter.ToUInt16(buffer)];
            stream.Read(stringBuffer);
            string value = Encoding.UTF8.GetString(stringBuffer);
            switch (name)
            {
                case "ARKPath": Game.Path = value; break;
                case "CommunismMode": CommunismMode = bool.Parse(value); break;
                case "CustomLaunchParameters":
                case "LaunchParameters":
                    if (value.Length > 0)
                        foreach (var parameter in value.Split())
                            if (parameter.Length > 0)
                                Game.LaunchParameters.Add(parameter);
                    break;
                case "DisableUpdChecks": CheckForUpdates = !bool.Parse(value); break;
                case "DwThreadsCount": Steam.Client.NumberOfDownloadThreads = int.Parse(value); break;
                case "GameLang": Game.Language = int.Parse(value); break;
                case "Lang": LocManager.CurrentLanguage = value; break;
                case "RunAsAdmin": Game.RunAsAdmin = bool.Parse(value); break;
            }
        }
    }
    /// <summary>Loads settings from the file and assigns their values to appropriate properties in static classes.</summary>
    public static void Load()
    {
        string settingsFile = $@"{App.AppDataFolder}\Settings.json";
        if (File.Exists(settingsFile))
        {
            using var stream = File.OpenRead(settingsFile);
            var json = JsonSerializer.Deserialize<Json>(stream)!;
            CheckForUpdates = json.CheckForUpdates;
            CloseOnGameLaunch = json.CloseOnGameLaunch;
            CommunismMode = json.CommunismMode;
            Game.RunAsAdmin = json.RunAsAdmin;
            Game.UseGlobalFonts = json.UseGlobalFonts;
            Game.UseTEKInjector = json.UseTEKInjector;
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
        else
        {
            string legacySettingsFile = $@"{App.AppDataFolder}\Settings.bin"; //Legacy format settings file used prior to 9.0.72
            if (File.Exists(legacySettingsFile))
            {
                LoadLegacy(legacySettingsFile);
                File.Delete(legacySettingsFile);
            }
        }
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
        var json = new Json(CheckForUpdates, CloseOnGameLaunch, CommunismMode, Game.RunAsAdmin, Game.UseGlobalFonts, Game.UseTEKInjector, Game.Language, Steam.Client.NumberOfDownloadThreads, Game.Path, LocManager.CurrentLanguage, Game.LaunchParameters, currentManifestIds);
        using var stream = File.Create(settingsFile);
        JsonSerializer.Serialize(stream, json, new JsonSerializerOptions() { WriteIndented = true });
    }
    /// <summary>Represents settings' JSON object.</summary>
    readonly record struct Json(bool CheckForUpdates, bool CloseOnGameLaunch, bool CommunismMode, bool RunAsAdmin, bool UseGlobalFonts, bool UseTEKInjector, int GameLanguage, int NumberOfDownloadThreads, string? ARKPath, string? LauncherLanguage, List<string>? LaunchParameters, Dictionary<string, ulong>? CurrentManifestIds);
}