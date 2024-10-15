using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights(); //need to remove the default rule.
        services.AddAzureClients(builder =>
        {
            // Add a storage account client
            builder.AddBlobServiceClient("DefaultEndpointsProtocol=https;AccountName=vjdefenderlandingflat;AccountKey=pfepCQQttUm2E6dyBa0dYTk57ckBkd5j9yjqS75xbK2hHaWxa4LWNAYE04WlGD4zuJpPXNfzF0rt+AStX0Yfog==;EndpointSuffix=core.windows.net");

            //// Select the appropriate credentials based on enviroment
            //builder.UseCredential(new DefaultAzureCredential());
        });
    })
    .ConfigureLogging(logging =>
    {//remove the default log, otherwise the 
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule defaltRule = options.Rules.FirstOrDefault(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaltRule is not null)
            {
                options.Rules.Remove(defaltRule);
            }
        });
    })
    .Build();

host.Run();
