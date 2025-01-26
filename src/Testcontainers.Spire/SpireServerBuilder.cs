using System;
using System.Linq;
using System.Text;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.Spire;

public class SpireServerBuilder : ContainerBuilder<SpireServerBuilder, SpireServerContainer, SpireServerConfiguration>
{
  public const string SpireServerImage = "ghcr.io/spiffe/spire-server:1.10.0";

  public const int SpireServerPort = 8081;

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
    string conf = "/etc/spire/server/server.conf";
    string crt = "/etc/spire/server/server.crt";
    string key = "/etc/spire/server/server.key";
    string agt = "/etc/spire/server/agent.crt";

    return base.Init()
            .WithImage(SpireServerImage)
            .WithPortBinding(SpireServerPort, true)
            .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerConf), conf)
            .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerCert), crt)
            .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerKey), key)
            .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentCert), agt)
            .WithEnvironment("TRUST_DOMAIN", Defaults.TrustDomain)
            .WithEnvironment("CA_BUNDLE_PATH", agt)
            .WithEnvironment("KEY_FILE_PATH", key)
            .WithEnvironment("CERT_FILE_PATH", crt)
            .WithCommand(
              "-config", conf,
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
