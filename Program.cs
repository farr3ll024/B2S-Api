using Azure.Identity;
using B2S_Api.Services;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------
// Add Azure Key Vault
// -------------------------------
var keyVaultUrl = builder.Configuration["KeyVaultUrl"];
if (!string.IsNullOrEmpty(keyVaultUrl))
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

// -------------------------------
// Add Azure App Configuration
// -------------------------------
builder.Configuration.AddAzureAppConfiguration(options =>
{
    var appConfigConnectionString = builder.Configuration["AppConfigConnectionString"];
    if (!string.IsNullOrEmpty(appConfigConnectionString)) options.Connect(appConfigConnectionString).Select("*");
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            var portalUrl = builder.Configuration["PortalUrl"];
            var portalUrl2 = builder.Configuration["PortalUrl2"];

            if (!string.IsNullOrEmpty(portalUrl) && !string.IsNullOrEmpty(portalUrl2))
                policy.WithOrigins(portalUrl, portalUrl2, "http://localhost:3003")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
        });
});


const string databaseId = "communication";
const string containerId = "emails";
var cosmosdbConnectionString = builder.Configuration["COSMOS_DB_CONNECTION_STRING"];
var cosmosAccountKey = builder.Configuration["COSMOS_DB_ACCOUNT_KEY"];
var cosmosAccountEndpoint = builder.Configuration["COSMOS_DB_ENDPOINT"];

if (!string.IsNullOrEmpty(cosmosdbConnectionString) &&
    !string.IsNullOrEmpty(cosmosAccountKey) &&
    !string.IsNullOrEmpty(cosmosAccountEndpoint) &&
    !string.IsNullOrEmpty(databaseId) &&
    !string.IsNullOrEmpty(containerId))
{
    builder.Services.AddSingleton(_ => new CosmosClient(cosmosdbConnectionString));

    builder.Services.AddTransient<CosmosDbService>(sp =>
    {
        var cosmosClient = sp.GetRequiredService<CosmosClient>();
        var cosmosDbService = new CosmosDbService(cosmosClient, databaseId, containerId);
        return cosmosDbService;
    });
}

// Add Background Service
builder.Services.AddHostedService<MessageSenderService>();

// -------------------------------
// Add services to the container
// -------------------------------
builder.Services.AddControllers();
builder.Services.AddSingleton<EmailService>();

var app = builder.Build();


// -------------------------------
// Middleware pipeline
// -------------------------------
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthorization();

// -------------------------------
// Map controllers
// -------------------------------
app.MapControllers();

// -------------------------------
// Run the application
// -------------------------------
app.Run();