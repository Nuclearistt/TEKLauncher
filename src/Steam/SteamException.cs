namespace TEKLauncher.Steam;

/// <summary>
/// An exception that occurred in Steam client and is not critical for app functioning,
/// usually to display a user-friendly message.
/// </summary>
class SteamException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SteamException"/> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public SteamException(string message) : base(message) { }
}