namespace ArgoBooks.Core.Services.Sync;

public sealed class QueueItem { public int Id { get; set; } public string Ciphertext { get; set; } = string.Empty; }
public sealed class ServerDevice { public int Id { get; set; } public string DeviceLabel { get; set; } = string.Empty; public DateTime? LastSeenAt { get; set; } }

/// <summary>Result of <c>pair/create</c>: the long-lived pairing token used for follow-up calls, plus the short code shown to the user.</summary>
public sealed record PairingCreation(string Token, string ShortCode);

/// <summary>Result of <c>pair/status</c>: current pairing state and, once the phone has responded, its public key.</summary>
public sealed record PairingStatusResult(string Status, string? PhonePublicKey);
