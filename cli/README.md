# Ivy CLI

Unified command-line tool for Ivy Interactive infrastructure. Manage Sliplane servers, services, projects, and deploy Tendril instances — all from one `ivy` command.

## Installation

```bash
# From source (in cli/src/)
dotnet tool install -g --add-source ./nupkg Ivy.Cli

# Or run directly without installing
dotnet run -- <command> [options]
```

## Authentication

### Sliplane commands

```bash
export SLIPLANE_API_KEY=your-sliplane-api-token
# Optional for legacy tokens:
export SLIPLANE_ORG_ID=your-org-id
```

### Tendril commands

```bash
export TENDRIL_BASE_URL=https://your-tendril-deploy.sliplane.app
export TENDRIL_API_KEY=your-internal-api-key   # optional if server has no key set
export SLIPLANE_API_KEY=your-sliplane-token    # also needed for status/servers/projects
```

## Commands

Commands are grouped by project: `ivy sliplane …` for Sliplane resources and `ivy tendril …` for Tendril deployments. CLI-wide settings live under `ivy config …`.

### Sliplane

```bash
ivy sliplane me                                          # current identity

ivy sliplane projects list
ivy sliplane projects create --name my-project
ivy sliplane projects delete --project-id abc123

ivy sliplane servers list
ivy sliplane servers get --server-id srv_123
ivy sliplane servers create --name my-server --instance-type base --location fsn
ivy sliplane servers metrics --server-id srv_123 --range 1h

ivy sliplane services list                               # across all projects
ivy sliplane services list --project-id proj_123
ivy sliplane services get --project-id proj_123 --service-id svc_456
ivy sliplane services create --project-id proj_123 --name my-app --server-id srv_123 --repo https://github.com/org/repo --public
ivy sliplane services deploy --project-id proj_123 --service-id svc_456
ivy sliplane services logs --project-id proj_123 --service-id svc_456
ivy sliplane services pause --project-id proj_123 --service-id svc_456
ivy sliplane services delete --project-id proj_123 --service-id svc_456

ivy sliplane credentials list
ivy sliplane credentials create --name ghcr-creds --type ghcr --username myuser --token ghp_xxx

ivy sliplane oauth list
ivy sliplane oauth get --client-id oauth_123
```

### Tendril

```bash
# List available Sliplane targets
ivy tendril servers
ivy tendril projects

# Deploy a new Tendril instance
ivy tendril deploy \
  --project-id proj_123 \
  --server-id srv_456 \
  --name tendril-artem \
  --username artem \
  --password supersecret \
  --anthropic-key sk-ant-xxx \
  --github-token ghp_xxx \
  --repo https://github.com/Ivy-Interactive/Ivy-Examples

# Check deployment status
ivy tendril status \
  --project-id proj_123 \
  --service-id svc_789
```

## Output

All commands output YAML — easy to read and pipe into other tools.

## Adding new commands

1. Create a new folder under `src/Commands/YourThing/`
2. Add a class inheriting `AsyncCommand<Settings>` (copy any existing command as template)
3. Register it in `Program.cs` under the appropriate top-level branch (`sliplane`, `tendril`, …) with `branch.AddBranch("yourthing", ...)` or `branch.AddCommand<>()`. Add a new top-level branch only when introducing a new project namespace.

### Description style

Keep `WithDescription(...)` strings short, imperative, and consistent. Don't put usage examples there — those belong in this README.

| Verb              | Description pattern         | Example                       |
| ----------------- | --------------------------- | ----------------------------- |
| top-level branch  | `<Action> <project> <scope>`| `Manage Sliplane resources`   |
| sub-branch        | `Manage <resource-plural>`  | `Manage servers`              |
| `list`            | `List <resource-plural>`    | `List servers`                |
| `get`             | `Get <resource>`            | `Get server`                  |
| `create`          | `Create <resource>`         | `Create server`               |
| `update`          | `Update <resource>`         | `Update server`               |
| `delete`          | `Delete <resource>`         | `Delete server`               |
| anything else     | `<Verb> <resource>`         | `Pause service`, `Rescale server` |

Inside a sub-branch, omit the parent name (the path already shows it): write `List servers`, not `List Sliplane servers`.

## Building

```bash
cd cli/src
dotnet build
dotnet run -- --help
```

## Packaging as dotnet tool

```bash
cd cli/src
dotnet pack -c Release -o ../nupkg
dotnet tool install -g --add-source ../nupkg Ivy.Cli
ivy --help
```
