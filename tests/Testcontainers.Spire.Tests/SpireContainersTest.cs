using System;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.Spire.Tests;

public class SpireContainersTest
{
    [Fact(Timeout = 60_000)]
    public async Task StartTest()
    {
        var td = "example.com";

        await using var net = new NetworkBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();
        await using var vol = new VolumeBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();
        var cout = Consume.RedirectStdoutAndStderrToConsole();

        var s = new SpireServerBuilder().WithNetwork(net).WithOutputConsumer(cout).Build();
        await s.StartAsync();

        var cert = Encoding.UTF8.GetString(await s.ReadFileAsync("/etc/spire/server/agent.crt"));
        Console.WriteLine("Agent cert:\n" + cert);

        var a = new SpireAgentBuilder().WithNetwork(net).WithAgentVolume(vol).WithOutputConsumer(cout).Build();
        await a.StartAsync();

        await s.ExecAsync([
            "/opt/spire/bin/spire-server", "entry", "create",
            "-parentID", $"spiffe://{td}/spire/agent/x509pop/cn/agent.example.com",
            "-spiffeID", $"spiffe://{td}/workload",
            "-selector", "docker:label:com.example:workload"
        ]);

        string expr = @$"msg=""SVID updated"" entry=[\w-]+ spiffe_id=""spiffe://{td}/workload"" subsystem_name=cache_manager$";
        await a.AssertLogAsync(expr, 10);
        
        var w = new ContainerBuilder()
                        .WithImage(Defaults.AgentImage)
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
                        .WithOutputConsumer(cout)
                        .Build();
        await w.StartAsync();

        await w.AssertLogAsync("Received 1 svid after", 10);
    }
}
