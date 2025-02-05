# Testcontainers.Spire

.NET library for [SPIRE](https://github.com/spiffe/spire) testing via [Testcontainers](https://testcontainers.com/).

## Usage
Install the NuGet dependency
```
dotnet add package Spiffe.Testcontainers.Spire
```

Run the container
```csharp
// Create a shared network
await using var net = new NetworkBuilder().WithName("example.com-network").Build();

// Create and start SPIRE Server
var server = new SpireServerBuilder()
                  .WithTrustDomain("example.com")
                  .WithNetwork(net)
                  .Build();
await server.StartAsync();

// Create and start SPIRE Agent
await using var vol = new VolumeBuilder().WithName("example.com-volume").Build();
var agent = new SpireAgentBuilder()
                  .WithTrustDomain("example.com")
                  .WithNetwork(net)
                  .WithAgentVolume(vol)
                  .Build();
await agent.StartAsync();

// Create a workload entry
await server.ExecAsync([
    "/opt/spire/bin/spire-server", "entry", "create",
    "-parentID", $"spiffe://example.com/spire/agent/x509pop/cn/agent.example.com",
    "-spiffeID", $"spiffe://example.com/workload",
    "-selector", "docker:label:com.example:workload"
]);
```
