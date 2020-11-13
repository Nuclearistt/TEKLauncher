using System.Collections.Generic;
using System.IO;
using static System.IO.File;
using static System.Windows.Application;
using static Microsoft.Win32.Registry;
using static TEKLauncher.UI.Message;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.SteamInterop
{
    internal static class Steam
    {
        private static int ARKEntryEndLine = 0, ParametersStringLine = 0;
        private static string LocalConfigFile;
        internal static bool IsARKPurchased = false;
        internal static string Path;
        internal static bool IsRunning => (((int?)LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")?.GetValue("SteamPID")) ?? 0) != 0;
        internal static bool IsSpacewarInstalled => !(WorkshopPath is null);
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
        internal static string WorkshopPath
        {
            get
            {
                string SpacewarPath = (string)LocalMachine?.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 480")?.GetValue("InstallLocation");
                return SpacewarPath is null ? null : $@"{SpacewarPath.Substring(0, SpacewarPath.Length - 15)}workshop\content\480";

            }
        }
        internal static void Initialize()
        {
            Path = (string)LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")?.GetValue("InstallPath");
            if (Path is null)
            {
                Show("Error", "Launcher cannot be started because you don't have Steam installed!");
                Current.Shutdown();
            }
            object ActiveUser = CurrentUser.OpenSubKey(@"Software\Valve\Steam\ActiveProcess")?.GetValue("ActiveUser");
            if (!(ActiveUser is null))
            {
                LocalConfigFile = $@"{Path}\userdata\{(int)ActiveUser}\config\localconfig.vdf";
                if (FileExists(LocalConfigFile))
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
            }
        }
    }
}