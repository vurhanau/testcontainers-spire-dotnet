using System;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;

namespace Spiffe.Testcontainers.Spire;


public sealed class SpireAgentConfiguration : ContainerConfiguration
{
    public string TrustDomain { get; } = Defaults.TrustDomain;

    public string ServerAddress { get; } = Defaults.ServerAddress;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireAgentConfiguration" /> class.
    /// </summary>
    public SpireAgentConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireAgentConfiguration" /> class.
    /// </summary>
    public SpireAgentConfiguration(string trustDomain, string serverAddress)
    {
        TrustDomain = trustDomain ?? throw new ArgumentNullException(nameof(trustDomain));
        ServerAddress = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireAgentConfiguration" /> class.
    /// </summary>
    /// <param name="resourceConfiguration">The Docker resource configuration.</param>
    public SpireAgentConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireAgentConfiguration" /> class.
    /// </summary>
    /// <param name="resourceConfiguration">The Docker resource configuration.</param>
    public SpireAgentConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireAgentConfiguration" /> class.
    /// </summary>
    /// <param name="resourceConfiguration">The Docker resource configuration.</param>
    public SpireAgentConfiguration(SpireAgentConfiguration resourceConfiguration)
        : this(new SpireAgentConfiguration(), resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireAgentConfiguration" /> class.
    /// </summary>
    /// <param name="oldValue">The old Docker resource configuration.</param>
    /// <param name="newValue">The new Docker resource configuration.</param>
    public SpireAgentConfiguration(SpireAgentConfiguration oldValue, SpireAgentConfiguration newValue)
        : base(oldValue, newValue)
    {
    }
}
