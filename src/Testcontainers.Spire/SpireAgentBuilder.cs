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

  public const int SpireAgentPort = 8080;

  public const string ConfigPath = "/etc/spire/agent/agent.conf";

  public const string SocketDir = "/tmp/spire/agent/public";

  public const string SocketPath = $"{SocketDir}/api.sock";
  
  public SpireAgentBuilder()
    : this(new SpireAgentConfiguration())
  {
    DockerResourceConfiguration = Init().DockerResourceConfiguration;
  }

  public SpireAgentBuilder(SpireAgentConfiguration dockerResourceConfiguration)
    : base(dockerResourceConfiguration)
  {
    DockerResourceConfiguration = dockerResourceConfiguration;
  }

  protected override SpireAgentConfiguration DockerResourceConfiguration { get; }

  protected override SpireAgentBuilder Init()
  {
    string srv = "/etc/spire/agent/server.crt";
    string crt = "/etc/spire/agent/agent.crt";
    string key = "/etc/spire/agent/agent.key";

    return base.Init()
        .WithImage(SpireAgentImage)
        .WithPortBinding(SpireAgentPort, true)
        .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock")
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentConf), ConfigPath)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentCert), crt)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentKey), key)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerCert), srv)
        .WithPrivileged(true)
        .WithCreateParameterModifier(parameterModifier =>
        {
            parameterModifier.HostConfig.PidMode = "host";
            parameterModifier.HostConfig.CgroupnsMode = "host";
        })
        .WithEnvironment("SERVER_ADDRESS", Defaults.ServerAddress)
        .WithEnvironment("TRUST_DOMAIN", Defaults.TrustDomain)
        .WithEnvironment("TRUST_BUNDLE_PATH", srv)
        .WithEnvironment("PRIVATE_KEY_PATH", key)
        .WithEnvironment("CERTIFICATE_PATH", crt)
        .WithCommand(
            "-config", ConfigPath,
            "-expandEnv", "true"
        );
  }

  public SpireAgentBuilder WithTrustDomain(string trustDomain)
  {
    _ = trustDomain ?? throw new ArgumentNullException(nameof(trustDomain));

    SpireAgentConfiguration oldConfig = DockerResourceConfiguration;
    SpireAgentConfiguration newConfig = new(trustDomain, oldConfig.ServerAddress);

    return Merge(oldConfig, newConfig).WithEnvironment("TRUST_DOMAIN", trustDomain);
  }

  public SpireAgentBuilder WithServerAddress(string serverAddress)
  {
    _ = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));

    SpireAgentConfiguration oldConfig = DockerResourceConfiguration;
    SpireAgentConfiguration newConfig = new(oldConfig.TrustDomain, serverAddress);

    return Merge(oldConfig, newConfig).WithEnvironment("SERVER_ADDRESS", serverAddress);
  }

  public SpireAgentBuilder WithAgentVolume(IVolume volume)
  {
    return WithVolumeMount(volume, SocketDir);
  }

  public override SpireAgentContainer Build()
  {
        Validate();

        var waitStrategy = Wait.ForUnixContainer();

        var spireAgentBuilder = DockerResourceConfiguration.WaitStrategies.Count() > 1 ? this : WithWaitStrategy(waitStrategy);
        return new SpireAgentContainer(spireAgentBuilder.DockerResourceConfiguration);
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
