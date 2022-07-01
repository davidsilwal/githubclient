
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GithubClient
{
    public class GithubOptions
    {

        public string Token { get; set; } = null!;
        public string LoginName { get; set; } = null!;
        public string DownloadPath { get; set; } = null!;

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
        public GithubClient(
            HttpClient httpClient,
            GithubOptions githubOptions
            )
        {
            _httpClient = httpClient;
            _githubOptions = githubOptions;
        }

        public async ValueTask StartAsync(CancellationToken stoppingToken)
        {
            var response = await _httpClient.GetAsync($"/users/{_githubOptions.LoginName}/repos", stoppingToken);
            var content = await response.Content.ReadAsStreamAsync(stoppingToken);


            if (!Directory.Exists(_githubOptions.DownloadPath))
            {
                Directory.CreateDirectory(_githubOptions.DownloadPath);
            }

            await foreach (var repo in JsonSerializer.DeserializeAsyncEnumerable<Repo>(content, cancellationToken: stoppingToken))
            {
                Console.WriteLine(repo.FullName);
                await DownloadRepoAsZipAsync(repo);
                await DeleteRepoAsync(repo);
            }
        }


        public async ValueTask DownloadRepoAsZipAsync(Repo reo)
        {
            var url = $"https://api.github.com/repos/{reo.FullName}/zipball/master";
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            using var resultStream = await response.Content.ReadAsStreamAsync();
            var pathToWrite = Path.Combine(_githubOptions.DownloadPath, $"{reo.Name}.zip");
            using var fileStream = File.Create(pathToWrite);
            resultStream.CopyTo(fileStream);
        }

        public async ValueTask DeleteRepoAsync(Repo repo)
        {
            var url = $"https://api.github.com/repos/{repo.FullName}";
            await _httpClient.DeleteAsync(url);
        }
    }
}
