using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Testcontainers.Spire;

public class SpireServerContainer : DockerContainer
{
  public SpireServerContainer(IContainerConfiguration configuration) : base(configuration)
  {
  }

  public string GetConnectionString()
  {
    return $"spire-server:{this.GetMappedPublicPort(8081)}";
  }
}
