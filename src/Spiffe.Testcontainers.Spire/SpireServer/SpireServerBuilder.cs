using System;
using System.Linq;
using System.Text;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Spiffe.Testcontainers.Spire.Server;

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
    ServerOptions options = DockerResourceConfiguration.Options;
    return base.Init()
               .WithImage(Defaults.ServerImage)
               .WithNetworkAliases(Defaults.ServerAddress)
               .Apply(options);
  }

  public SpireServerBuilder WithOptions(ServerOptions options)
  {
    _ = options ?? throw new ArgumentNullException(nameof(options));

    SpireServerConfiguration oldConfig = DockerResourceConfiguration;
    SpireServerConfiguration newConfig = new(options);

    return Merge(oldConfig, newConfig).Apply(options);
  }

  public override SpireServerContainer Build()
  {
    Validate();

    var waitStrategy = Wait.ForUnixContainer();

    var spireServerBuilder = DockerResourceConfiguration.WaitStrategies.Count() > 1 ? this : WithWaitStrategy(waitStrategy);
    return new SpireServerContainer(spireServerBuilder.DockerResourceConfiguration);
  }

  protected override SpireServerBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
  {
    return Merge(DockerResourceConfiguration, new SpireServerConfiguration(resourceConfiguration));
  }

  protected override SpireServerBuilder Clone(IContainerConfiguration resourceConfiguration)
  {
    return Merge(DockerResourceConfiguration, new SpireServerConfiguration(resourceConfiguration));
  }

  protected override SpireServerBuilder Merge(SpireServerConfiguration oldValue, SpireServerConfiguration newValue)
  {
    return new SpireServerBuilder(new SpireServerConfiguration(oldValue, newValue));
  }

  private SpireServerBuilder Apply(ServerOptions options)
  {
    ServerConf c = options.Conf;
    string conf = c.Render();
    return WithPortBinding(c.Port, true)
          .WithResourceMapping(Encoding.UTF8.GetBytes(conf), options.ConfPath)
          .WithResourceMapping(Encoding.UTF8.GetBytes(options.Cert), c.CertFilePath)
          .WithResourceMapping(Encoding.UTF8.GetBytes(options.Key), c.KeyFilePath)
          .WithResourceMapping(Encoding.UTF8.GetBytes(options.CaBundle), c.CaBundlePath)
          .WithCommand(
            "-config", options.ConfPath,
            "-expandEnv", "true"
          );
  }
}
