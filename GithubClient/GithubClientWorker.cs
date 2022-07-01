namespace GithubClient
{
    public class GithubClientWorker : BackgroundService
    {
        private readonly ILogger<GithubClientWorker> _logger;
        private readonly GithubClient _githubClient;

        public GithubClientWorker(ILogger<GithubClientWorker> logger,
            GithubClient githubClient)
        {
            _logger = logger;
            _githubClient = githubClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _githubClient.StartAsync(stoppingToken);
            }
        }
    }
}