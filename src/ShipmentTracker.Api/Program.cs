using ShipmentTracker.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ShipmentStore>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/shipments", (ShipmentStore store) => Results.Ok(store.GetAll()));

app.MapGet("/shipments/{id}", (string id, ShipmentStore store) =>
    store.Get(id) is { } shipment ? Results.Ok(shipment) : Results.NotFound());

app.MapPost("/shipments", (CreateShipmentRequest request, ShipmentStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Origin) || string.IsNullOrWhiteSpace(request.Destination))
        return Results.BadRequest(new { error = "Origin and destination are required." });

    var shipment = store.Create(request.Origin.Trim(), request.Destination.Trim());
    return Results.Created($"/shipments/{shipment.Id}", shipment);
});

app.MapPatch("/shipments/{id}/status", (string id, UpdateStatusRequest request, ShipmentStore store) =>
{
    if (!Enum.TryParse<ShipmentStatus>(request.Status, ignoreCase: true, out var newStatus))
        return Results.BadRequest(new { error = $"Unknown status '{request.Status}'." });

    return store.UpdateStatus(id, newStatus) switch
    {
        UpdateResult.NotFound => Results.NotFound(),
        UpdateResult.AlreadyDelivered => Results.Conflict(new { error = "Delivered shipments cannot change status." }),
        _ => Results.Ok(store.Get(id))
    };
});

app.Run();

// Lets the test project start the API in memory with WebApplicationFactory.
public partial class Program { }
