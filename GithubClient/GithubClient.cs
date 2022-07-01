
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

        readonly string _logFile;

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

            _logFile = Path.Combine(_downloadPath, "log.txt");
        }

        public async ValueTask StartAsync(CancellationToken stoppingToken)
        {
            var response = await _httpClient.GetAsync($"/users/{_githubOptions.LoginName}/repos", stoppingToken);
            var content = await response.Content.ReadAsStreamAsync(stoppingToken);

            if (!Directory.Exists(_downloadPath))
            {
                Directory.CreateDirectory(_downloadPath);
            }
            var logfileStream = new FileStream(_logFile, File.Exists(_logFile) ? FileMode.Append : FileMode.OpenOrCreate);
            using var logWriter = new StreamWriter(logfileStream);


            var repos = JsonSerializer.DeserializeAsyncEnumerable<Repo>(content, cancellationToken: stoppingToken);
            var options = new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = stoppingToken };
            await Parallel.ForEachAsync(repos, options, async (repo, ct) =>
            {

                await logWriter.WriteLineAsync($"Processing started {repo!.FullName}");
                Console.WriteLine($"Processing started {repo!.FullName}");

                await DownloadRepoAsZipAsync(repo, logWriter);
                if (_githubOptions.DeleteAfterClone)
                {
                    await DeleteRepoAsync(repo, logWriter);
                }
            });

        }

        public async ValueTask DownloadRepoAsZipAsync(Repo repo, StreamWriter writer)
        {

            Console.WriteLine($"Downloading started {repo!.FullName}");
            await writer.WriteLineAsync($"Downloading started {repo!.FullName}");

            var url = $"https://api.github.com/repos/{repo.FullName}/zipball/master";
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            using var resultStream = await response.Content.ReadAsStreamAsync();
            var pathToWrite = Path.Combine(_downloadPath, $"{repo.Name}.zip");
            using var fs = new FileStream(pathToWrite, FileMode.Create);
            await resultStream.CopyToAsync(fs);


            Console.WriteLine($"Downloading finished {repo!.FullName}");
            await writer.WriteLineAsync($"Downloading finished {repo!.FullName}");

        }

        public async ValueTask DeleteRepoAsync(Repo repo, StreamWriter writer)
        {

            Console.WriteLine($"Deleting started {repo!.FullName}");
            await writer.WriteLineAsync($"Deleting started {repo!.FullName}");

            var url = $"https://api.github.com/repos/{repo.FullName}";
            await _httpClient.DeleteAsync(url);


            Console.WriteLine($"Deleting finished {repo!.FullName}");
            await writer.WriteLineAsync($"Deleting finished {repo!.FullName}");
        }


    }
}
