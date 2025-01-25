using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Testcontainers.Spire;

public class SpireServerContainer : DockerContainer
{
  private readonly SpireServerConfiguration _configuration;

  public SpireServerContainer(SpireServerConfiguration configuration)
      : base(configuration)
  {
      _configuration = configuration;
  }

  public SpireServerConfiguration GetConfiguration()
  {
      return _configuration;
  }
}
