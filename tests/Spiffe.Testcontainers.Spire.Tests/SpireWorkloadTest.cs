using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;

namespace Spiffe.Testcontainers.Spire.Tests;

public class SpireWorkloadTest
{
  [Fact]
  public async Task StartWorkloadTest()
  {
    var d = "tests/Spiffe.Testcontainers.Spire.Tests.Workload";
    var futureImage = new ImageFromDockerfileBuilder()
      .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), d)
      .WithDockerfile("Dockerfile")
      .Build();
    await futureImage.CreateAsync();

    var c = new ContainerBuilder()
      .WithImage(futureImage)
      .Build();

    await c.StartAsync();
  }
}
