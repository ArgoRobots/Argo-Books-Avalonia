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

    /// <summary>
    /// Whether the session shut down normally. False marks a SessionEnd reconstructed on
    /// the next launch from a leftover <see cref="Services.SessionSentinel"/>, meaning the
    /// run was force-quit, cut off by an OS restart, or lost to a power failure.
    /// <para>
    /// A flag rather than a third <see cref="SessionAction"/> so that a client running
    /// ahead of the server degrades safely: the upload filter drops an unknown field but
    /// coerces an unknown enum value to "Unknown", which would break session pairing
    /// outright. Null on SessionStart, and on ends from builds predating this field, where
    /// the reader should assume clean.
    /// </para>
    /// </summary>
    public bool? Clean { get; set; }
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
