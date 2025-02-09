using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Spiffe.Testcontainers.Spire.Agent;

public class SpireAgentContainer : DockerContainer
{
  public SpireAgentContainer(IContainerConfiguration configuration) : base(configuration)
  {
  }
}
