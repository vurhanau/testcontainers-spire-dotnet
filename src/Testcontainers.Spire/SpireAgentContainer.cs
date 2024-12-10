using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Volumes;

namespace Testcontainers.Spire;

public class SpireAgentContainer : DockerContainer
{
  private IVolume vol;

  public SpireAgentContainer(IContainerConfiguration configuration, IVolume vol) : base(configuration)
  {
    this.vol = vol;
  }

  public IVolume GetAgentVolume()
  {
    return this.vol;
  }
}
