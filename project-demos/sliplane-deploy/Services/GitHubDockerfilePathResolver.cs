namespace SliplaneDeploy.Services;

using System.Net;
using System.Text.RegularExpressions;

/// <summary>
/// Result of resolving which Dockerfile and Docker context to send to Sliplane.
/// </summary>
public record DockerfileResolution(string DockerfilePath, string DockerContext);

/// <summary>
/// If the requested Dockerfile is missing from a GitHub repo, returns a repository-relative path
/// to a shared default Dockerfile (must exist on the same default branch / fork).
/// Docker context stays the app folder (e.g. <c>project-demos/book-library</c>); the Dockerfile path
/// is from the repo root (e.g. <c>.github/docker/Dockerfile.ivy-default</c>) so <c>docker build -f … context</c>
/// matches normal Docker behaviour: <c>COPY . .</c> only sees that folder.
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
    /// Resolves Dockerfile path and Docker context. When falling back to the shared Dockerfile,
    /// keeps <paramref name="dockerContext"/> as the app directory (monorepo subfolder).
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

        return new DockerfileResolution(fallback, contextTrim);
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
