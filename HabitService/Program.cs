
using HabitService.Data;
using HabitService.Filters;
using HabitService.Integrations.Anthropic;
using HabitService.Middlewares;
using HabitService.Repositories;
using HabitService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

namespace HabitService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        "logs/habitservice-.log",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} - {Message:lj} {Properties:j}{NewLine}{Exception}")
                    );

                // Add services to the container.
                builder.Services.AddDbContext<HabitDbContext>(options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                });
                builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>(client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration["NotificationServiceBaseUrl"]!);
                });

                builder.Services.Configure<AnthropicOptions>(builder.Configuration.GetSection("Anthropic"));
                builder.Services.AddHttpClient<IInsightsClient, AnthropicInsightsClient>((serviceProvider, client) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<AnthropicOptions>>().Value;

                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
                    client.DefaultRequestHeaders.Add("anthropic-version", options.ApiVersion);
                    client.Timeout = TimeSpan.FromSeconds(20); // slightly above our per-call CancelAfter(15s), as a hard outer bound
                });

                builder.Services.AddScoped<IHabitRepository, HabitRepository>();
                builder.Services.AddScoped<IHabitService, HabitService.Services.HabitService>();
                builder.Services.AddScoped<ApiKeyAuthorizationFilter>();
                builder.Services.AddSingleton<IStreakCacheService, StreakCacheService>();
                builder.Services.AddScoped<StreakCacheResourceFilter>();
                builder.Services.AddScoped<IHabitInsightService, HabitInsightsService>();
                builder.Services.AddSingleton<IInsightCacheService, InsightCacheService>();

                builder.Services.AddControllers();
                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                builder.Services.AddOpenApi();

                var jwtSettings = builder.Configuration.GetSection("Jwt");

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
                    };
                });

                builder.Services.AddAuthorization();

                var app = builder.Build();

                app.UseMiddleware<CorrelationIdMiddleware>();
                app.UseMiddleware<ExceptionHandler>();
                app.UseMiddleware<RequestLogger>();

                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.MapScalarApiReference(); // adds a browsable UI at /scalar/v1
                }

                app.UseHttpsRedirection();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();

                app.Run();
            }
            catch(Exception ex)
            {
                Log.Fatal(ex, "The service terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
