using System;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;

namespace Spiffe.Testcontainers.Spire.Agent;

public sealed class SpireAgentConfiguration : ContainerConfiguration
{
    public AgentOptions Options { get; } = new();

    public SpireAgentConfiguration()
    {
    }

    public SpireAgentConfiguration(AgentOptions options)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));
        Options = new(options);
    }

    public SpireAgentConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    public SpireAgentConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    public SpireAgentConfiguration(SpireAgentConfiguration resourceConfiguration)
        : this(new SpireAgentConfiguration(), resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    public SpireAgentConfiguration(SpireAgentConfiguration oldValue, SpireAgentConfiguration newValue)
        : base(oldValue, newValue)
    {
    }
}
