using System;
using System.Text;
using System.Threading;
using DotNet.Testcontainers.Builders;
using Testcontainers.Spire;
using Testcontainers.Client;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Containers;
using System.Threading.Tasks;
using DotNet.Testcontainers.Volumes;

var td1 = "example1.org";
var td2 = "example2.org";
var ss1 = "spire-server1";
var ss2 = "spire-server2";

await using var net = new NetworkBuilder().WithName(Guid.NewGuid().ToString("D")).Build();

// td1
var s1 = CreateSpireServer(td1, net, ss1);
await using var vol1 = new VolumeBuilder().WithName(Guid.NewGuid().ToString("D")).Build();
var a1 = await CreateSpireAgent(td1, ss1, net, vol1);

// td2
var s2 = CreateSpireServer(td2, net, ss2);
await using var vol2 = new VolumeBuilder().WithName(Guid.NewGuid().ToString("D")).Build();
var a2 = await CreateSpireAgent(td2, ss2, net, vol2);

Thread.Sleep(10000);

var c1 = await TestWorkload.Get(vol1, net);
var c2 = await TestWorkload.Get(vol2, net);

Thread.Sleep(60000);

static async Task<IContainer> CreateSpireAgent(string trustDomain, string serverAddress, INetwork net, IVolume vol)
{
    var a = new SpireAgentBuilder()
                .WithNetwork(net)
                .WithAgentVolume(vol)
                .WithTrustDomain(trustDomain)
                .WithServerAddress(serverAddress)
                .WithCommand(
                    "-config", SpireAgentBuilder.ConfigPath,
                    "-serverAddress", serverAddress,
                    "-expandEnv", "true"
                )
                .Build();
    await a.StartAsync();

    return a;
}

static async Task<IContainer> CreateSpireServer(string trustDomain, INetwork net, string networkAlias)
{
    var s = new SpireServerBuilder()
                    .WithTrustDomain(trustDomain)
                    .WithNetworkAliases(networkAlias)
                    .WithNetwork(net)
                    .Build();
    await s.StartAsync();
    await s.ExecAsync([
        "/opt/spire/bin/spire-server", "entry", "create",
        "-parentID", $"spiffe://{trustDomain}/myagent",
        "-spiffeID", $"spiffe://{trustDomain}/myservice",
        "-selector", "docker:label:org.example.workload:client"
    ]);

    var entries = @"
    {
        ""entries"":[
            {
                ""parent_id"": ""spiffe://{trustDomain}/spire/agent/cn/agent.{trustDomain}"",
                ""spiffe_id"": ""spiffe://{trustDomain}/client"",
                ""selectors"": [{
                    ""type"": ""docker"",
                    ""value"": ""label:org.example.workload:client""
                }]
            },
            {
                ""parent_id"": ""spiffe://{trustDomain}/spire/agent/cn/agent.{trustDomain}"",
                ""spiffe_id"": ""spiffe://{trustDomain}/server"",
                ""selectors"": [{
                    ""type"": ""docker"",
                    ""value"": ""label:org.example.workload:server""
                }]
            }
        ]
    }
    ".Replace("{trustDomain}", trustDomain);
    await s.CopyAsync(Encoding.UTF8.GetBytes(entries), "/etc/spire/server/entries.json");
    await s.ExecAsync([
        "/opt/spire/bin/spire-server", "entry", "create", "-data", "/etc/spire/server/entries.json"
    ]);

    return s;
}
