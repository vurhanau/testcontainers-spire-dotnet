using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Volumes;
using Testcontainers.Spire;

namespace Testcontainers.Client
{
    public class TestWorkload
    {
        public static async Task<IContainer> Get(IVolume vol, INetwork net)
        {
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
            return w;
        }
    }
}