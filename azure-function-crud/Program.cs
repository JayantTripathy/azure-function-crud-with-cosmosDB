using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Register the CosmosClient with the dependency injection container
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var cosmosDbConnectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
    return new CosmosClient(cosmosDbConnectionString);
});

// Register other services if needed
// Register logging services
builder.Services.AddLogging();
// Build the application
builder.Build().Run();