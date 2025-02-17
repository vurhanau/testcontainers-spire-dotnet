# Spiffe.Testcontainers.Spire

.NET library for [SPIRE](https://github.com/spiffe/spire) testing via [Testcontainers](https://testcontainers.com/).

## Usage
Install the NuGet dependency
```
dotnet add package Spiffe.Testcontainers.Spire
```

Run the container
```csharp
await using var network = new NetworkBuilder().WithName("network-example.com").Build();

// Start Spire server
var server = new SpireServerBuilder().WithNetwork(network).Build();
await server.StartAsync();

// Start Spire agent
await using var volume = new VolumeBuilder().WithName("volume-example.com").Build();
var agent = new SpireAgentBuilder()
                    .WithNetwork(network)
                    .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock")
                    .WithVolumeMount(volume, "/tmp/spire/agent/public")
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
