using System.Globalization;
using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;

namespace Testcontainers.Spire.Tests;

public class ConnectServerAgentTest
{
    [Fact]
    public async Task ConnectServerAndAgentTest()
    {
        const string hostName = "spire-server";

        var network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
        // --user 1000:1000 \
        // -p 8081:8081 \
        // -v $(realpath src)/Testcontainers.Spire/conf/server:/etc/spire/server \
        // ghcr.io/spiffe/spire-server:1.10.0 \
        // -config /etc/spire/server/server.conf

        var server = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("ghcr.io/spiffe/spire-server:1.10.0")
            .WithPortBinding(8081, 8081)
            .WithBindMount(@"/Users/avurhanau/Projects/testcontainers-spire-dotnet/src/Testcontainers.Spire/conf/server", "/etc/spire/server")
            .WithCommand("-config", "/etc/spire/server/server.conf")
            .WithNetwork(network)
            .WithNetworkAliases(hostName)
            .Build();

		// -p 8080:8080 \
		// -v $(realpath src)/Testcontainers.Spire/conf/agent:/etc/spire/agent \
		// ghcr.io/spiffe/spire-agent:1.10.0 \
		// -config /etc/spire/agent/agent.conf
        var agent = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("ghcr.io/spiffe/spire-agent:1.10.0")
            .WithPortBinding(8080, 8080)
            .WithBindMount(@"/Users/avurhanau/Projects/testcontainers-spire-dotnet/src/Testcontainers.Spire/conf/agent", "/etc/spire/agent")
            .WithCommand("-config", "/etc/spire/agent/agent.conf", "-serverAddress", "spire-server:8081")
            .WithNetwork(network)
            .Build();

        await network.CreateAsync();
        await server.StartAsync();
        await agent.StartAsync();

        Console.WriteLine($"ok");
    }
}