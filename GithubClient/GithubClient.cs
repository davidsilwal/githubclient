
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GithubClient
{
    public class GithubOptions
    {

        public string Token { get; set; } = null!;
        public string LoginName { get; set; } = null!;
        public string? DownloadPath { get; set; }
        public bool DeleteAfterClone { get; set; }
    };


    public record Repo(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("full_name")] string FullName
        );

    public class GithubClient
    {
        private readonly HttpClient _httpClient;
        private readonly GithubOptions _githubOptions;
        readonly string _downloadPath;
        public GithubClient(
            HttpClient httpClient,
            GithubOptions githubOptions
            )
        {
            _httpClient = httpClient;
            _githubOptions = githubOptions;


            _downloadPath = _githubOptions.DownloadPath is { Length: > 0 }
                ? _githubOptions.DownloadPath
                : Environment.ExpandEnvironmentVariables("%userprofile%/downloads/");
        }

        public async ValueTask StartAsync(CancellationToken stoppingToken)
        {
            var response = await _httpClient.GetAsync($"/users/{_githubOptions.LoginName}/repos", stoppingToken);
            var content = await response.Content.ReadAsStreamAsync(stoppingToken);

            if (!Directory.Exists(_downloadPath))
            {
                Directory.CreateDirectory(_downloadPath);
            }

            await foreach (var repo in JsonSerializer.DeserializeAsyncEnumerable<Repo>(content, cancellationToken: stoppingToken))
            {
                Console.WriteLine(repo!.FullName);
                await DownloadRepoAsZipAsync(repo);

                if (_githubOptions.DeleteAfterClone)
                {
                    await DeleteRepoAsync(repo);
                }
            }
        }


        public async ValueTask DownloadRepoAsZipAsync(Repo reo)
        {
            var url = $"https://api.github.com/repos/{reo.FullName}/zipball/master";
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            using var resultStream = await response.Content.ReadAsStreamAsync();
            var pathToWrite = Path.Combine(_downloadPath, $"{reo.Name}.zip");
            using var fs = new FileStream(pathToWrite, FileMode.Create);
            await resultStream.CopyToAsync(fs);

        }

        public async ValueTask DeleteRepoAsync(Repo repo)
        {
            var url = $"https://api.github.com/repos/{repo.FullName}";
            await _httpClient.DeleteAsync(url);
        }
    }
}
