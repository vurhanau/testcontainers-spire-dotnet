using System;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;

namespace Spiffe.Testcontainers.Spire.Server;

public sealed class SpireServerConfiguration : ContainerConfiguration
{
  public SpireServerConfiguration()
  {
  }

  public SpireServerConfiguration(ServerOptions options)
  {
    _ = options ?? throw new ArgumentNullException(nameof(options));
    Options = new ServerOptions(options);
  }

  public SpireServerConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    : base(resourceConfiguration)
  {
    // Passes the configuration upwards to the base implementations to create an updated immutable copy.
  }

  public SpireServerConfiguration(IContainerConfiguration resourceConfiguration)
    : base(resourceConfiguration)
  {
    // Passes the configuration upwards to the base implementations to create an updated immutable copy.
  }

  public SpireServerConfiguration(SpireServerConfiguration resourceConfiguration)
    : this(new SpireServerConfiguration(), resourceConfiguration)
  {
    // Passes the configuration upwards to the base implementations to create an updated immutable copy.
  }

  public SpireServerConfiguration(SpireServerConfiguration oldValue, SpireServerConfiguration newValue)
    : base(oldValue, newValue)
  {
  }

  public ServerOptions Options { get; } = new();
}
