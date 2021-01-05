using System.Collections.Generic;
using TEKLauncher.Data;
using TEKLauncher.SteamInterop;
using static TEKLauncher.ARK.Game;
using static TEKLauncher.Data.Settings;
using static TEKLauncher.SteamInterop.Steam;

namespace TEKLauncher.ARK
{
    internal static class LaunchParameters
    {
        internal static void Initialize()
        {
            if (string.IsNullOrEmpty(Settings.LaunchParameters))
            {
                if (IsARKPurchased)
                    foreach (string Parameter in Steam.LaunchParameters.Split())
                        Add(Parameter);
            }
            else
                foreach (string Parameter in Settings.LaunchParameters.Split())
                    Add(Parameter);
            IsInitialized = true;
            Settings.LaunchParameters = Parameters.Count == 0 ? string.Empty : string.Join(" ", Parameters);
        }
        private static bool IsInitialized = false;
        private static readonly List<string> Parameters = new List<string>();
        internal static readonly string[] GameCultureCodes = new[] { "ca", "cs", "da", "de", "en", "es", "eu", "fi", "fr", "hu", "it", "ja", "ka", "ko", "nl", "pl", "pt_BR", "ru", "sv", "th", "tr", "uk", "zh", "zh-Hans-CN", "zh-TW" };
        internal static string CultureParameter => UseGlobalFonts && GlobalFontsInstalled ? "-culture=global" : $"-culture={GameCultureCodes[GameLang]}";
        internal static void Add(string Parameter)
        {
            if (!Parameters.Contains(Parameter))
            {
                Parameters.Add(Parameter);
                if (IsInitialized)
                    Settings.LaunchParameters = string.Join(" ", Parameters);
            }
        }
        internal static void Remove(string Parameter)
        {
            if (Parameters.Contains(Parameter))
            {
                Parameters.Remove(Parameter);
                Settings.LaunchParameters = Parameters.Count == 0 ? string.Empty : string.Join(" ", Parameters);
            }
        }
        internal static bool ParameterExists(string Parameter) => Parameters.Contains(Parameter);
    }
}