namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// Session start or end event.
/// </summary>
public class SessionEvent : TelemetryEvent
{
    /// <inheritdoc />
    public override TelemetryDataType DataType => TelemetryDataType.Session;

    /// <summary>
    /// Session action type.
    /// </summary>
    public SessionAction Action { get; set; }

    /// <summary>
    /// Session duration in seconds (only for SessionEnd events).
    /// </summary>
    public long? DurationSeconds { get; set; }
}

/// <summary>
/// Session action types.
/// </summary>
public enum SessionAction
{
    /// <summary>
    /// Application started.
    /// </summary>
    SessionStart,

    /// <summary>
    /// Application closed.
    /// </summary>
    SessionEnd
}
