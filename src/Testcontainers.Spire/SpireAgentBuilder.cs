using System;
using System.Linq;
using System.Text;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Volumes;

namespace Testcontainers.Spire;

public class SpireAgentBuilder : ContainerBuilder<SpireAgentBuilder, SpireAgentContainer, SpireAgentConfiguration>
{
  public const string SpireAgentImage = "ghcr.io/spiffe/spire-agent:1.10.0";

  private IVolume vol;
  
  public SpireAgentBuilder()
    : base(new SpireAgentConfiguration())
  {
    DockerResourceConfiguration = Init().DockerResourceConfiguration;
  }

  public SpireAgentBuilder(SpireAgentConfiguration dockerResourceConfiguration)
    : base(dockerResourceConfiguration)
  {
    DockerResourceConfiguration = dockerResourceConfiguration;
  }

  protected override SpireAgentConfiguration DockerResourceConfiguration { get; }

  public SpireAgentBuilder WithVolume(IVolume volume)
  {
    this.vol = volume;
    return this;
  }

  protected override SpireAgentBuilder Init()
  {
    var volume = new VolumeBuilder()
                      .WithName("spire-agent-" + Guid.NewGuid().ToString("D"))
                      .Build();
    this.vol = volume;
    string conf = "/etc/spire/agent/agent.conf";
    string srv = "/etc/spire/agent/server.crt";
    string crt = "/etc/spire/agent/agent.crt";
    string key = "/etc/spire/agent/agent.key";

    return base.Init()
        .WithImage(SpireAgentImage)
        .WithPortBinding(8080, true)
        .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock")
        .WithVolumeMount(volume, "/tmp/spire/agent/public")
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentConf), conf)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentCert), crt)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentKey), key)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerCert), srv)
        .WithPrivileged(true)
        .WithCreateParameterModifier(parameterModifier =>
        {
            parameterModifier.HostConfig.PidMode = "host";
            parameterModifier.HostConfig.CgroupnsMode = "host";
        })
        .WithCommand(
            "-config", conf,
            "-serverAddress", "spire-server"
        );
  }

  public override SpireAgentContainer Build()
  {
        Validate();

        var waitStrategy = Wait.ForUnixContainer();

        var spireAgentBuilder = DockerResourceConfiguration.WaitStrategies.Count() > 1 ? this : WithWaitStrategy(waitStrategy);
        return new SpireAgentContainer(spireAgentBuilder.DockerResourceConfiguration, this.vol!);
  }

  protected override SpireAgentBuilder Clone(IContainerConfiguration resourceConfiguration)
  {
    return Merge(DockerResourceConfiguration, new SpireAgentConfiguration(resourceConfiguration));
  }

  protected override SpireAgentBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
  {
    return Merge(DockerResourceConfiguration, new SpireAgentConfiguration(resourceConfiguration));
  }

  protected override SpireAgentBuilder Merge(SpireAgentConfiguration oldValue, SpireAgentConfiguration newValue)
  {
    return new SpireAgentBuilder(new SpireAgentConfiguration(oldValue, newValue));
  }
}
