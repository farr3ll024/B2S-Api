using Azure.Identity;
using B2S_Api.Services;

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
            if (!string.IsNullOrEmpty(portalUrl))
                policy.WithOrigins(portalUrl)
                    .AllowAnyHeader()
                    .AllowAnyMethod();

            var portalUrl2 = builder.Configuration["PortalUrl2"];
            if (!string.IsNullOrEmpty(portalUrl2))
                policy.WithOrigins(portalUrl2)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
        });
});

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