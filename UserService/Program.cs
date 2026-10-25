using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;
using UserService.Authorization;
using UserService.Data;
using UserService.Filters;
using UserService.Middleware;
using UserService.Services;
using UserService.Settings;

namespace UserService
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
                        "logs/userservice-.log",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} - {Message:lj} {Properties:j}{NewLine}{Exception}")
                    );

                // Add services to the container.
                builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
                builder.Services.AddDbContext<UserDbContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
                builder.Services.AddScoped<IUserService, UserService.Services.UserService>();
                builder.Services.AddScoped<ITokenService, TokenService>();
                builder.Services.AddScoped<NotFoundExceptionFilter>();

                var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

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
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

                builder.Services.Configure<ApiBehaviorOptions>(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                });

                builder.Services.AddHttpContextAccessor();
                builder.Services.AddSingleton<IAuthorizationHandler, SameUserHandler>();

                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("SameUserOnly", policy =>
                    {
                        policy.Requirements.Add(new SameUserRequirement());
                    });
                });

                builder.Services.AddControllers(options =>
                {
                    options.Filters.Add<ValidationFilter>();
                    options.Filters.Add<ResponseEnvelopeFilter>();
                });
                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                builder.Services.AddOpenApi();

                var app = builder.Build();

                if (app.Configuration.GetValue<bool>("MigrateOnStartup"))
                {
                    using var migrationScope = app.Services.CreateScope();
                    migrationScope.ServiceProvider.GetRequiredService<UserDbContext>().Database.Migrate();
                }

                var version = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

                app.MapGet("/health", (IConfiguration config) => Results.Ok(new
                {
                    status = "ok",
                    version,
                    slot = config["SLOT_LABEL"] ?? "unset"
                })).AllowAnonymous();

                app.UseMiddleware<CorrelationIdMiddleware>();
                app.UseMiddleware<ExceptionHandler>();
                app.UseMiddleware<RequestLogger>();

                // Configure the HTTP request pipeline.
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
            catch (Exception ex)
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
