using System;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Volumes;
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

        var so = new ServerOptions();
        so.Conf.LogLevel = "DEBUG";
        var s = new SpireServerBuilder().WithNetwork(net).WithOptions(so).WithOutputConsumer(output).Build();
        await s.StartAsync();

        var ao = new AgentOptions();
        ao.Conf.LogLevel = "DEBUG";
        var socketPath = ao.Conf.SocketPath;
        var socketDir = Path.GetDirectoryName(socketPath);
        var a = new SpireAgentBuilder()
                        .WithNetwork(net)
                        .WithBindMount("/var/run/docker.sock", ao.Conf.DockerSocketPath)
                        .WithVolumeMount(vol, socketDir)
                        .WithOptions(ao)
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
                        .WithVolumeMount(vol, socketDir)
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
                            "-socketPath", socketPath
                        )
                        .WithOutputConsumer(output)
                        .Build();
        await w.StartAsync();

        await w.AssertLogAsync("Received 1 svid after", 10);
    }

    [Fact(Timeout = 60_000)]
    public async Task StartFederationTest()
    {
        var output = Consume.RedirectStdoutAndStderrToConsole();
        var net = new NetworkBuilder().WithName( "spire-federation-" + Guid.NewGuid().ToString("D")).Build();

        async Task<(SpireServerContainer Srv, SpireAgentContainer Agt, Func<Task> Close)> Start(ServerConf sc, AgentConf ac)
        {
            var td = sc.TrustDomain;
            var vol = new VolumeBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();
            var so = new ServerOptions{ Conf = sc };
            var s = new SpireServerBuilder()
                            .WithNetworkAliases(ac.ServerAddress)
                            .WithNetwork(net)
                            .WithOptions(so)
                            .WithOutputConsumer(output)
                            .Build();
            await s.StartAsync();

            var ao = new AgentOptions(){ Conf = ac };
            var socketDir = Path.GetDirectoryName(ac.SocketPath);
            var a = new SpireAgentBuilder()
                            .WithNetwork(net)
                            .WithBindMount("/var/run/docker.sock", ao.Conf.DockerSocketPath)
                            .WithVolumeMount(vol, socketDir)
                            .WithOptions(ao)
                            .WithOutputConsumer(output)
                            .Build();
            await a.StartAsync();

            await s.ExecAsync([
                "/opt/spire/bin/spire-server", "entry", "create",
                "-parentID", $"spiffe://{td}/spire/agent/x509pop/cn/agent.example.com",
                "-spiffeID", $"spiffe://{td}/workload",
                "-selector", "docker:label:com.example:workload"
            ]);

            return (s, a, async () =>
            {
                try { await a.DisposeAsync(); } catch {}
                try { await s.DisposeAsync(); } catch {}
                try { await vol.DisposeAsync(); } catch {}
                try { await net.DisposeAsync(); } catch {}
            });
        }

        Func<Task> Close1 = () => Task.CompletedTask;
        Func<Task> Close2 = () => Task.CompletedTask;

        try
        {
            var sc1 = new ServerConf
            {
                TrustDomain = "example1.org",
                Federation = new ServerConfFederation
                {
                    FederatesWith = [ new() { TrustDomain = "example2.org", Host = "spire-server2", } ]
                }
            };
            var ac1 = new AgentConf{ TrustDomain = "example1.org", ServerAddress = "spire-server1", };
            (SpireServerContainer s1, SpireAgentContainer a1, Close1) = await Start(sc1, ac1);

            var sc2 = new ServerConf
            {
                TrustDomain = "example2.org",
                Federation = new ServerConfFederation
                {
                    FederatesWith = [ new() { TrustDomain = "example1.org", Host = "spire-server1", } ]
                }
            };
            var ac2 = new AgentConf{ TrustDomain = "example2.org", ServerAddress = "spire-server2", };
            (SpireServerContainer s2, SpireAgentContainer a2, Close2) = await Start(sc2, ac2);
        }
        finally
        {
            await Close1();
            await Close2();
        }
    }
}
