using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(azure_function_crud.Startup))]

namespace azure_function_crud
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Get CosmosDB connection string from environment variables
            string cosmosDbConnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");

            // Validate the connection string
            if (string.IsNullOrEmpty(cosmosDbConnectionString))
            {
                throw new InvalidOperationException("The CosmosDB connection string is missing in environment settings.");
            }

            // Register CosmosClient as a singleton
            builder.Services.AddSingleton<CosmosClient>((s) =>
            {
                return new CosmosClient(cosmosDbConnectionString);
            });

            // Register other services if needed
        }
    }
}
