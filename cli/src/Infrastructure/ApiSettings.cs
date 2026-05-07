using System.ComponentModel;
using Spectre.Console.Cli;

namespace Ivy.Cli.Infrastructure;

/// <summary>
/// Base settings for all Sliplane commands.
/// API key is read from --api-key flag or SLIPLANE_API_KEY env var.
/// </summary>
public class ApiSettings : CommandSettings
{
    [CommandOption("--api-key <KEY>")]
    [Description("Sliplane API key (or set SLIPLANE_API_KEY env var)")]
    public string? ApiKey { get; init; }

    [CommandOption("--org-id <ORG_ID>")]
    [Description("Organization ID for legacy tokens (or set SLIPLANE_ORG_ID env var)")]
    public string? OrgId { get; init; }

    public SliplaneClient CreateClient()
    {
        var key = ApiKey ?? Environment.GetEnvironmentVariable("SLIPLANE_API_KEY")
            ?? throw new InvalidOperationException(
                "Sliplane API key required. Use --api-key or set SLIPLANE_API_KEY.");
        var org = OrgId ?? Environment.GetEnvironmentVariable("SLIPLANE_ORG_ID");
        return new SliplaneClient(key, org);
    }
}

/// <summary>
/// Base settings for all Tendril commands.
/// Tendril base URL: --tendril-url or TENDRIL_BASE_URL env var.
/// Tendril API key: --tendril-api-key or TENDRIL_API_KEY env var (optional).
/// </summary>
public class TendrilApiSettings : CommandSettings
{
    [CommandOption("--tendril-url <URL>")]
    [Description("Base URL of the tendril-deploy instance (or set TENDRIL_BASE_URL env var)")]
    public string? TendrilUrl { get; init; }

    [CommandOption("--tendril-api-key <KEY>")]
    [Description("API key for tendril-deploy (or set TENDRIL_API_KEY env var). Optional if server has no key configured.")]
    public string? TendrilApiKey { get; init; }

    public TendrilClient CreateTendrilClient()
    {
        var url = TendrilUrl ?? Environment.GetEnvironmentVariable("TENDRIL_BASE_URL")
            ?? throw new InvalidOperationException(
                "Tendril base URL required. Use --tendril-url or set TENDRIL_BASE_URL.");
        var key = TendrilApiKey ?? Environment.GetEnvironmentVariable("TENDRIL_API_KEY");
        return new TendrilClient(url, key);
    }
}
