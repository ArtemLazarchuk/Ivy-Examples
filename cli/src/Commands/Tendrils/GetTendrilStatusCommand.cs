using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Tendrils;

/// <summary>
/// ivy tendrils status — poll status of a deployed Tendril service.
/// Calls GET /api/v1/tendrils/{projectId}/{serviceId}.
/// </summary>
public sealed class GetTendrilStatusCommand : AsyncCommand<GetTendrilStatusCommand.Settings>
{
    public sealed class Settings : TendrilApiSettings
    {
        [CommandOption("--sliplane-token <TOKEN>")]
        [Description("Your Sliplane API token (or set SLIPLANE_API_KEY env var)")]
        public string? SliplaneToken { get; init; }

        [CommandOption("--project-id <ID>")]
        [Description("Sliplane project ID of the Tendril service")]
        public required string ProjectId { get; init; }

        [CommandOption("--service-id <ID>")]
        [Description("Sliplane service ID returned by 'ivy tendrils deploy'")]
        public required string ServiceId { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // Status endpoint requires X-Sliplane-Token header — pass it via query or we add it to client
        // The tendril-deploy API reads it from the X-Sliplane-Token request header.
        // We create a raw HTTP request to pass the extra header.
        var sliplaneToken = settings.SliplaneToken
            ?? Environment.GetEnvironmentVariable("SLIPLANE_API_KEY")
            ?? throw new InvalidOperationException(
                "Sliplane token required. Use --sliplane-token or set SLIPLANE_API_KEY.");

        var tendrilApiKey = settings.TendrilApiKey
            ?? Environment.GetEnvironmentVariable("TENDRIL_API_KEY");

        var baseUrl = settings.TendrilUrl
            ?? Environment.GetEnvironmentVariable("TENDRIL_BASE_URL")
            ?? throw new InvalidOperationException(
                "Tendril base URL required. Use --tendril-url or set TENDRIL_BASE_URL.");

        using var http = new System.Net.Http.HttpClient();
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrEmpty(tendrilApiKey))
            http.DefaultRequestHeaders.Add("X-Api-Key", tendrilApiKey);
        http.DefaultRequestHeaders.Add("X-Sliplane-Token", sliplaneToken);

        var response = await http.GetAsync(
            $"api/v1/tendrils/{settings.ProjectId}/{settings.ServiceId}");

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new System.Net.Http.HttpRequestException($"HTTP {(int)response.StatusCode}: {err}");
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var doc = await System.Text.Json.JsonDocument.ParseAsync(stream);
        YamlOutput.Write(doc);
        return 0;
    }
}
