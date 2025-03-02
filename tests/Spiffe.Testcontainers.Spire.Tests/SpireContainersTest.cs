using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Networks;
using Spiffe.Testcontainers.Spire.Agent;
using Spiffe.Testcontainers.Spire.Server;

namespace Spiffe.Testcontainers.Spire.Tests;

public class SpireContainersTest
{
  [Fact]
  public void RenderServerConfigTest()
  {
    var c = new ServerConf
    {
      Federation = new ServerConfFederation
      {
        Port = 8082,
        FederatesWith =
        [
          new ServerConfFederationWith { TrustDomain = "example1.org", Host = "spire-server1", Port = 8443 },
          new ServerConfFederationWith { TrustDomain = "example2.org", Host = "spire-server2", Port = 8443 }
        ]
      }
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
    foreach (var fi in f.FederatesWith)
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

  [Fact(Timeout = 180_000)]
  public async Task StartTest()
  {
    var td = "example.com";

    await using var net = new NetworkBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();
    await using var vol = new VolumeBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();
    var output = Consume.RedirectStdoutAndStderrToConsole();

    var so = new ServerOptions
    {
      Conf =
      {
        LogLevel = "DEBUG"
      }
    };
    var s = new SpireServerBuilder().WithNetwork(net).WithOptions(so).WithOutputConsumer(output).Build();
    await s.StartAsync();

    var ao = new AgentOptions
    {
      Conf =
      {
        LogLevel = "DEBUG"
      }
    };
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

    var expr = $"""msg="SVID updated" entry=[\w-]+ spiffe_id="spiffe://{td}/workload" subsystem_name=cache_manager$""";
    await a.AssertLogAsync(expr, 20);

    var w = new ContainerBuilder()
      .WithImage(SpireAgentBuilder.Image)
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

    await w.AssertLogAsync("Received 1 svid after", 20);
  }

  [Fact(Timeout = 180_000)]
  public async Task StartFederationTest()
  {
    using var output = Consume.RedirectStdoutAndStderrToConsole();
    await using var net = new NetworkBuilder()
      .WithName("spire-federation-" + Guid.NewGuid().ToString("D"))
      .Build();

    var close0 = () => Task.CompletedTask;
    var close1 = () => Task.CompletedTask;
    try
    {
      var td = await Task.WhenAll(
        Start("example1.org", "spire-server1", net, output),
        Start("example2.org", "spire-server2", net, output));
      close0 = td[0].Close;
      close1 = td[1].Close;

      await Task.WhenAll(
        Federate(td[0].Srv, td[1].Srv, "example1.org"),
        Federate(td[1].Srv, td[0].Srv, "example2.org")
      );

      // Bundle refresh interval
      await Task.Delay(60_000);
      await Task.WhenAll(
        td[0].Srv.AssertLogAsync(
          "level=info msg=\"Bundle refreshed\" subsystem_name=bundle_client trust_domain=example1.org", 80),
        td[1].Srv.AssertLogAsync(
          "level=info msg=\"Bundle refreshed\" subsystem_name=bundle_client trust_domain=example2.org", 80)
      );
    }
    catch (Exception e)
    {
      Assert.Fail(e.ToString());
    }
    finally
    {
      await close0();
      await close1();
    }
  }

  private static async Task Federate(SpireServerContainer from, SpireServerContainer to, string trustDomain)
  {
    var resp = await from.ExecAsync([
      "/opt/spire/bin/spire-server", "bundle", "show", "-format", "spiffe"
    ]);
    Assert.Empty(resp.Stderr);

    var bundle = Encoding.UTF8.GetBytes(resp.Stdout);
    var bundlePath = $"/tmp/{trustDomain}.bundle";
    await to.CopyAsync(bundle, bundlePath);
    resp = await to.ExecAsync([
      "/opt/spire/bin/spire-server", "bundle", "set", "-format", "spiffe", "-id", $"spiffe://{trustDomain}", "-path",
      bundlePath
    ]);
    Assert.Empty(resp.Stderr);
    Assert.Contains("bundle set", resp.Stdout);
  }

  private static async Task<(SpireServerContainer Srv, Func<Task> Close)> Start(
    string trustDomain,
    string serverHost,
    INetwork net,
    IOutputConsumer output)
  {
    var sc = new ServerConf
    {
      TrustDomain = trustDomain,
      Federation = new ServerConfFederation
      {
        Port = 8082, FederatesWith =
        [
          new ServerConfFederationWith
          {
            TrustDomain = trustDomain,
            Host = serverHost,
            Port = 8082
          }
        ]
      }
    };
    var ac = new AgentConf { TrustDomain = trustDomain, ServerAddress = serverHost };
    var vol = new VolumeBuilder().WithName(trustDomain + "-" + Guid.NewGuid().ToString("D")).Build();
    var so = new ServerOptions { Conf = sc };
    var s = new SpireServerBuilder()
      .WithNetworkAliases(ac.ServerAddress)
      .WithNetwork(net)
      .WithOptions(so)
      .WithOutputConsumer(output)
      .Build();
    await s.StartAsync();

    var ao = new AgentOptions { Conf = ac };
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
      "-parentID", $"spiffe://{trustDomain}/spire/agent/x509pop/cn/agent.example.com",
      "-spiffeID", $"spiffe://{trustDomain}/workload",
      "-selector", "docker:label:com.example:workload"
    ]);

    return (s, async () =>
    {
      await Close(s);
      await Close(vol);
    });

    async Task Close(IAsyncDisposable d)
    {
      try
      {
        await d.DisposeAsync();
      }
      catch
      {
        // ignored
      }
    }
  }
}
