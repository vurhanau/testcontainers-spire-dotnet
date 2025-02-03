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
    return base.Init()
        .WithImage(Defaults.AgentImage)
        .WithPortBinding(Defaults.AgentPort, true)
        .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock")
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentConfig), Defaults.AgentConfigPath)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentCert), Defaults.AgentCertPath)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentKey), Defaults.AgentKeyPath)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerCert), Defaults.AgentServerCertPath)
        .WithPrivileged(true)
        .WithCreateParameterModifier(parameterModifier =>
        {
            parameterModifier.HostConfig.PidMode = "host";
            parameterModifier.HostConfig.CgroupnsMode = "host";
        })
        .WithEnvironment("SERVER_ADDRESS", Defaults.ServerAddress)
        .WithEnvironment("TRUST_DOMAIN", Defaults.TrustDomain)
        .WithEnvironment("TRUST_BUNDLE_PATH", Defaults.AgentServerCertPath)
        .WithEnvironment("PRIVATE_KEY_PATH", Defaults.AgentKeyPath)
        .WithEnvironment("CERTIFICATE_PATH", Defaults.AgentCertPath)
        .WithCommand(
            "-config", Defaults.AgentConfigPath,
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
    return WithVolumeMount(volume, Defaults.AgentSocketDir);
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
