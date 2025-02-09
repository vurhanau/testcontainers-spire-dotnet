using System;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;
using System.Text;
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

    [Fact(Timeout = 180_000)]
    public async Task StartFederationTest()
    {
        using var output = Consume.RedirectStdoutAndStderrToConsole();
        await using var net = new NetworkBuilder().WithName("spire-federation-" + Guid.NewGuid().ToString("D")).Build();

        async Task<(SpireServerContainer Srv, Func<Task> Close)> Start(ServerConf sc, AgentConf ac)
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

            return (s, async () =>
            {
                try { await a.DisposeAsync(); } catch {}
                try { await s.DisposeAsync(); } catch {}
                try { await vol.DisposeAsync(); } catch {}
            });
        }

        async Task Federate(SpireServerContainer from, SpireServerContainer to, string trustDomain)
        {
            var resp = await from.ExecAsync([
                "/opt/spire/bin/spire-server", "bundle", "show", "-format", "spiffe"
            ]);
            Assert.Empty(resp.Stderr);

            var bundle = Encoding.UTF8.GetBytes(resp.Stdout);
            var bundlePath = $"/tmp/{trustDomain}.bundle";
            await to.CopyAsync(bundle, bundlePath);
            resp = await to.ExecAsync([
                "/opt/spire/bin/spire-server", "bundle", "set", "-format", "spiffe", "-id", $"spiffe://{trustDomain}", "-path", bundlePath
            ]);
            Assert.Empty(resp.Stderr);
            Assert.Contains("bundle set", resp.Stdout);
        }

        Func<Task> close1 = () => Task.CompletedTask;
        Func<Task> close2 = () => Task.CompletedTask;
        try
        {
            var sc1 = new ServerConf
            {
                TrustDomain = "example1.org",
                Federation = new ServerConfFederation
                {
                    Port = 8082,
                    FederatesWith = [ new() { TrustDomain = "example2.org", Host = "spire-server2", Port = 8082, } ]
                }
            };
            var sc2 = new ServerConf
            {
                TrustDomain = "example2.org",
                Federation = new ServerConfFederation
                {
                    Port = 8082,
                    FederatesWith = [ new() { TrustDomain = "example1.org", Host = "spire-server1", Port = 8082, } ]
                }
            };
            var ac1 = new AgentConf{ TrustDomain = "example1.org", ServerAddress = "spire-server1", };
            var ac2 = new AgentConf{ TrustDomain = "example2.org", ServerAddress = "spire-server2", };

            var td = await Task.WhenAll(Start(sc1, ac1), Start(sc2, ac2));
            close1 = td[0].Close;
            close2 = td[1].Close;

            await Task.WhenAll(
                Federate(td[0].Srv, td[1].Srv, "example1.org"),
                Federate(td[1].Srv, td[0].Srv, "example2.org")
            );

            // Bundle refresh interval
            await Task.Delay(60_000);
            await Task.WhenAll(
                td[0].Srv.AssertLogAsync($"level=info msg=\"Bundle refreshed\" subsystem_name=bundle_client trust_domain=example2.org", 60),
                td[1].Srv.AssertLogAsync($"level=info msg=\"Bundle refreshed\" subsystem_name=bundle_client trust_domain=example1.org", 60)
            );
        }
        catch (Exception e)
        {
            Assert.Fail(e.ToString());
        }
        finally
        {
            await close1();
            await close2();
        }
    }
}
