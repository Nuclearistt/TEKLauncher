using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TEKLauncher.Utils;
using static System.Diagnostics.Process;
using static System.IO.File;
using static System.Windows.Application;
using static Microsoft.Win32.Registry;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.UI.Message;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.SteamInterop
{
    internal static class Steam
    {
        private static int ARKEntryEndLine = 0, ParametersStringLine = 0;
        private static string LocalConfigFile, SpacewarPath;
        internal static bool IsARKPurchased = false;
        internal static int ActiveUserID;
        internal static string Path;
        internal static bool IsRunning
        {
            get
            {
                int PID = ((int?)LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")?.GetValue("SteamPID")) ?? 0;
                if (PID == 0)
                    return false;
                Process SteamProcess;
                try { SteamProcess = GetProcessById(PID); }
                catch (ArgumentException) { SteamProcess = null; }
                return !(SteamProcess is null);
            }
        }
        internal static bool IsSpacewarInstalled => !(SpacewarPath is null);
        internal static string LaunchParameters
        {
            get
            {
                if (ParametersStringLine == 0)
                    return string.Empty;
                else
                    using (StreamReader Reader = new StreamReader(LocalConfigFile))
                    {
                        string Line = null;
                        for (int Iterator = 0; Iterator <= ParametersStringLine; Iterator++)
                            Line = Reader.ReadLine();
                        return Line.Substring(24, Line.Length - 25);
                    }
            }
            set
            {
                List<string> Lines = new List<string>(ReadAllLines(LocalConfigFile));
                if (ParametersStringLine == 0)
                    Lines.Insert(ARKEntryEndLine, $"\t\t\t\t\t\t\"LaunchOptions\"\t\t\"{value}\"");
                else
                    Lines[ParametersStringLine] = $"\t\t\t\t\t\t\"LaunchOptions\"\t\t\"{value}\"";
                WriteAllLines(LocalConfigFile, Lines.ToArray());
            }
        }
        internal static string WorkshopPath => SpacewarPath is null ? null : $@"{SpacewarPath.Substring(0, SpacewarPath.Length - 15)}workshop\content\480";
        internal static void Initialize()
        {
            Path = (string)LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")?.GetValue("InstallPath");
            if (Path is null)
            {
                Show("Error", LocString(LocCode.CantStartLauncher));
                Current.Shutdown();
            }
            object ActiveUser = CurrentUser.OpenSubKey(@"Software\Valve\Steam\ActiveProcess")?.GetValue("ActiveUser");
            if (!(ActiveUser is null) && FileExists(LocalConfigFile = $@"{Path}\userdata\{ActiveUserID = (int)ActiveUser}\config\localconfig.vdf"))
                using (StreamReader Reader = new StreamReader(LocalConfigFile))
                {
                    bool ARKEntryFound = false;
                    for (int LineIndex = 0; !Reader.EndOfStream; LineIndex++)
                    {
                        string Line = Reader.ReadLine();
                        if (Line == "\t\t\t\t\t\"346110\"")
                            ARKEntryFound = true;
                        else if (ARKEntryFound && Line.StartsWith("\t\t\t\t\t\t\"LaunchOptions\""))
                            ParametersStringLine = LineIndex;
                        else if (ARKEntryFound && Line.Contains("}"))
                        {
                            ARKEntryEndLine = --LineIndex;
                            IsARKPurchased = true;
                            break;
                        }
                    }
                }
            string DefaultSpacewarPath = $@"{Path}\steamapps\common\Spacewar", LibrariesFile = $@"{Path}\steamapps\libraryfolders.vdf", RegistrySpacewarPath = (string)LocalMachine?.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 480")?.GetValue("InstallLocation");
            List<string> SpacewarPaths = new List<string>();
            if (Directory.Exists(DefaultSpacewarPath))
                SpacewarPaths.Add(DefaultSpacewarPath);
            if (FileExists(LibrariesFile))
                try
                {
                    VDFStruct Struct;
                    using (StreamReader Reader = new StreamReader(LibrariesFile))
                        Struct = new VDFStruct(Reader);
                    foreach (VDFStruct Child in Struct.Children)
                        if (int.TryParse(Child.Key, out _) && !(Child.Value is null))
                        {
                            string Path = $@"{Child.Value.Replace(@"\\", @"\")}\steamapps\common\Spacewar";
                            if (Directory.Exists(Path))
                                SpacewarPaths.Add(Path);
                        }
                }
                catch { }
            SpacewarPath = SpacewarPaths.Count != 0 && SpacewarPaths.Find(Path => Path == RegistrySpacewarPath) is null ? SpacewarPaths[0] : RegistrySpacewarPath;
        }
    }
}