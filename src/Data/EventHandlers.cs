namespace TEKLauncher.Data;

/// <summary>Container for event handlers used by <see cref="Downloader"/> and <see cref="Steam.Client"/>.</summary>
class EventHandlers
{
    /// <summary>Handler for progress initialization event.</summary>
    public Action<bool, long>? PrepareProgress;
    /// <summary>Handler for progress update event.</summary>
    public Action<long>? UpdateProgress;
    /// <summary>Handler for status set event.</summary>
    public Action<string, int>? SetStatus;
}