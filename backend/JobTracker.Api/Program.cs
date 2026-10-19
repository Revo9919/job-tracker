using FluentValidation;
using FluentValidation.AspNetCore;
using JobTracker.Api.Middleware;
using JobTracker.Api.Validators;
using JobTracker.Business.Matching.Providers;
using JobTracker.Business.Services.Implementations;
using JobTracker.Business.Services.Interfaces;
using JobTracker.DataAccess.Context;
using JobTracker.DataAccess.Repositories.Implementations;
using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.DataAccess.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddDbContext<JobTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("JobTracker.DataAccess")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICvProfileRepository, CvProfileRepository>();
builder.Services.AddScoped<IMatchResultRepository, MatchResultRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ILlmProvider, GeminiLlmProvider>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICvProfileService, CvProfileService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<SignupValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is missing from appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Swagger removed here to avoid OpenAPI package version mismatches in this workspace.
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Try-Catch to extract the exact LoaderExceptions causing the crash
try
{
    app.MapControllers();
}
catch (ReflectionTypeLoadException ex)
{
    Console.WriteLine("=== REFLECTION TYPE LOAD EXCEPTION ===");
    foreach (var loaderEx in ex.LoaderExceptions)
    {
        if (loaderEx != null)
        {
            Console.WriteLine(loaderEx.Message);
        }
    }
    Console.WriteLine("======================================");
    throw; // Rethrow to halt startup
}

app.Run();