using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using NSwag.AspNetCore;
using Api;
using Api.Controllers;

namespace Api;

public static class Program
{
    private const string AppSettingsPath = "Config/Json";

    private static WebApplication BuildApp()
    {
        // Load .env file BEFORE building configuration
        // TraversePath searches upward from current directory to find .env
        Env.TraversePath().Load();
        
        var builder = WebApplication.CreateBuilder();
        
        // Load appsettings from Config/Json since they're not in the default location
        builder.Configuration
            .AddJsonFile($"{AppSettingsPath}/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"{AppSettingsPath}/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Register application services
        builder.Services.AddControllers();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        builder.Services.AddAuthorization();
        builder.Services.AddHealthChecks();
        builder.Services.AddOpenApiDocument();

        // --- Thinger.io ---
        // Add THINGER_ACCESS_TOKEN and THINGER_USERNAME to your .env file
        builder.Services.AddSingleton<ThingerBucketController>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            string token    = config["THINGER_ACCESS_TOKEN"]
                              ?? throw new InvalidOperationException("Missing env var: THINGER_ACCESS_TOKEN");
            string username = config["THINGER_USERNAME"]
                              ?? throw new InvalidOperationException("Missing env var: THINGER_USERNAME");

            // Optional: override base URL for self-hosted instances via THINGER_BASE_URL
            string baseUrl = config["THINGER_BASE_URL"] ?? "https://eu-central.aws.thinger.io";;

            return new ThingerBucketController(token, username, baseUrl);
        });

        Console.WriteLine("Build complete.");
        return builder.Build();
    }

    public static async Task Main(string[] args)
    {
        var app = BuildApp();
        
        // Development-only middleware
        if (app.Environment.IsDevelopment())
        {
            Console.WriteLine("✓ Running in Development mode");
            app.UseOpenApi();
            app.UseSwaggerUi();
        }
        else if(app.Environment.IsProduction())
        {
            Console.WriteLine("✓ Running in Production mode");
        }
        else if(app.Environment.IsStaging())
        {
            Console.WriteLine("✓ Running in Staging mode");
        }
        
        // Configure middleware pipeline
        app.UseCors("AllowFrontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");
        app.UseOpenApi();
        app.UseSwaggerUi();
        
        await app.RunAsync();
    }
}