using System.Linq;
using System.Text;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.Spire;

public class SpireServerBuilder : ContainerBuilder<SpireServerBuilder, SpireServerContainer, SpireServerConfiguration>
{
  public const string SpireServerImage = "ghcr.io/spiffe/spire-server:1.10.0";
  
  public SpireServerBuilder()
    : base(new SpireServerConfiguration())
  {
    DockerResourceConfiguration = Init().DockerResourceConfiguration;
  }

  public SpireServerBuilder(SpireServerConfiguration dockerResourceConfiguration)
    : base(dockerResourceConfiguration)
  {
    DockerResourceConfiguration = dockerResourceConfiguration;
  }

  protected override SpireServerConfiguration DockerResourceConfiguration { get; }

  protected override SpireServerBuilder Init()
  {
    string conf = "/etc/spire/server/server.conf";
    string crt = "/etc/spire/server/server.crt";
    string key = "/etc/spire/server/server.key";
    string agt = "/etc/spire/server/agent.key";

    return base.Init()
        .WithImage(SpireServerImage)
        .WithPortBinding(8081, true)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerConf), conf)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerCert), crt)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.ServerKey), key)
        .WithResourceMapping(Encoding.UTF8.GetBytes(Defaults.AgentCert), agt)
        .WithNetworkAliases("spire-server");
  }

  public override SpireServerContainer Build()
  {
        Validate();

        var waitStrategy = Wait.ForUnixContainer();

        var spireServerBuilder = DockerResourceConfiguration.WaitStrategies.Count() > 1 ? this : WithWaitStrategy(waitStrategy);
        return new SpireServerContainer(spireServerBuilder.DockerResourceConfiguration);
  }

  protected override SpireServerBuilder Clone(IContainerConfiguration resourceConfiguration)
  {
    return Merge(DockerResourceConfiguration, new SpireServerConfiguration(resourceConfiguration));
  }

  protected override SpireServerBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
  {
    return Merge(DockerResourceConfiguration, new SpireServerConfiguration(resourceConfiguration));
  }

  protected override SpireServerBuilder Merge(SpireServerConfiguration oldValue, SpireServerConfiguration newValue)
  {
    return new SpireServerBuilder(new SpireServerConfiguration(oldValue, newValue));
  }
}
