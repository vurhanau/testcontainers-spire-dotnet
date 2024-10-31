using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Testcontainers.Spire;

public class SpireAgentContainer : DockerContainer
{
  public SpireAgentContainer(IContainerConfiguration configuration) : base(configuration)
  {
  }
}
