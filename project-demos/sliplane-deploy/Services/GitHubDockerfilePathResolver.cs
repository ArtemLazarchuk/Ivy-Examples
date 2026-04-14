namespace SliplaneDeploy.Services;

using System.Net;
using System.Text.RegularExpressions;

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
    /// Returns the Dockerfile path to send to Sliplane (unchanged if probe is skipped or inconclusive).
    /// </summary>
    public async Task<string> ResolveAsync(string? gitRepoUrl, string branch, string dockerfilePath, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(dockerfilePath) ? "Dockerfile" : dockerfilePath.Trim();
        if (!TryParseGitHubRepo(gitRepoUrl, out var owner, out var repo))
            return path;

        var branchTrim = string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim();
        if (await ExistsOnGitHubRawAsync(owner, repo, branchTrim, path, cancellationToken).ConfigureAwait(false))
            return path;

        var fallback = (_configuration["Sliplane:DefaultDockerfilePath"] ?? ".github/docker/Dockerfile.ivy-default").Trim();
        if (string.IsNullOrEmpty(fallback) || string.Equals(fallback, path, StringComparison.Ordinal))
            return path;

        if (await ExistsOnGitHubRawAsync(owner, repo, branchTrim, fallback, cancellationToken).ConfigureAwait(false))
            return fallback;

        return path;
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
