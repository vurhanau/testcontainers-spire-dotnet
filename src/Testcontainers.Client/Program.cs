using System;
using System.Text;
using System.Threading;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Testcontainers.Spire;

var entries = @"
{
    ""entries"":[
        {
            ""parent_id"": ""spiffe://example.org/spire/agent/cn/agent.example.com"",
            ""spiffe_id"": ""spiffe://example.org/client"",
            ""selectors"": [{
                ""type"": ""docker"",
                ""value"": ""label:org.example.workload:client""
            }]
        },
        {
            ""parent_id"": ""spiffe://example.org/spire/agent/cn/agent.example.com"",
            ""spiffe_id"": ""spiffe://example.org/server"",
            ""selectors"": [{
                ""type"": ""docker"",
                ""value"": ""label:org.example.workload:server""
            }]
        }
    ]
}
";

await using var net = new NetworkBuilder().WithName(Guid.NewGuid().ToString("D")).Build();
await using var vol = new VolumeBuilder().WithName(Guid.NewGuid().ToString("D")).Build();

var s = new SpireServerBuilder().WithNetwork(net).Build();
await s.StartAsync();

await s.ExecAsync([
    "/opt/spire/bin/spire-server", "entry", "create",
    "-parentID", "spiffe://example.org/myagent",
    "-spiffeID", "spiffe://example.org/myservice",
    "-selector", "docker:label:org.example.workload:client"
]);
await s.CopyAsync(Encoding.UTF8.GetBytes(entries), "/etc/spire/server/entries.json");
await s.ExecAsync([
    "/opt/spire/bin/spire-server", "entry", "create", "-data", "/etc/spire/server/entries.json"
]);

var a = new SpireAgentBuilder()
                .WithNetwork(net)
                .WithVolumeMount(vol)
                .WithCommand(
                    "-config", SpireAgentBuilder.ConfigPath,
                    "-serverAddress", Defaults.ServerNetworkAlias,
                    "-expandEnv", "true"
                )
                .Build();
await a.StartAsync();

Thread.Sleep(3000);

using IOutputConsumer outputConsumer = Consume.RedirectStdoutAndStderrToConsole();
var w = new ContainerBuilder()
                .WithImage(SpireAgentBuilder.SpireAgentImage)
                .WithNetwork(net)
                .WithVolumeMount(vol, "/tmp/spire/agent/public")
                .WithLabel("org.example.workload", "client")
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
Thread.Sleep(60000);