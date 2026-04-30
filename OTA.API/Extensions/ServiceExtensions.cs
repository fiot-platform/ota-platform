using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using OTA.API.Helpers;
using OTA.API.Models.Settings;
using OTA.API.Repositories;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services;
using OTA.API.Services.Interfaces;

namespace OTA.API.Extensions
{
    /// <summary>
    /// Static extension methods used by Program.cs to organise service registration into
    /// logical groups. Each method extends <see cref="IServiceCollection"/> and follows the
    /// AddXxx naming convention used by the ASP.NET Core framework.
    /// </summary>
    public static class ServiceExtensions
    {
        // ─────────────────────────────────────────────────────────────────────
        // MongoDB
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers <see cref="IMongoClient"/> and <see cref="IMongoDatabase"/> as singletons
        /// using configuration from the MongoDbSettings section.
        /// </summary>
        public static IServiceCollection AddMongoDb(
            this IServiceCollection services,
            IConfiguration config)
        {
            var settings = config
                .GetSection(MongoDbSettings.SectionName)
                .Get<MongoDbSettings>()
                ?? throw new InvalidOperationException(
                    $"Missing configuration section '{MongoDbSettings.SectionName}'.");

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException("MongoDbSettings.ConnectionString must not be empty.");

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
                throw new InvalidOperationException("MongoDbSettings.DatabaseName must not be empty.");

            // IMongoClient is thread-safe and expensive to create — register as singleton.
            services.AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString));

