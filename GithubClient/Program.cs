using GithubClient;
using System.Net.Http.Headers;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        var sp = services.BuildServiceProvider();

        var config = sp.GetRequiredService<IConfiguration>();
        var githubOption = new GithubOptions();
        config.GetSection("Github").Bind(githubOption);

        services.AddSingleton(_ => githubOption);

        services.AddHttpClient<GithubClient.GithubClient>(options =>
        {
            options.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            options.BaseAddress = new Uri("https://api.github.com");
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", $"{githubOption.Token}");
            options.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("github-client", "1.0.0"));
        });

        services.AddHostedService<GithubClientWorker>();
    })
    .Build();


host.Run();
