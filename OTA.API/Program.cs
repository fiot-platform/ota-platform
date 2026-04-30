using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using OTA.API.BackgroundJobs;
using OTA.API.Extensions;
using OTA.API.Middleware;
using OTA.API.Models.Settings;
using Serilog;
using Serilog.Events;

// ── Bootstrap logger (used before DI is ready) ───────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting OTA Platform API...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    // ── Bind typed settings ───────────────────────────────────────────────────
    builder.Services.Configure<MongoDbSettings>(
        builder.Configuration.GetSection(MongoDbSettings.SectionName));
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection(JwtSettings.SectionName));
    builder.Services.Configure<GiteaSettings>(
        builder.Configuration.GetSection(GiteaSettings.SectionName));
    builder.Services.Configure<CorsSettings>(
        builder.Configuration.GetSection(CorsSettings.SectionName));
    builder.Services.Configure<OTA.API.Models.Settings.MqttSettings>(
        builder.Configuration.GetSection(OTA.API.Models.Settings.MqttSettings.SectionName));
    builder.Services.Configure<FirebaseSettings>(
        builder.Configuration.GetSection(FirebaseSettings.SectionName));

    // ── Firebase initialisation ───────────────────────────────────────────────
    var firebaseSettings = builder.Configuration
        .GetSection(FirebaseSettings.SectionName)
        .Get<FirebaseSettings>();

    if (firebaseSettings?.Enabled == true && !string.IsNullOrWhiteSpace(firebaseSettings.ServiceAccountKeyPath))
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(firebaseSettings.ServiceAccountKeyPath)
                });
                Log.Information("Firebase initialised from {Path}.", firebaseSettings.ServiceAccountKeyPath);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Firebase initialisation failed — push notifications will be disabled.");
        }
    }
    else
    {
        Log.Information("Firebase notifications are disabled (FirebaseSettings.Enabled=false or no key file).");
    }

    // ── Infrastructure ────────────────────────────────────────────────────────
    builder.Services.AddMongoDb(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddRoleBasedAuthorization();
    builder.Services.AddCorsPolicy(builder.Configuration);

    // ── Repository & service layers ───────────────────────────────────────────
    builder.Services.AddApplicationRepositories();
    builder.Services.AddApplicationServices();
    builder.Services.Configure<OTA.API.Models.Settings.EmailSettings>(
        builder.Configuration.GetSection(OTA.API.Models.Settings.EmailSettings.SectionName));

    // ── HTTP client for Gitea API ─────────────────────────────────────────────
    builder.Services.AddHttpClient<OTA.API.Services.Interfaces.IGiteaApiService,
                                   OTA.API.Services.GiteaApiService>(client =>
    {
        var giteaSettings = builder.Configuration
            .GetSection(GiteaSettings.SectionName)
            .Get<GiteaSettings>();

        if (giteaSettings is not null && !string.IsNullOrWhiteSpace(giteaSettings.BaseUrl))
            client.BaseAddress = new Uri(giteaSettings.BaseUrl.TrimEnd('/') + "/api/v1/");

        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // ── MQTT ──────────────────────────────────────────────────────────────────
    builder.Services.AddMqtt(builder.Configuration);

    // ── Background services ───────────────────────────────────────────────────
    builder.Services.AddHostedService<WebhookRetryJob>();
    builder.Services.AddHostedService<OTA.API.BackgroundJobs.MqttBackgroundService>();

    // ── MVC ───────────────────────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter(
                    System.Text.Json.JsonNamingPolicy.CamelCase));
        });

    // ── Swagger ───────────────────────────────────────────────────────────────
    builder.Services.AddSwaggerWithJwt();
    builder.Services.AddEndpointsApiExplorer();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    // ── HTTP context accessor (required by some middleware) ───────────────────
    builder.Services.AddHttpContextAccessor();

    // ── Response caching ──────────────────────────────────────────────────────
    builder.Services.AddResponseCaching(opts => { opts.MaximumBodySize = 4 * 1024 * 1024; });

    // ─────────────────────────────────────────────────────────────────────────
    // Build the application
    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Seed default SuperAdmin if database is empty ──────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var seederLogger = app.Services.GetRequiredService<ILogger<Program>>();
        try
        {
            await OTA.API.Helpers.DataSeeder.SeedAsync(scope.ServiceProvider, seederLogger);
        }
        catch (Exception ex)
        {
            seederLogger.LogWarning(ex,
                "DataSeeder: Could not reach MongoDB — skipping seed. " +
                "The API will still start; ensure the database is reachable.");
        }
    }

    // ── Response caching ──────────────────────────────────────────────────────
    app.UseResponseCaching();

    // ── Global exception handler (must be first in pipeline) ─────────────────
    app.UseMiddleware<ExceptionMiddleware>();

    // ── Swagger UI (always enabled) ──────────────────────────────────────────
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OTA Platform API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
    });

    // ── Serilog request logging ───────────────────────────────────────────────
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        options.GetLevel = (ctx, elapsed, ex) => ex != null || ctx.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : ctx.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
    });

    // ── HTTPS redirection ─────────────────────────────────────────────────────
    app.UseHttpsRedirection();

    // ── CORS (before auth) ────────────────────────────────────────────────────
    app.UseCors("OtaFrontendPolicy");

    // ── Authentication & authorisation ────────────────────────────────────────
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Audit context population ──────────────────────────────────────────────
    // Runs after auth so JWT claims are available.
    app.UseMiddleware<AuditMiddleware>();

    // ── CustomerAdmin scope enforcement ──────────────────────────────────────
    app.UseMiddleware<AuthorizationScopeMiddleware>();

    // ── QA document static file serving ──────────────────────────────────────
    var qaDocsDir = Path.Combine(app.Environment.ContentRootPath, "qa-docs");
    Directory.CreateDirectory(qaDocsDir);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(qaDocsDir),
        RequestPath  = "/qa-docs",
    });

    // ── Firmware binary static file serving ───────────────────────────────────
    var firmwareUploadDir = Path.Combine(app.Environment.ContentRootPath, "firmware-uploads");
    Directory.CreateDirectory(firmwareUploadDir);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(firmwareUploadDir),
        RequestPath  = "/firmware-uploads",
    });

    // ── Health check endpoint ─────────────────────────────────────────────────
    app.MapHealthChecks("/health");

    // ── Controllers ───────────────────────────────────────────────────────────
    app.MapControllers();

    Log.Information("OTA Platform API started successfully. Listening on {Urls}.",
        string.Join(", ", app.Urls));

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "OTA Platform API terminated unexpectedly.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
