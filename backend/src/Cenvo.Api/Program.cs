using System.Text.Json.Serialization;
using Cenvo.Api;
using Cenvo.Api.Endpoints;
using Cenvo.Api.Hubs;
using Cenvo.Api.Seed;
using Cenvo.Infrastructure;
using Cenvo.Infrastructure.Persistence;
using Cenvo.Infrastructure.Security;
using Cenvo.Api.Messaging;
using Cenvo.Api.Storage;
using Cenvo.Modules.Cafe.Contracts;
using Cenvo.Modules.CarWash.Contracts;
using Cenvo.Modules.CarWash.Persistence;
using Cenvo.Modules.Cafe.Persistence;
using Cenvo.Modules.Catalog.Persistence;
using Cenvo.Modules.Hr.Persistence;
using Cenvo.Modules.Inventory.Persistence;
using Cenvo.Modules.Organization.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// ---------- Infrastructure & modules ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default")
                                   ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default"));
foreach (var module in new[] { typeof(OrganizationModel), typeof(CatalogModel), typeof(InventoryModel), typeof(CafeModel), typeof(CarWashModel),
             typeof(HrModel) })
    builder.Services.AddModule(module.Assembly);
builder.Services.AddScoped<IKitchenNotifier, KitchenNotifier>();
builder.Services.AddScoped<IWashNotifier, WashNotifier>();
builder.Services.Configure<PhotoStorageOptions>(builder.Configuration.GetSection("Storage:Photos"));
builder.Services.AddSingleton<IPhotoStorage, LocalPhotoStorage>();
// National ID / phone encryption key: only in appsettings.Development.json for dev; elsewhere from an environment
// variable or secret store (PersonalData__Key). Missing → the app refuses to start rather than store them in clear.
builder.Services.AddSingleton<IPersonalDataCipher>(AesGcmPersonalDataCipher.FromBase64(builder.Configuration["PersonalData:Key"]));
// No Zalo OA / SMS account yet: reminders are only logged. Swap for the real provider here.
builder.Services.AddSingleton<IReminderSender, LogReminderSender>();

// ---------- Auth ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtSection);
var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32) throw new InvalidOperationException("Jwt:Key phải dài ít nhất 32 ký tự.");
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer, ValidAudience = jwt.Audience, IssuerSigningKey = JwtTokenService.SigningKey(jwt.Key),
        NameClaimType = System.Security.Claims.ClaimTypes.Name, RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        ClockSkew = TimeSpan.FromSeconds(30),
    };
    // SignalR sends the token in the query string because WebSockets cannot carry headers.
    o.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs")) ctx.Token = token;
            return Task.CompletedTask;
        },
    };
});
builder.Services.AddAuthorization(o => o.AddCenvoPolicies());
builder.Services.AddCenvoRateLimits(builder.Configuration.GetValue<int?>("RateLimits:CredentialAttemptsPerMinute"));

// ---------- Web ----------
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Swashbuckle reads the MVC JSON options to describe schemas: mirror the enum converter so enums appear as strings.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR().AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<ErrorHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cenvo API", Version = "v1" });
    c.CustomSchemaIds(t => t.FullName!.Replace("+", "."));
    // Non-nullable C# members become required in the schema so the frontend generates accurate types.
    c.SupportNonNullableReferenceTypes();
    c.NonNullableReferenceTypesAsRequired();
    var scheme = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter(); // after authentication: the limit is per signed-in user when there is one
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapCatalogEndpoints();
app.MapInventoryEndpoints();
app.MapCafeEndpoints();
app.MapCarWashEndpoints();
app.MapHrEndpoints();
app.MapHub<KitchenHub>("/hubs/kitchen");
app.MapHub<WashHub>("/hubs/carwash");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

if (app.Configuration.GetValue("Database:Migrate", true))
{
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<CurrentUser>().RunAs(SystemUser.AllBranches);
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }
    if (app.Configuration.GetValue("Database:Seed", false)) await DataSeeder.Run(app.Services);
}

app.Run();

public partial class Program;
