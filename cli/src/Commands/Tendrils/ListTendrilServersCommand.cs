using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Tendrils;

/// <summary>ivy tendrils servers — list Sliplane servers available for Tendril deployment.</summary>
public sealed class ListTendrilServersCommand : AsyncCommand<TendrilApiSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TendrilApiSettings settings)
    {
        var client = settings.CreateTendrilClient();
        var result = await client.GetAsync("api/v1/servers");
        YamlOutput.Write(result);
        return 0;
    }
}
