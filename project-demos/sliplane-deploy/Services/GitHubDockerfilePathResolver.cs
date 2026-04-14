namespace SliplaneDeploy.Services;

using System.Net;
using System.Text.RegularExpressions;
using SliplaneDeploy.Models;

/// <summary>
/// Resolved paths for Sliplane git deploy. When the repo has no Dockerfile at the requested path,
/// falls back to the configured default Dockerfile path and, for monorepo subfolders, uses
/// repository root as Docker context plus <c>IVY_APP_DIR</c> so the shared Dockerfile can copy the app.
/// </summary>
public record DockerfileResolution(
    string DockerfilePath,
    string DockerContext,
    IReadOnlyList<EnvironmentVariable>? AdditionalEnv = null);

/// <summary>
/// If the requested Dockerfile is missing from a GitHub repo, returns a repository-relative path
/// to a shared default Dockerfile (must exist on the same default branch / fork).
/// </summary>
public class GitHubDockerfilePathResolver
{
    private static readonly Regex GitHubRepoRegex = new(
        @"^https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?/?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GitHubDockerfilePathResolver(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public static bool TryParseGitHubRepo(string? gitRepoUrl, out string owner, out string repo)
    {
        owner = repo = "";
        if (string.IsNullOrWhiteSpace(gitRepoUrl)) return false;
        var trimmed = gitRepoUrl.Trim().TrimEnd('/');
        var m = GitHubRepoRegex.Match(trimmed);
        if (!m.Success) return false;
        owner = m.Groups["owner"].Value;
        repo = m.Groups["repo"].Value;
        return true;
    }

    /// <summary>
    /// Resolves Dockerfile path and Docker context for Sliplane. When the shared default Dockerfile
    /// is used with a non-root context, returns <see cref="DockerContext"/> as "." and sets
    /// <c>IVY_APP_DIR</c> (available during build on Sliplane) for the shared Ivy Dockerfile.
    /// </summary>
    public async Task<DockerfileResolution> ResolveAsync(
        string? gitRepoUrl,
        string branch,
        string dockerfilePath,
        string dockerContext,
        CancellationToken cancellationToken = default)
    {
        var contextTrim = string.IsNullOrWhiteSpace(dockerContext) ? "." : dockerContext.Trim();
        var path = string.IsNullOrWhiteSpace(dockerfilePath) ? "Dockerfile" : dockerfilePath.Trim();
        if (!TryParseGitHubRepo(gitRepoUrl, out var owner, out var repo))
            return new DockerfileResolution(path, contextTrim);

        var branchTrim = string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim();
        if (await ExistsOnGitHubRawAsync(owner, repo, branchTrim, path, cancellationToken).ConfigureAwait(false))
            return new DockerfileResolution(path, contextTrim);

        var fallback = (_configuration["Sliplane:DefaultDockerfilePath"] ?? ".github/docker/Dockerfile.ivy-default").Trim();
        if (string.IsNullOrEmpty(fallback) || string.Equals(fallback, path, StringComparison.Ordinal))
            return new DockerfileResolution(path, contextTrim);

        if (!await ExistsOnGitHubRawAsync(owner, repo, branchTrim, fallback, cancellationToken).ConfigureAwait(false))
            return new DockerfileResolution(path, contextTrim);

        if (!IsNonRootRepositoryContext(contextTrim))
            return new DockerfileResolution(fallback, contextTrim);

        var ivyAppDir = NormalizeRepoRelativePath(contextTrim);
        return new DockerfileResolution(
            fallback,
            DockerContext: ".",
            AdditionalEnv: [new EnvironmentVariable("IVY_APP_DIR", ivyAppDir, Secret: false)]);
    }

    private static bool IsNonRootRepositoryContext(string dockerContext)
    {
        var t = dockerContext.Replace('\\', '/').Trim().Trim('/');
        return t.Length != 0 && !string.Equals(t, ".", StringComparison.Ordinal);
    }

    /// <summary>Repo-relative path with forward slashes, no leading/trailing slashes.</summary>
    private static string NormalizeRepoRelativePath(string dockerContext)
    {
        var t = dockerContext.Replace('\\', '/').Trim().Trim('/');
        while (t.StartsWith("./", StringComparison.Ordinal))
            t = t[2..];
        return t;
    }

    private async Task<bool> ExistsOnGitHubRawAsync(string owner, string repo, string branch, string repoRelativePath, CancellationToken cancellationToken)
    {
        var url = BuildRawUrl(owner, repo, branch, repoRelativePath);
        try
        {
            using var client = _httpClientFactory.CreateClient("GitHubRaw");
            using (var head = new HttpRequestMessage(HttpMethod.Head, url))
            {
                using var headResponse = await client.SendAsync(head, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (headResponse.StatusCode == HttpStatusCode.OK) return true;
                if (headResponse.StatusCode == HttpStatusCode.NotFound) return false;
            }

            using (var get = new HttpRequestMessage(HttpMethod.Get, url))
            {
                using var getResponse = await client.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (getResponse.StatusCode == HttpStatusCode.OK) return true;
                if (getResponse.StatusCode == HttpStatusCode.NotFound) return false;
            }

            // Rate limits, private repo without token, etc. — do not treat as "missing".
            return true;
        }
        catch
        {
            return true;
        }
    }

    private static string BuildRawUrl(string owner, string repo, string branch, string repoRelativePath)
    {
        var normalized = repoRelativePath.Replace('\\', '/').TrimStart('/');
        var encodedPath = string.Join("/", normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
        return $"https://raw.githubusercontent.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/{Uri.EscapeDataString(branch)}/{encodedPath}";
    }
}
