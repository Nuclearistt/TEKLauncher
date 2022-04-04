namespace TEKLauncher.Servers;

/// <summary>Represents server/cluster information JSON object.</summary>
record Info
{
    /// <summary>Nickname of the server/cluster owner.</summary>
    public string? HosterName { get; init; }
    /// <summary>Custom name of the server.</summary>
    public string? ServerName { get; init; }
    /// <summary>Name of the cluster.</summary>
    public string? ClusterName { get; init; }
    /// <summary>URL of the cluster icon.</summary>
    public string? IconUrl { get; init; }
    /// <summary>Server/cluster's Discord server invite link.</summary>
    public string? Discord { get; init; }
    /// <summary>Server description.</summary>
    public Description? ServerDescription { get; init; }
    /// <summary>Cluster description.</summary>
    public Description? ClusterDescription { get; init; }
}