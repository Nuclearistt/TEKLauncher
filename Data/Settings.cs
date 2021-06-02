using System.Collections.Generic;
using System.IO;
using static System.Array;
using static System.BitConverter;
using static System.IO.File;
using static System.Text.Encoding;
using static TEKLauncher.App;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Data
{
    internal static class Settings
    {
        internal static bool DeleteSettings, DowngradeMode;
        private static readonly string SettingsPath = $@"{AppDataFolder}\Settings.bin";
        private static readonly string[] Keys = new[] { "ARKPath", "CloseOnGameRun", "CommunismMode", "CustomLaunchParameters", "DwThreadsCount", "GameLang", "Lang", "LaunchParameters", "RunAsAdmin", "UseBattlEye", "UseGlobalFonts", "ValThreadsCount" };
        private static readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        internal static bool CloseOnGameRun { get => bool.Parse(Data["CloseOnGameRun"]); set => Data["CloseOnGameRun"] = value.ToString(); }
        internal static bool CommunismMode { get => bool.Parse(Data["CommunismMode"]); set => Data["CommunismMode"] = value.ToString(); }
        internal static bool RunAsAdmin { get => bool.Parse(Data["RunAsAdmin"]); set => Data["RunAsAdmin"] = value.ToString(); }
        internal static bool UseBattlEye { get => bool.Parse(Data["UseBattlEye"]); set => Data["UseBattlEye"] = value.ToString(); }
        internal static bool UseGlobalFonts { get => bool.Parse(Data["UseGlobalFonts"]); set => Data["UseGlobalFonts"] = value.ToString(); }
        internal static int DwThreadsCount { get => int.Parse(Data["DwThreadsCount"]);set => Data["DwThreadsCount"] = value.ToString(); }
        internal static int GameLang { get => int.Parse(Data["GameLang"]); set => Data["GameLang"] = value.ToString(); }
        internal static int ValThreadsCount { get => int.Parse(Data["ValThreadsCount"]); set => Data["ValThreadsCount"] = value.ToString(); }
        internal static string ARKPath { get => Data["ARKPath"]; set => Data["ARKPath"] = value; }
        internal static string CustomLaunchParameters { get => Data["CustomLaunchParameters"]; set => Data["CustomLaunchParameters"] = value; }
        internal static string Lang { get => Data["Lang"]; set => Data["Lang"] = value; }
        internal static string LaunchParameters { get => Data["LaunchParameters"]; set => Data["LaunchParameters"] = value; }
        private static void Encode(FileStream Stream, string StringToEncode)
        {
            byte[] EncodedString = UTF8.GetBytes(StringToEncode);
            Stream.Write(GetBytes((short)EncodedString.Length), 0, 2);
            Stream.Write(EncodedString, 0, EncodedString.Length);
        }
        private static void InitializeSetting(string Key, string DefaultValue)
        {
            if (!Data.ContainsKey(Key))
                Data.Add(Key, DefaultValue);
        }
        private static string Decode(FileStream Stream)
        {
            byte[] Buffer = new byte[2];
            Stream.Read(Buffer, 0, 2);
            Stream.Read(Buffer = new byte[ToInt16(Buffer, 0)], 0, Buffer.Length);
            return UTF8.GetString(Buffer);
        }
        internal static void Initialize()
        {
            if (FileExists(SettingsPath))
                using (FileStream Reader = OpenRead(SettingsPath))
                    while (Reader.Position != Reader.Length)
                    {
                        KeyValuePair<string, string> DecodedSetting = new KeyValuePair<string, string>(Decode(Reader), Decode(Reader));
                        if (IndexOf(Keys, DecodedSetting.Key) != -1 && !Data.ContainsKey(DecodedSetting.Key))
                            Data.Add(DecodedSetting.Key, DecodedSetting.Value);
                    }
            else
                Directory.CreateDirectory(AppDataFolder);
            InitializeSetting("CloseOnGameRun", bool.FalseString);
            InitializeSetting("CommunismMode", bool.FalseString);
            InitializeSetting("CustomLaunchParameters", string.Empty);
            InitializeSetting("DwThreadsCount", "6");
            InitializeSetting("GameLang", "4");
            InitializeSetting("Lang", string.Empty);
            InitializeSetting("LaunchParameters", string.Empty);
            InitializeSetting("RunAsAdmin", bool.TrueString);
            InitializeSetting("UseBattlEye", bool.TrueString);
            InitializeSetting("UseGlobalFonts", bool.FalseString);
            InitializeSetting("ValThreadsCount", "4");
        }
        internal static void RemoveKey(string Key) => Data.Remove(Key);
        internal static void Save()
        {
            if (!DeleteSettings)
            {
                using (FileStream Writer = Create(SettingsPath))
                    foreach (string Key in Keys)
                        if (Data.ContainsKey(Key))
                        {
                            Encode(Writer, Key);
                            Encode(Writer, Data[Key]);
                        }
            }
            else if (FileExists(SettingsPath))
                Delete(SettingsPath);
        }
        internal static bool KeyExists(string Key) => Data.ContainsKey(Key);
    }
}