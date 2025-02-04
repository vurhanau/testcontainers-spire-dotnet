# Testcontainers.Spire

.NET library for [SPIRE](https://github.com/spiffe/spire) testing via [Testcontainers](https://testcontainers.com/).

## Usage
Install the NuGet dependency
```
dotnet add package Testcontainers.Spire
```

Run the container
```csharp
// Create a shared network
await using var net = new NetworkBuilder().WithName("example.com-network").Build();

// Create and start SPIRE Server
var server = new SpireServerBuilder().WithNetwork(net).Build();
await server.StartAsync();

// Create and start SPIRE Agent
await using var vol = new VolumeBuilder().WithName("example.com-volume").Build();
var agent = new SpireAgentBuilder().WithNetwork(net).WithAgentVolume(vol).Build();
await agent.StartAsync();

// Create a workload entry
await server.ExecAsync([
    "/opt/spire/bin/spire-server", "entry", "create",
    "-parentID", $"spiffe://{td}/spire/agent/x509pop/cn/agent.example.com",
    "-spiffeID", $"spiffe://{td}/workload",
    "-selector", "docker:label:com.example:workload"
]);
```