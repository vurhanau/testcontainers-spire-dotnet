using System;
using System.Linq;
using System.Text;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.Spire;

public class SpireServerBuilder : ContainerBuilder<SpireServerBuilder, SpireServerContainer, SpireServerConfiguration>
{
  public SpireServerBuilder()
      : this(new SpireServerConfiguration())
  {
    DockerResourceConfiguration = Init().DockerResourceConfiguration;
  }

  private SpireServerBuilder(SpireServerConfiguration resourceConfiguration)
      : base(resourceConfiguration)
  {
    DockerResourceConfiguration = resourceConfiguration;
  }

  protected override SpireServerConfiguration DockerResourceConfiguration { get; }

  protected override SpireServerBuilder Init()
  {
    return base.Init()
            .WithImage(Defaults.ServerImage)
            .WithPortBinding(Defaults.ServerPort, true)
            .WithNetworkAliases(Defaults.ServerAddress)
            .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerConfig), Defaults.ServerConfigPath)
            .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerCert), Defaults.ServerCertPath)
            .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerKey), Defaults.ServerKeyPath)
            .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentCert), Defaults.ServerAgentCertPath)
            .WithEnvironment("TRUST_DOMAIN", Defaults.TrustDomain)
            .WithEnvironment("CA_BUNDLE_PATH", Defaults.ServerAgentCertPath)
            .WithEnvironment("KEY_FILE_PATH", Defaults.ServerKeyPath)
            .WithEnvironment("CERT_FILE_PATH", Defaults.ServerCertPath)
            .WithCommand(
              "-config", Defaults.ServerConfigPath,
              "-expandEnv", "true"
            );
  }

  public SpireServerBuilder WithTrustDomain(string trustDomain)
  {
    _ = trustDomain ?? throw new ArgumentNullException(nameof(trustDomain));

    SpireServerConfiguration oldConfig = DockerResourceConfiguration;
    SpireServerConfiguration newConfig = new(trustDomain);

    return Merge(oldConfig, newConfig).WithEnvironment("TRUST_DOMAIN", trustDomain);
  }

  public override SpireServerContainer Build()
  {
    Validate();

    var waitStrategy = Wait.ForUnixContainer();

    var spireServerBuilder = DockerResourceConfiguration.WaitStrategies.Count() > 1 ? this : WithWaitStrategy(waitStrategy);
    return new SpireServerContainer(spireServerBuilder.DockerResourceConfiguration);
  }

  /// <inheritdoc />
  protected override SpireServerBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
  {
    return Merge(DockerResourceConfiguration, new SpireServerConfiguration(resourceConfiguration));
  }

  /// <inheritdoc />
  protected override SpireServerBuilder Clone(IContainerConfiguration resourceConfiguration)
  {
    return Merge(DockerResourceConfiguration, new SpireServerConfiguration(resourceConfiguration));
  }

  /// <inheritdoc />
  protected override SpireServerBuilder Merge(SpireServerConfiguration oldValue, SpireServerConfiguration newValue)
  {
    return new SpireServerBuilder(new SpireServerConfiguration(oldValue, newValue));
  }
}
