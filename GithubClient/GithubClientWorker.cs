namespace GithubClient
{
    public class GithubClientWorker : IHostedService
    {
        private readonly ILogger<GithubClientWorker> _logger;
        private readonly GithubClient _githubClient;

        public GithubClientWorker(ILogger<GithubClientWorker> logger,
            GithubClient githubClient)
        {
            _logger = logger;
            _githubClient = githubClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _githubClient.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}