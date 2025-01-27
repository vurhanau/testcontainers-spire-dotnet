using System;
using System.Threading;
using DotNet.Testcontainers.Builders;
using Testcontainers.Spire;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Containers;
using System.Threading.Tasks;
using DotNet.Testcontainers.Volumes;
using DotNet.Testcontainers.Configurations;

var td1 = "example1.com";
var td2 = "example2.com";
var ss1 = "spire-server1";
var ss2 = "spire-server2";

await using var net = new NetworkBuilder().WithName(Guid.NewGuid().ToString("D")).Build();

// td1
var s1 = await CreateSpireServer(net, ss1, td1);
await using var vol1 = CreateVolume(td1);
var a1 = await CreateSpireAgent(net, vol1, ss1, td1);

// td2
var s2 = await CreateSpireServer(net, ss2, td2);
await using var vol2 = CreateVolume(td2);
var a2 = await CreateSpireAgent(net, vol2, ss2, td2);

await s1.ExecAsync([
    "/opt/spire/bin/spire-server", "entry", "create",
    "-parentID", $"spiffe://{td1}/spire/agent/x509pop/cn/agent.example.com",
    "-spiffeID", $"spiffe://{td1}/workload",
    "-selector", "docker:label:com.example:workload"
]);
await s2.ExecAsync([
    "/opt/spire/bin/spire-server", "entry", "create",
    "-parentID", $"spiffe://{td2}/spire/agent/x509pop/cn/agent.example.com",
    "-spiffeID", $"spiffe://{td2}/workload",
    "-selector", "docker:label:com.example:workload"
]);

using IOutputConsumer outputConsumer = Consume.RedirectStdoutAndStderrToConsole();
var c1 = await CreateWorkload(net, vol1, outputConsumer);
var c2 = await CreateWorkload(net, vol2, outputConsumer);

Thread.Sleep(60000);

static IVolume CreateVolume(string td)
{
    return new VolumeBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();
}

static async Task<IContainer> CreateSpireAgent(INetwork net, IVolume vol, string serverAddress, string trustDomain)
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

static async Task<IContainer> CreateSpireServer(INetwork net, string networkAlias, string trustDomain)
{
    var s = new SpireServerBuilder()
                    .WithTrustDomain(trustDomain)
                    .WithNetworkAliases(networkAlias)
                    .WithNetwork(net)
                    .Build();
    await s.StartAsync();

    return s;
}

static async Task<IContainer> CreateWorkload(INetwork net, IVolume vol, IOutputConsumer outputConsumer)
{
    var w = new ContainerBuilder()
                    .WithImage(SpireAgentBuilder.SpireAgentImage)
                    .WithNetwork(net)
                    .WithVolumeMount(vol, "/tmp/spire/agent/public")
                    .WithLabel("com.example", "workload")
                    .WithPrivileged(true)
                    .WithCreateParameterModifier(parameterModifier =>
                    {
                        parameterModifier.HostConfig.PidMode = "host";
                        parameterModifier.HostConfig.CgroupnsMode = "host";
                    })
                    .WithEntrypoint(
                        "/opt/spire/bin/spire-agent", "api", "fetch"
                    )
                    .WithCommand(
                        "-socketPath", "/tmp/spire/agent/public/api.sock"
                    )
                    .WithOutputConsumer(outputConsumer)
                    .Build();
    await w.StartAsync();

    return w;
}
