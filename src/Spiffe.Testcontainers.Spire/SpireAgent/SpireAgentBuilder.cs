using System;
using System.Linq;
using System.Text;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Volumes;

namespace Spiffe.Testcontainers.Spire.Agent;

public class SpireAgentBuilder : ContainerBuilder<SpireAgentBuilder, SpireAgentContainer, SpireAgentConfiguration>
{ 
  public const string Image = "ghcr.io/spiffe/spire-agent:1.10.0";

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
    AgentOptions options = DockerResourceConfiguration.Options;

    return base.Init()
               .WithImage(Image)
               .WithPrivileged(true)
               .WithCreateParameterModifier(parameterModifier =>
               {
                   parameterModifier.HostConfig.PidMode = "host";
                   parameterModifier.HostConfig.CgroupnsMode = "host";
               })
               .Apply(options);
  }

  public SpireAgentBuilder WithOptions(AgentOptions options)
  {
    _ = options ?? throw new ArgumentNullException(nameof(options));

    SpireAgentConfiguration oldConfig = DockerResourceConfiguration;
    SpireAgentConfiguration newConfig = new(options);

    return Merge(oldConfig, newConfig).Apply(options);
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

  private SpireAgentBuilder Apply(AgentOptions options)
  {
    AgentConf c = options.Conf;
    string conf = c.Render();
    return WithResourceMapping(Encoding.UTF8.GetBytes(conf), options.ConfPath)
          .WithResourceMapping(Encoding.UTF8.GetBytes(options.Cert), c.CertFilePath)
          .WithResourceMapping(Encoding.UTF8.GetBytes(options.Key), c.KeyFilePath)
          .WithResourceMapping(Encoding.UTF8.GetBytes(options.TrustBundleCert), c.TrustBundlePath)
          .WithCommand(
              "-config", options.ConfPath,
              "-expandEnv", "true"
          );
  }
}
