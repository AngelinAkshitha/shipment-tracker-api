# Stage 1: build and publish with the full .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY src/ShipmentTracker.Api/ShipmentTracker.Api.csproj src/ShipmentTracker.Api/
RUN dotnet restore src/ShipmentTracker.Api/ShipmentTracker.Api.csproj
COPY src/ src/
RUN dotnet publish src/ShipmentTracker.Api/ShipmentTracker.Api.csproj -c Release -o /app --no-restore

# Stage 2: run on the small ASP.NET runtime image only
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ShipmentTracker.Api.dll"]
