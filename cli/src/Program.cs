using Ivy.Cli.Commands.Config;

using Ivy.Cli.Commands.Sliplane;
using Ivy.Cli.Commands.Sliplane.Credentials;
using Ivy.Cli.Commands.Sliplane.OAuth;
using Ivy.Cli.Commands.Sliplane.Projects;
using Ivy.Cli.Commands.Sliplane.Servers;
using Ivy.Cli.Commands.Sliplane.Services;
using Ivy.Cli.Commands.Tendrils;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("ivy");
    config.SetApplicationVersion("0.1.0");

    // ── Config ────────────────────────────────────────────────────────
    config.AddBranch("config", cfg =>
    {
        cfg.SetDescription("Manage saved CLI configuration (~/.ivy/config.json)");
        cfg.AddCommand<ConfigListCommand>("list")
            .WithDescription("Show all saved config values");
        cfg.AddCommand<ConfigGetCommand>("get")
            .WithDescription("Get a saved config value");
        cfg.AddCommand<ConfigSetCommand>("set")
            .WithDescription("Save a config value (e.g. ivy config set sliplane_api_key sk-xxx)");
        cfg.AddCommand<ConfigUnsetCommand>("unset")
            .WithDescription("Remove a single config value (e.g. ivy config unset sliplane_api_key)");
        cfg.AddCommand<ConfigClearCommand>("clear")
            .WithDescription("Delete all saved config from ~/.ivy/config.json");
    });

    // ── Sliplane: identity ─────────────────────────────────────────────
    config.AddCommand<MeCommand>("me")
        .WithDescription("Get current Sliplane identity and token context");

    // ── Sliplane: projects ─────────────────────────────────────────────
    config.AddBranch("projects", branch =>
    {
        branch.SetDescription("Manage Sliplane projects");
        branch.AddCommand<ListProjectsCommand>("list")
            .WithDescription("List all projects");
        branch.AddCommand<CreateProjectCommand>("create")
            .WithDescription("Create a new project");
        branch.AddCommand<UpdateProjectCommand>("update")
            .WithDescription("Update a project name");
        branch.AddCommand<DeleteProjectCommand>("delete")
            .WithDescription("Delete a project");
    });

    // ── Sliplane: servers ──────────────────────────────────────────────
    config.AddBranch("servers", branch =>
    {
        branch.SetDescription("Manage Sliplane servers");
        branch.AddCommand<ListServersCommand>("list")
            .WithDescription("List all servers");
        branch.AddCommand<GetServerCommand>("get")
            .WithDescription("Get server details");
        branch.AddCommand<CreateServerCommand>("create")
            .WithDescription("Create a new server");
        branch.AddCommand<DeleteServerCommand>("delete")
            .WithDescription("Delete a server");
        branch.AddCommand<RescaleServerCommand>("rescale")
            .WithDescription("Rescale a server (scale up only)");
        branch.AddCommand<ServerMetricsCommand>("metrics")
            .WithDescription("Get server metrics");
        branch.AddCommand<ListServerVolumesCommand>("volumes")
            .WithDescription("List server volumes");
        branch.AddCommand<CreateServerVolumeCommand>("create-volume")
            .WithDescription("Create a volume on a server");
    });

    // ── Sliplane: services ─────────────────────────────────────────────
    config.AddBranch("services", branch =>
    {
        branch.SetDescription("Manage Sliplane services");
        branch.AddCommand<ListServicesCommand>("list")
            .WithDescription("List services in a project (or all projects)");
        branch.AddCommand<GetServiceCommand>("get")
            .WithDescription("Get service details");
        branch.AddCommand<CreateServiceCommand>("create")
            .WithDescription("Create a new service");
        branch.AddCommand<UpdateServiceCommand>("update")
            .WithDescription("Update a service");
        branch.AddCommand<DeleteServiceCommand>("delete")
            .WithDescription("Delete a service");
        branch.AddCommand<PauseServiceCommand>("pause")
            .WithDescription("Pause a service");
        branch.AddCommand<UnpauseServiceCommand>("unpause")
            .WithDescription("Unpause a service");
        branch.AddCommand<DeployServiceCommand>("deploy")
            .WithDescription("Trigger a deployment");
        branch.AddCommand<ServiceLogsCommand>("logs")
            .WithDescription("Get service logs");
        branch.AddCommand<ServiceMetricsCommand>("metrics")
            .WithDescription("Get service metrics");
        branch.AddCommand<ServiceEventsCommand>("events")
            .WithDescription("Get service events");
        branch.AddCommand<AddDomainCommand>("add-domain")
            .WithDescription("Add a custom domain");
        branch.AddCommand<RemoveDomainCommand>("remove-domain")
            .WithDescription("Remove a custom domain");
    });

    // ── Sliplane: registry credentials ────────────────────────────────
    config.AddBranch("credentials", branch =>
    {
        branch.SetDescription("Manage Sliplane registry credentials");
        branch.AddCommand<ListCredentialsCommand>("list")
            .WithDescription("List all registry credentials");
        branch.AddCommand<GetCredentialsCommand>("get")
            .WithDescription("Get registry credentials details");
        branch.AddCommand<CreateCredentialsCommand>("create")
            .WithDescription("Create registry credentials");
        branch.AddCommand<UpdateCredentialsCommand>("update")
            .WithDescription("Update registry credentials name");
        branch.AddCommand<DeleteCredentialsCommand>("delete")
            .WithDescription("Delete registry credentials");
    });

    // ── Sliplane: OAuth ────────────────────────────────────────────────
    config.AddBranch("oauth", branch =>
    {
        branch.SetDescription("Manage Sliplane OAuth clients");
        branch.AddCommand<ListOAuthClientsCommand>("list")
            .WithDescription("List OAuth clients");
        branch.AddCommand<GetOAuthClientCommand>("get")
            .WithDescription("Get OAuth client details");
        branch.AddCommand<UpdateOAuthClientCommand>("update")
            .WithDescription("Update OAuth client metadata");
        branch.AddCommand<ListOAuthClientUsersCommand>("users")
            .WithDescription("List OAuth client authorized users");
    });

    // ── Tendrils ───────────────────────────────────────────────────────
    // Requires: TENDRIL_BASE_URL (+ TENDRIL_API_KEY if server has key configured)
    //           SLIPLANE_API_KEY (for status/servers/projects that forward to Sliplane)
    config.AddBranch("tendrils", branch =>
    {
        branch.SetDescription("Deploy and manage Tendril instances via tendril-deploy API");
        branch.AddCommand<DeployTendrilCommand>("deploy")
            .WithDescription("Deploy a new Tendril instance on Sliplane");
        branch.AddCommand<GetTendrilStatusCommand>("status")
            .WithDescription("Get status of a deployed Tendril service");
        branch.AddCommand<ListTendrilServersCommand>("servers")
            .WithDescription("List Sliplane servers available for Tendril deployment");
        branch.AddCommand<ListTendrilProjectsCommand>("projects")
            .WithDescription("List Sliplane projects available for Tendril deployment");
    });
});

return app.Run(args);