            // IMongoDatabase is also lightweight — use the singleton client.
            services.AddSingleton<IMongoDatabase>(sp =>
                sp.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName));

            // Bind settings as a singleton options object for injection elsewhere.
            services.Configure<MongoDbSettings>(config.GetSection(MongoDbSettings.SectionName));

            return services;
        }

        // ─────────────────────────────────────────────────────────────────────
        // JWT Authentication
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Configures JWT Bearer authentication with issuer, audience, lifetime, and signing key
        /// validation using settings from the JwtSettings configuration section.
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration config)
        {
            var jwtSettings = config
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>()
                ?? throw new InvalidOperationException(
                    $"Missing configuration section '{JwtSettings.SectionName}'.");

            if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
                throw new InvalidOperationException("JwtSettings.Secret must not be empty.");

            var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

            services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false; // Set to true in production behind TLS termination
                    options.SaveToken = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidIssuer              = jwtSettings.Issuer,
                        ValidateAudience         = true,
                        ValidAudience            = jwtSettings.Audience,
                        ValidateLifetime         = true,
                        ClockSkew                = TimeSpan.FromSeconds(30),
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey         = new SymmetricSecurityKey(key)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            // Suppress default redirect behaviour; return JSON 401.
                            context.HandleResponse();
                            context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            return context.Response.WriteAsync(
                                "{\"success\":false,\"message\":\"Authentication required. Please provide a valid Bearer token.\",\"statusCode\":401}");
                        },
                        OnForbidden = context =>
                        {
                            context.Response.StatusCode  = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";
                            return context.Response.WriteAsync(
                                "{\"success\":false,\"message\":\"You do not have permission to perform this action.\",\"statusCode\":403}");
                        }
                    };
                });

            return services;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Authorization Policies
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers all RBAC authorization policies. One policy is created per role,
        /// plus composite policies for multi-role actions.
        /// </summary>
        public static IServiceCollection AddRoleBasedAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // ── Per-role policies ─────────────────────────────────────────
                options.AddPolicy("SuperAdmin",        p => p.RequireRole("SuperAdmin"));
                options.AddPolicy("PlatformAdmin",     p => p.RequireRole("PlatformAdmin"));
                options.AddPolicy("ReleaseManager",    p => p.RequireRole("ReleaseManager"));
                options.AddPolicy("QA",                p => p.RequireRole("QA"));
                options.AddPolicy("CustomerAdmin",     p => p.RequireRole("CustomerAdmin"));
                options.AddPolicy("Viewer",            p => p.RequireRole("Viewer"));
                options.AddPolicy("Device",            p => p.RequireRole("Device"));

                // ── Composite policies ────────────────────────────────────────

                // Firmware approval: SuperAdmin, PlatformAdmin, or ReleaseManager
                options.AddPolicy("CanApproveFirmware", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "ReleaseManager"));

                // QA session management: QA engineers, Release Manager, plus admins
                options.AddPolicy("CanRunQASession", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "ReleaseManager", "QA"));

                // Device management: SuperAdmin or PlatformAdmin
                options.AddPolicy("CanManageDevices", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin"));

                // Firmware push to device: admins + ReleaseManager
                options.AddPolicy("CanPushFirmware", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "ReleaseManager"));

                // Audit log access: SuperAdmin, PlatformAdmin, ReleaseManager, QA
                options.AddPolicy("CanViewAudit", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "ReleaseManager", "QA"));

                // Repository sync: SuperAdmin, PlatformAdmin, ReleaseManager
                options.AddPolicy("CanSyncRepository", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "ReleaseManager"));

                // Rollout management: SuperAdmin, PlatformAdmin, ReleaseManager
                options.AddPolicy("CanManageRollouts", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "ReleaseManager"));

                // Rollout control (pause/resume/cancel): SuperAdmin, PlatformAdmin
                options.AddPolicy("CanControlRollouts", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin"));

                // Report access (all human roles excluding Device)
                options.AddPolicy("CanViewReports", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "ReleaseManager", "QA",
                                  "CustomerAdmin", "Viewer"));

                // User management: SuperAdmin, PlatformAdmin
                options.AddPolicy("CanManageUsers", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin"));

                // Project management: SuperAdmin, PlatformAdmin
                options.AddPolicy("CanManageProjects", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin"));

                // Firmware sync from Gitea
                options.AddPolicy("CanSyncFirmware", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "ReleaseManager"));

                // Device view: all except Device role itself
                options.AddPolicy("CanViewDevices", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "CustomerAdmin", "ReleaseManager", "QA", "Viewer"));

                // OTA job acknowledgement: SuperAdmin or ReleaseManager
                options.AddPolicy("CanAcknowledgeOta", p =>
                    p.RequireRole("SuperAdmin", "ReleaseManager"));

                // Client management
                options.AddPolicy("CanViewClients", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin", "ReleaseManager", "QA", "CustomerAdmin", "Viewer"));
                options.AddPolicy("CanManageClients", p =>
                    p.RequireRole("SuperAdmin", "PlatformAdmin"));
            });

            return services;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Application Services
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers all application service implementations as scoped dependencies.
        /// Each service is registered against its corresponding interface.
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // QA session management
            services.AddScoped<IQAService, OTA.API.Services.QAService>();

            // Auth & user management
            services.AddScoped<IAuthService, OTA.API.Services.AuthService>();
            services.AddScoped<IUserService, OTA.API.Services.UserService>();

            // Firmware & repository
            services.AddScoped<IFirmwareService, OTA.API.Services.FirmwareService>();
            services.AddScoped<IRepositoryService, OTA.API.Services.RepositoryService>();

            // Project
            services.AddScoped<IProjectService, OTA.API.Services.ProjectService>();

            // OTA rollouts & jobs
            services.AddScoped<IOtaService, OTA.API.Services.OtaService>();

            // Device management
            services.AddScoped<IDeviceService, OTA.API.Services.DeviceService>();

            // Gitea webhook processing (GiteaApiService is registered via AddHttpClient in Program.cs)
            services.AddScoped<IGiteaWebhookService, OTA.API.Services.GiteaWebhookService>();
            // NOTE: IGiteaApiService is intentionally omitted here — it is registered via
            // builder.Services.AddHttpClient<IGiteaApiService, GiteaApiService>() in Program.cs
            // so that the named HttpClient with the correct base address and auth header is injected.

            // Audit
            services.AddScoped<IAuditService, OTA.API.Services.AuditService>();

            // Reports
            services.AddScoped<IReportService, OTA.API.Services.ReportService>();

            // Supporting services
            services.AddScoped<IVersionComparisonService, OTA.API.Services.VersionComparisonService>();
            services.AddScoped<IRolloutPolicyService, OTA.API.Services.RolloutPolicyService>();

            // Audit context (scoped per-request)
            services.AddScoped<AuditContext>();

            // Firebase push notifications
            services.AddScoped<INotificationService, OTA.API.Services.FirebaseNotificationService>();

            // Email notifications
            services.AddScoped<IEmailService, OTA.API.Services.EmailService>();

            // Client management
            services.AddScoped<IClientService, OTA.API.Services.ClientService>();

            return services;
        }

        // ─────────────────────────────────────────────────────────────────────
        // MQTT
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers the MQTT service as a singleton and binds configuration from the
        /// MqttSettings section. The <see cref="MqttService"/> instance is shared
        /// across the application lifetime and used by <see cref="BackgroundJobs.MqttBackgroundService"/>.
        /// </summary>
        public static IServiceCollection AddMqtt(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.Configure<MqttSettings>(config.GetSection(MqttSettings.SectionName));

            // Register as both the concrete type (needed by MqttBackgroundService for
            // subscription setup) and the interface (used by any service that publishes).
            services.AddSingleton<MqttService>();
            services.AddSingleton<IMqttService>(sp => sp.GetRequiredService<MqttService>());

            return services;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Application Repositories
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers all repository implementations as scoped dependencies.
        /// </summary>
        public static IServiceCollection AddApplicationRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDeviceOtaEventRepository, DeviceOtaEventRepository>();
            services.AddScoped<IQASessionRepository, QASessionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IFirmwareRepository, FirmwareRepository>();
            services.AddScoped<IDeviceRepository, DeviceRepository>();
            services.AddScoped<IRolloutRepository, RolloutRepository>();
            services.AddScoped<IOtaJobRepository, OtaJobRepository>();
            services.AddScoped<IRepositoryMasterRepository, RepositoryMasterRepository>();
            services.AddScoped<IRepositoryEventRepository, RepositoryEventRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IRolloutPolicyRepository, RolloutPolicyRepository>();
            services.AddScoped<IEmailNotificationSettingsRepository, EmailNotificationSettingsRepository>();
            services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();

            return services;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Swagger / OpenAPI
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Configures Swashbuckle with JWT Bearer security scheme so that the Swagger UI
        /// can issue authenticated requests directly from the browser.
        /// </summary>
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title       = "OTA Platform API",
                    Version     = "v1",
                    Description = "Enterprise Over-The-Air firmware update platform REST API.",
                    Contact     = new OpenApiContact
                    {
                        Name  = "OTA Platform Team",
                        Email = "platform@example.com"
                    }
                });

                // Include XML documentation comments in Swagger UI.
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath);

                // Add JWT Bearer security scheme.
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name         = "Authorization",
                    Description  = "Enter your JWT Bearer token: **Bearer &lt;token&gt;**",
                    In           = ParameterLocation.Header,
                    Type         = SecuritySchemeType.Http,
                    Scheme       = "bearer",
                    BearerFormat = "JWT",
                    Reference    = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                };

                options.AddSecurityDefinition("Bearer", securityScheme);

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securityScheme, Array.Empty<string>() }
                });
            });

            return services;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CORS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Configures the CORS policy using allowed origins from the CorsSettings configuration section.
        /// </summary>
        public static IServiceCollection AddCorsPolicy(
            this IServiceCollection services,
            IConfiguration config)
        {
            var corsSettings = config
                .GetSection(CorsSettings.SectionName)
                .Get<CorsSettings>()
                ?? new CorsSettings();

            services.AddCors(options =>
            {
                options.AddPolicy("OtaFrontendPolicy", policy =>
                {
                    if (corsSettings.AllowedOrigins.Length == 0)
                    {
                        // Fallback: allow localhost origins in development.
                        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                    else
                    {
                        policy.WithOrigins(corsSettings.AllowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                });
            });

            return services;
        }
    }
}
