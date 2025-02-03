using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.Spire.Tests;

public class SpireContainersTest
{
    [Fact]//(Timeout = 60_000)]
    public async Task StartTest()
    {
        var td = "example.com";

        await using var net = new NetworkBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();
        await using var vol = new VolumeBuilder().WithName(td + "-" + Guid.NewGuid().ToString("D")).Build();

        var s = new SpireServerBuilder().WithNetwork(net).Build();
        await s.StartAsync();
        await s.AssertLog("msg=\"Initializing health checkers\"", 10);

        var a = new SpireAgentBuilder().WithNetwork(net).WithAgentVolume(vol).Build();
        await a.StartAsync();
        await a.AssertLog("msg=\"Initializing health checkers\"", 10);

        await s.ExecAsync([
            "/opt/spire/bin/spire-server", "entry", "create",
            "-parentID", $"spiffe://{td}/spire/agent/x509pop/cn/agent.example.com",
            "-spiffeID", $"spiffe://{td}/workload",
            "-selector", "docker:label:com.example:workload"
        ]);
        // todo: assert log
        // server:
        // msg="Agent attestation request completed" address="172.25.0.3:51972" agent_id="spiffe://example.com/spire/agent/x509pop/cn/agent.example.com"
        // msg="Signed X509 SVID" authorized_as=agent authorized_via=datastore caller_addr="172.25.0.3:51974" caller_id="spiffe://example.com/spire/agent/x509pop/cn/agent.example.com" entry_id=c6618b1d-ebc0-4476-b747-f0a312f744af expiration="2025-02-04T00:34:01Z" method=BatchNewX509SVID request_id=1b61741a-1d5c-49c6-a0ac-d99c07f41078 revision_number=0 serial_num=6336550875125993042626296812949540257 service=svid.v1.SVID spiffe_id="spiffe://example.com/workload" subsystem_name=api
        // msg="SVID updated" entry=c6618b1d-ebc0-4476-b747-f0a312f744af spiffe_id="spiffe://example.com/workload" subsystem_name=cache_manager
        
        using IOutputConsumer outputConsumer = Consume.RedirectStdoutAndStderrToConsole();
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
                        .WithOutputConsumer(outputConsumer)
                        .Build();
        await w.StartAsync();
        // Received 1 svid after
    }
}
