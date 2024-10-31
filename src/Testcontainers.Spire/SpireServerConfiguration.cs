using System;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.Spire;

public sealed class SpireServerConfiguration : ContainerConfiguration
{
    public string TrustDomain { get; } = Defaults.TrustDomain;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireServerConfiguration" /> class.
    /// </summary>
    public SpireServerConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireServerConfiguration" /> class.
    /// </summary>
    public SpireServerConfiguration(string trustDomain)
    {
        TrustDomain = trustDomain ?? throw new ArgumentNullException(nameof(trustDomain));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireServerConfiguration" /> class.
    /// </summary>
    /// <param name="resourceConfiguration">The Docker resource configuration.</param>
    public SpireServerConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireServerConfiguration" /> class.
    /// </summary>
    /// <param name="resourceConfiguration">The Docker resource configuration.</param>
    public SpireServerConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireServerConfiguration" /> class.
    /// </summary>
    /// <param name="resourceConfiguration">The Docker resource configuration.</param>
    public SpireServerConfiguration(SpireServerConfiguration resourceConfiguration)
        : this(new SpireServerConfiguration(), resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpireServerConfiguration" /> class.
    /// </summary>
    /// <param name="oldValue">The old Docker resource configuration.</param>
    /// <param name="newValue">The new Docker resource configuration.</param>
    public SpireServerConfiguration(SpireServerConfiguration oldValue, SpireServerConfiguration newValue)
        : base(oldValue, newValue)
    {
    }
}
