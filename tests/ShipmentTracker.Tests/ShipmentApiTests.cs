using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ShipmentTracker.Api;

namespace ShipmentTracker.Tests;

// Starts the real API in memory and calls it over HTTP, like a client would.
public class ShipmentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ShipmentApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateShipment_ThenGetIt_ReturnsSameShipment()
    {
        var create = await _client.PostAsJsonAsync("/shipments", new { origin = "Melbourne", destination = "Tokyo" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<Shipment>();
        var fetched = await _client.GetFromJsonAsync<Shipment>($"/shipments/{created!.Id}");

        Assert.Equal("Tokyo", fetched!.Destination);
        Assert.Equal(ShipmentStatus.Booked, fetched.Status);
    }

    [Fact]
    public async Task CreateShipment_WithoutDestination_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/shipments", new { origin = "Melbourne", destination = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetShipment_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/shipments/SHP-9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WithUnknownStatus_ReturnsBadRequest()
    {
        var response = await _client.PatchAsJsonAsync("/shipments/SHP-1001/status", new { status = "Lost" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

// Tests the business rules directly, without HTTP.
public class ShipmentStoreTests
{
    [Fact]
    public void DeliveredShipment_CannotChangeStatus()
    {
        var store = new ShipmentStore();
        var shipment = store.Create("Chennai", "Hamburg");

        Assert.Equal(UpdateResult.Updated, store.UpdateStatus(shipment.Id, ShipmentStatus.Delivered));
        Assert.Equal(UpdateResult.AlreadyDelivered, store.UpdateStatus(shipment.Id, ShipmentStatus.InTransit));
    }

    [Fact]
    public void NewShipments_GetUniqueIds()
    {
        var store = new ShipmentStore();
        var a = store.Create("A", "B");
        var b = store.Create("C", "D");

        Assert.NotEqual(a.Id, b.Id);
    }
}
