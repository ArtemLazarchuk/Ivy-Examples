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

### Sliplane

```bash
ivy me                                          # current identity

ivy projects list
ivy projects create --name my-project
ivy projects delete --project-id abc123

ivy servers list
ivy servers get --server-id srv_123
ivy servers create --name my-server --instance-type base --location fsn
ivy servers metrics --server-id srv_123 --range 1h

ivy services list                               # across all projects
ivy services list --project-id proj_123
ivy services get --project-id proj_123 --service-id svc_456
ivy services create --project-id proj_123 --name my-app --server-id srv_123 --repo https://github.com/org/repo --public
ivy services deploy --project-id proj_123 --service-id svc_456
ivy services logs --project-id proj_123 --service-id svc_456
ivy services pause --project-id proj_123 --service-id svc_456
ivy services delete --project-id proj_123 --service-id svc_456

ivy credentials list
ivy credentials create --name ghcr-creds --type ghcr --username myuser --token ghp_xxx

ivy oauth list
ivy oauth get --client-id oauth_123
```

### Tendrils

```bash
# List available Sliplane targets
ivy tendrils servers
ivy tendrils projects

# Deploy a new Tendril instance
ivy tendrils deploy \
  --project-id proj_123 \
  --server-id srv_456 \
  --name tendril-artem \
  --username artem \
  --password supersecret \
  --anthropic-key sk-ant-xxx \
  --github-token ghp_xxx \
  --repo https://github.com/Ivy-Interactive/Ivy-Examples

# Check deployment status
ivy tendrils status \
  --project-id proj_123 \
  --service-id svc_789
```

## Output

All commands output YAML — easy to read and pipe into other tools.

## Adding new commands

1. Create a new folder under `src/Commands/YourThing/`
2. Add a class inheriting `AsyncCommand<Settings>` (copy any existing command as template)
3. Register it in `Program.cs` with `config.AddBranch("yourthing", ...)` or `config.AddCommand<>()`

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
