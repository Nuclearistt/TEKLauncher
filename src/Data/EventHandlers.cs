namespace TEKLauncher.Data;

/// <summary>Container for event handlers used by <see cref="Downloader"/> and <see cref="Steam.Client"/>.</summary>
class EventHandlers
{
    /// <summary>Handler for validation start event.</summary>
    public Action? StartValidation;
    /// <summary>Handler for progress initialization event.</summary>
    public Action<bool, long>? PrepareProgress;
    /// <summary>Handler for validation counters update event.</summary>
    public Action<int, int, int>? UpdateCounters;
    /// <summary>Handler for progress update event.</summary>
    public Action<long>? UpdateProgress;
    /// <summary>Handler for status set event.</summary>
    public Action<string, int>? SetStatus;
    /// <summary>Handler for stage set event.</summary>
    public Action<LocCode, bool>? SetStage;
}