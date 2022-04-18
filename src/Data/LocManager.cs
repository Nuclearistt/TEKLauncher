namespace TEKLauncher.Data;

/// <summary>Manages launcher localizations.</summary>
static class LocManager
{
    /// <summary>List of all localized strings.</summary>
    static readonly string[] s_list = new string[226];
    /// <summary>List of ISO 639-1 codes of all currently supported languages.</summary>
    static readonly string[] s_supportedLangs = { "en", "es", "fr", "pt", "el", "ru" };
    /// <summary>Gets the index of current launcher language in the list of all supported languages.</summary>
    public static int CurrentLanguageIndex => Array.IndexOf(s_supportedLangs, CurrentLanguage);
    /// <summary>Gets or sets the ISO 639-1 code of current display language of the launcher.</summary>
    public static string? CurrentLanguage { get; set; }
    /// <summary>Selects launcher display language based on setting value and OS language, and loads localized strings.</summary>
    /// <param name="cultureCode">Culture code of Windows' UI.</param>
    public static void Initialize(string cultureCode)
    {
        if (string.IsNullOrEmpty(CurrentLanguage) || Array.IndexOf(s_supportedLangs, CurrentLanguage) == -1)
        {
            if (cultureCode.Length < 2)
                CurrentLanguage = "en"; //Fallback to English if cultureCode is invalid
            else
            {
                cultureCode = cultureCode[..2]; //Leave only the language code
                if (cultureCode == "be" || cultureCode == "uk")
                    CurrentLanguage = "ru"; //Belarusian and Ukrainian are very similar to Russian so Russian loc can be used
                else if (Array.IndexOf(s_supportedLangs, cultureCode) == -1)
                    CurrentLanguage = "en"; //Fallback to English if OS language is not in the list of supported ones
                else
                    CurrentLanguage = cultureCode; //OS language is supported, use its loc
            }
        }
        //Load localized strings
        using var resourceStream = Application.GetResourceStream(new($"pack://application:,,,/res/loc/{CurrentLanguage}.txt")).Stream;
        using var reader = new StreamReader(resourceStream);
        for (int i = 0; i < s_list.Length; i++)
            s_list[i] = reader.ReadLine()!.Replace(@"\n", "\n");
    }
    /// <summary>Sets new current language by its index.</summary>
    /// <param name="index">Index of the new language in supported languages list.</param>
    public static void SetCurrentLanguage(int index) => CurrentLanguage = s_supportedLangs[index];
    /// <summary>Converts an amount of bytes into its string representation.</summary>
    /// <param name="bytes">Amount of bytes.</param>
    /// <returns>The string representation of size.</returns>
    public static string BytesToString(long bytes) => bytes switch
    {
        >= 1073741824 => $"{bytes / 1073741824.0:0.##} {s_list[(int)LocCode.GB]}",
        >= 1048576 => $"{bytes / 1048576.0:0.#} {s_list[(int)LocCode.MB]}",
        _ => $"{bytes / 1024.0:0} {s_list[(int)LocCode.KB]}"
    };
    /// <summary>Converts an amount of bytes into its string representation.</summary>
    /// <param name="bytes">Amount of bytes.</param>
    /// <param name="unit">When this method returns, contains the acronym of measure unit.</param>
    /// <returns>The numeric part of string representation of size.</returns>
    public static string BytesToString(long bytes, out string unit)
    {
        if (bytes >= 1073741824)
        {
            unit = s_list[(int)LocCode.GB];
            return (bytes / 1073741824.0).ToString("0.##");
        }
        else if (bytes >= 1048576)
        {
            unit = s_list[(int)LocCode.MB];
            return (bytes / 1048576.0).ToString("0.#");
        }
        else
        {
            unit = s_list[(int)LocCode.KB];
            return (bytes / 1024.0).ToString("0");
        }
    }
    /// <summary>Retrieves a localized string by its identifier.</summary>
    /// <param name="code">Identifier of the localized string.</param>
    /// <returns>A localized string.</returns>
    public static string GetString(LocCode code) => s_list[(int)code];
    /// <summary>Converts an amount of seconds into its string representation.</summary>
    /// <param name="seconds">Amount of seconds.</param>
    /// <returns>The string representation of time interval.</returns>
    public static string SecondsToString(long seconds)
    {
        if (seconds == 0)
            return string.Concat("0", s_list[(int)LocCode.Second]);
        var resultBuilder = new StringBuilder(6);
        if (seconds >= 3600)
            resultBuilder.Append(seconds / 3600).Append(s_list[(int)LocCode.Hour]);
        long mod = seconds % 3600;
        if (mod >= 60)
            resultBuilder.Append(' ').Append(mod / 60).Append(s_list[(int)LocCode.Minute]);
        if (seconds < 300)
        {
            mod = seconds % 60;
            if (mod != 0)
                resultBuilder.Append(' ').Append(mod).Append(s_list[(int)LocCode.Second]);
        }
        int offset = resultBuilder[0] == ' ' ? 1 : 0;
        return resultBuilder.ToString(offset, resultBuilder.Length - offset);
    }
}