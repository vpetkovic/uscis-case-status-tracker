using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using USCIS.Common;

[assembly: FunctionsStartup(typeof(Worker.USCIS.Startup))]

namespace Worker.USCIS
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<IDataAccess, DataAccess>(_ => new DataAccess(config["AzureTableStorage:ConnectionString"], config["AzureTableStorage:TableName"]));
            builder.Services.AddHttpClient<StatusTrackerClient>(c =>
            {
                c.BaseAddress = new Uri(config["USCISRequestUrl"]);
            });

        }
    }
}