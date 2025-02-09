using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Spiffe.Testcontainers.Spire.Agent;
using Spiffe.Testcontainers.Spire.Server;

namespace Spiffe.Testcontainers.Spire.Tests;

public class SpireContainersTest
{
    [Fact]
    public void RenderServerConfigTest()
    {
        var c = new ServerConf()
        {
            Federation = new()
            {
                Port = 8082,
                FederatesWith =
                [
                    new() { TrustDomain = "example1.org", Host = "spire-server1", Port = 8443 },
                    new() { TrustDomain = "example2.org", Host = "spire-server2", Port = 8443 },
                ],
            },
        };
        var result = c.Render();
        Assert.NotEmpty(result);
        Assert.Contains(c.TrustDomain, result);
        Assert.Contains(c.LogLevel, result);
        Assert.Contains(c.CaBundlePath, result);
        Assert.Contains(c.KeyFilePath, result);
        Assert.Contains(c.CertFilePath, result);

        var f = c.Federation;
        Assert.Contains(f.Port.ToString(), result);
        foreach (var fi in  f.FederatesWith)
        {
            Assert.Contains(fi.TrustDomain, result);
            Assert.Contains(fi.Host, result);
            Assert.Contains(fi.Port.ToString(), result);
        }
    }

    [Fact]
    public void RenderAgentConfigTest()
    {
        var c = new AgentConf();
        var result = c.Render();
        Assert.NotEmpty(result);
        Assert.Contains(c.ServerAddress, result);
        Assert.Contains(c.ServerPort.ToString(), result);
        Assert.Contains(c.SocketPath, result);
        Assert.Contains(c.TrustBundlePath, result);
        Assert.Contains(c.TrustDomain, result);
        Assert.Contains(c.DataDir, result);
        Assert.Contains(c.LogLevel, result);
        Assert.Contains(c.CertFilePath, result);
        Assert.Contains(c.KeyFilePath, result);
        Assert.Contains(c.DockerSocketPath, result);
    }

    [Fact(Timeout = 60_000)]
    public async Task StartTest()
    {
        var td = "example.com";

        await using var net = new NetworkBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();
        await using var vol = new VolumeBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();
        var output = Consume.RedirectStdoutAndStderrToConsole();

        var serverOptions = new ServerOptions();
        serverOptions.Conf.LogLevel = "DEBUG";
        var s = new SpireServerBuilder().WithNetwork(net).WithOptions(serverOptions).WithOutputConsumer(output).Build();
        await s.StartAsync();

        var agentSocketDir = "/tmp/spire/agent/public";
        var agentSocketPath = agentSocketDir + "/api.sock";
        var agentOptions = new AgentOptions();
        var c = agentOptions.Conf;
        c.SocketPath = agentSocketPath;
        var a = new SpireAgentBuilder()
                        .WithNetwork(net)
                        .WithBindMount("/var/run/docker.sock", c.DockerSocketPath)
                        .WithVolumeMount(vol, agentSocketPath)
                        .WithOptions(agentOptions)
                        .WithOutputConsumer(output)
                        .Build();
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
                        .WithVolumeMount(vol, agentSocketDir)
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
                            "-socketPath", agentSocketPath
                        )
                        .WithOutputConsumer(output)
                        .Build();
        await w.StartAsync();

        await w.AssertLogAsync("Received 1 svid after", 10);
    }
}
