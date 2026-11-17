using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace ShipmentTracker.Api;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShipmentStatus { Booked, InTransit, AtPort, Delivered }

public record Shipment(string Id, string Origin, string Destination, ShipmentStatus Status, DateTime UpdatedAtUtc);

public record CreateShipmentRequest(string? Origin, string? Destination);

public record UpdateStatusRequest(string? Status);

public enum UpdateResult { Updated, NotFound, AlreadyDelivered }

// In-memory store. Thread-safe because many requests can hit it at the same time.
public class ShipmentStore
{
    private readonly ConcurrentDictionary<string, Shipment> _shipments = new();
    private int _nextNumber = 1000;

    public ShipmentStore()
    {
        Create("Chennai", "Rotterdam");
        Create("Melbourne", "Singapore");
    }

    public IReadOnlyList<Shipment> GetAll() =>
        _shipments.Values.OrderBy(s => s.Id).ToList();

    public Shipment? Get(string id) =>
        _shipments.TryGetValue(id.ToUpperInvariant(), out var shipment) ? shipment : null;

    public Shipment Create(string origin, string destination)
    {
        var id = $"SHP-{Interlocked.Increment(ref _nextNumber)}";
        var shipment = new Shipment(id, origin, destination, ShipmentStatus.Booked, DateTime.UtcNow);
        _shipments[id] = shipment;
        return shipment;
    }

    public UpdateResult UpdateStatus(string id, ShipmentStatus newStatus)
    {
        var current = Get(id);
        if (current is null) return UpdateResult.NotFound;
        if (current.Status == ShipmentStatus.Delivered) return UpdateResult.AlreadyDelivered;

        _shipments[current.Id] = current with { Status = newStatus, UpdatedAtUtc = DateTime.UtcNow };
        return UpdateResult.Updated;
    }
}
