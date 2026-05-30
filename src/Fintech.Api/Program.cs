using System.Text.Json.Serialization;
using Fintech.Api.Auth;
using Fintech.Api.Data;
using Fintech.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("InvestmentDb")
    ?? "Host=localhost;Port=5432;Database=investment_demo;Username=investment;Password=investment";

builder.Services.AddDbContext<InvestmentDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<InvestmentRequestService>();
builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "postgresql");
builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5186"])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var auth = builder.Configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();
if (auth.Mode == AuthMode.Keycloak)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = auth.Authority;
            options.Audience = auth.Audience;
            options.RequireHttpsMetadata = auth.RequireHttpsMetadata;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                NameClaimType = "preferred_username",
                RoleClaimType = "roles"
            };
        });
}
else
{
    builder.Services.AddAuthentication(DevelopmentAuthHandler.SchemeName)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevelopmentAuthHandler>(
            DevelopmentAuthHandler.SchemeName,
            _ => { });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Operator, policy => policy.RequireRole("operator"));
    options.AddPolicy(Policies.Auditor, policy => policy.RequireRole("auditor"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();
app.MapControllers();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<InvestmentDbContext>().Database.MigrateAsync();
}

app.Run();

public partial class Program;
