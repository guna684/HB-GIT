using System.Text;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. Controller Services
// =========================================================================
builder.Services.AddControllers();

// =========================================================================
// 2. CORS (Cross-Origin Resource Sharing)
// =========================================================================
// Web browsers block requests between different origins (different port or domain).
// Here we explicitly allow our React frontend (running on http://localhost:5173)
// to make requests to this backend.
// We NEVER use AllowAnyOrigin with AllowCredentials for security reasons.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// =========================================================================
// 3. Dependency Injection (DI) & Repositories
// =========================================================================
// For now, we use In-Memory repositories registered as Singletons so our data
// persists across requests while the backend server is running.
//
// LATER: When you want to switch to PostgreSQL with Entity Framework Core,
// you can replace these two lines with your DbContext and Scoped repositories:
//   builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connString));
//   builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
//   builder.Services.AddScoped<IBirthdayRepository, PostgresBirthdayRepository>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IBirthdayRepository, InMemoryBirthdayRepository>();

// =========================================================================
// 4. Application Services
// =========================================================================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BirthdayService>();

// =========================================================================
// 5. Background Hosted Services
// =========================================================================
// Runs in the background every hour to check for upcoming birthdays and log reminders
builder.Services.AddHostedService<ReminderBackgroundService>();

// =========================================================================
// 6. JWT Authentication
// =========================================================================
string secretKey = builder.Configuration["Jwt:Key"] ?? "BirthdayReminderSecretKeyForJwtAuthenticationMustBeVeryLongAndSecure12345!";
string issuer = builder.Configuration["Jwt:Issuer"] ?? "BirthdayReminderApi";
string audience = builder.Configuration["Jwt:Audience"] ?? "BirthdayReminderApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // False for local HTTP development
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// =========================================================================
// 7. Swagger / OpenAPI with JWT Authorization Support
// =========================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Birthday Reminder API",
        Version = "v1",
        Description = "ASP.NET Core Web API for Birthday Reminder web application"
    });

    // Add JWT Bearer definition to Swagger UI so you can test secured endpoints
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the text box below (do not include the word 'Bearer ')."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
// Ensure wwwroot directory exists and WebRootPath is configured before build
string webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(webRoot))
{
    Directory.CreateDirectory(webRoot);
}
builder.Environment.WebRootPath = webRoot;

var app = builder.Build();

// Ensure wwwroot/uploads directory exists for static file serving
string uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// =========================================================================
// 8. Global Error Handling Middleware
// =========================================================================
// Catches unhandled server exceptions and returns a clean, standardized JSON error response
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred while processing request {Path}", context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred. Please try again." });
    }
});

// Configure Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Birthday Reminder API v1");
    });
}

// Serve uploaded profile images from wwwroot
app.UseStaticFiles();

// Apply CORS policy for the React app
app.UseCors("AllowReactApp");

// Authentication must be called BEFORE Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controller routes (e.g. /api/auth and /api/birthdays)
app.MapControllers();

app.Run();
