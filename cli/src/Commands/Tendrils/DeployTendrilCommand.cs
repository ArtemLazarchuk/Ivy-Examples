using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Tendrils;

/// <summary>
/// ivy tendrils deploy — create and deploy a new Tendril instance on Sliplane.
/// Calls POST /api/v1/tendrils on the tendril-deploy service.
/// </summary>
public sealed class DeployTendrilCommand : AsyncCommand<DeployTendrilCommand.Settings>
{
    public sealed class Settings : TendrilApiSettings
    {
        // ── Required Sliplane targeting ────────────────────────────────────

        [CommandOption("--sliplane-token <TOKEN>")]
        [Description("Your Sliplane API token (or set SLIPLANE_API_KEY env var)")]
        public string? SliplaneToken { get; init; }

        [CommandOption("--project-id <ID>")]
        [Description("Sliplane project ID where the Tendril service will be created")]
        public required string ProjectId { get; init; }

        [CommandOption("--server-id <ID>")]
        [Description("Sliplane server ID where the service will run")]
        public required string ServerId { get; init; }

        [CommandOption("--name <NAME>")]
        [Description("Name for the new Sliplane service, e.g. tendril-artem")]
        public required string ServiceName { get; init; }

        // ── Tendril login credentials ──────────────────────────────────────

        [CommandOption("--username <USERNAME>")]
        [Description("Username for logging into the deployed Tendril web UI")]
        public required string BasicAuthUsername { get; init; }

        [CommandOption("--password <PASSWORD>")]
        [Description("Password for the Tendril web UI (minimum 8 characters)")]
        public required string BasicAuthPassword { get; init; }

        // ── Agent API keys (all optional) ──────────────────────────────────

        [CommandOption("--anthropic-key <KEY>")]
        [Description("Anthropic API key → ANTHROPIC_API_KEY in the container")]
        public string? AnthropicApiKey { get; init; }

        [CommandOption("--claude-token <TOKEN>")]
        [Description("Claude OAuth token (claude setup-token) → CLAUDE_CODE_OAUTH_TOKEN")]
        public string? ClaudeCodeOAuthToken { get; init; }

        [CommandOption("--github-token <TOKEN>")]
        [Description("GitHub personal access token → GITHUB_TOKEN")]
        public string? GitHubToken { get; init; }

        [CommandOption("--openai-key <KEY>")]
        [Description("OpenAI API key → OPENAI_API_KEY")]
        public string? OpenAiApiKey { get; init; }

        [CommandOption("--gemini-key <KEY>")]
        [Description("Google Gemini API key → GEMINI_API_KEY")]
        public string? GeminiApiKey { get; init; }

        // ── Workspace repos ────────────────────────────────────────────────

        [CommandOption("--repo <URL>")]
        [Description("GitHub repo to clone into the container on startup (repeatable)")]
        public string[]? Repos { get; init; }

        // ── Optional overrides ─────────────────────────────────────────────

        [CommandOption("--volume-id <ID>")]
        [Description("Sliplane persistent volume ID to attach (recommended to persist data)")]
        public string? VolumeId { get; init; }

        [CommandOption("--git-repo <URL>")]
        [Description("Source repo for the Tendril Dockerfile (defaults to official Ivy-Tendril repo)")]
        public string? GitRepo { get; init; }

        [CommandOption("--branch <BRANCH>")]
        [Description("Branch to build from (default: development)")]
        public string? Branch { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateTendrilClient();

        // Sliplane token: explicit flag → env var
        var sliplaneToken = settings.SliplaneToken
            ?? Environment.GetEnvironmentVariable("SLIPLANE_API_KEY")
            ?? throw new InvalidOperationException(
                "Sliplane token required. Use --sliplane-token or set SLIPLANE_API_KEY.");

        var body = new Dictionary<string, object?>
        {
            ["sliplaneApiToken"]  = sliplaneToken,
            ["projectId"]         = settings.ProjectId,
            ["serverId"]          = settings.ServerId,
            ["serviceName"]       = settings.ServiceName,
            ["basicAuthUsername"] = settings.BasicAuthUsername,
            ["basicAuthPassword"] = settings.BasicAuthPassword,
        };

        if (!string.IsNullOrEmpty(settings.AnthropicApiKey))      body["anthropicApiKey"]      = settings.AnthropicApiKey;
        if (!string.IsNullOrEmpty(settings.ClaudeCodeOAuthToken))  body["claudeCodeOAuthToken"] = settings.ClaudeCodeOAuthToken;
        if (!string.IsNullOrEmpty(settings.GitHubToken))           body["gitHubToken"]          = settings.GitHubToken;
        if (!string.IsNullOrEmpty(settings.OpenAiApiKey))          body["openAiApiKey"]         = settings.OpenAiApiKey;
        if (!string.IsNullOrEmpty(settings.GeminiApiKey))          body["geminiApiKey"]         = settings.GeminiApiKey;
        if (settings.Repos is { Length: > 0 })                     body["repos"]                = settings.Repos;
        if (!string.IsNullOrEmpty(settings.VolumeId))              body["volumeId"]             = settings.VolumeId;
        if (!string.IsNullOrEmpty(settings.GitRepo))               body["gitRepo"]              = settings.GitRepo;
        if (!string.IsNullOrEmpty(settings.Branch))                body["branch"]               = settings.Branch;

        AnsiConsole.MarkupLine("[yellow]Deploying Tendril...[/]");
        var result = await client.PostAsync("api/v1/tendrils", body);
        YamlOutput.Write(result);
        AnsiConsole.MarkupLine("[green]Tendril deployment accepted. Use 'ivy tendrils status' to check progress.[/]");
        return 0;
    }
}
