namespace ArgoBooks.Core.Services.Sync;

public sealed class QueueItem { public int Id { get; set; } public string Ciphertext { get; set; } = string.Empty; }
public sealed class ServerDevice { public int Id { get; set; } public string DeviceLabel { get; set; } = string.Empty; public DateTime? LastSeenAt { get; set; } }
