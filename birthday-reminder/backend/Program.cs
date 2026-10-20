using System.Text;
using backend.Data;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// =============================================================================
// PROGRAM.CS - BACKEND ENTRY POINT
// =============================================================================
// This is the very first file that runs when you execute `dotnet run`.
// It does two main jobs:
// 1. BUILDER: Registers services, database connections, and security (Dependency Injection).
// 2. APP PIPELINE: Configures HTTP middleware (CORS, Static Files, Auth, Routing).
// =============================================================================

// Fix for Npgsql (PostgreSQL driver) so local DateTime values work smoothly without UTC errors
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Create the WebApplication builder
var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// 1. CONTROLLERS
// Registers our API controllers (AuthController, BirthdaysController) so ASP.NET
// knows how to handle incoming HTTP requests like GET /api/birthdays.
// =============================================================================
builder.Services.AddControllers();

// =============================================================================
// 2. CORS (Cross-Origin Resource Sharing)
// WHY WE NEED THIS:
// Web browsers have a built-in security rule: website A (http://localhost:5173 - React)
// cannot talk to website B (http://localhost:5000 - C# Backend) unless website B
// explicitly says: "I trust website A!"
// This policy gives React permission to make API calls to this backend.
// =============================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// =============================================================================
// 3. DEPENDENCY INJECTION (DI) & STORAGE REPOSITORIES
// WHAT IS DEPENDENCY INJECTION?
// Instead of creating objects manually using `new PostgresBirthdayRepository()`,
// ASP.NET Core creates and passes them to our controllers automatically!
//
// Here we check if you provided a PostgreSQL connection string in appsettings.json.
// - If YES -> We use PostgreSQL with Entity Framework Core.
// - If NO  -> We use In-Memory lists (data resets when server restarts).
// This lets you run the app immediately even without PostgreSQL installed!
// =============================================================================
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
bool usePostgres = !string.IsNullOrWhiteSpace(connectionString) &&
                   !connectionString.Contains("YOUR_PASSWORD", StringComparison.OrdinalIgnoreCase);

if (usePostgres)
{
    // Connect to PostgreSQL database using Entity Framework Core
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Scoped: A new repository instance is created for each HTTP request
    builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
    builder.Services.AddScoped<IBirthdayRepository, PostgresBirthdayRepository>();
    builder.Services.AddScoped<IPasswordResetOtpRepository, PostgresPasswordResetOtpRepository>();
}
else
{
    // Singleton: A single shared repository instance stays in memory for the life of the server
    builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
    builder.Services.AddSingleton<IBirthdayRepository, InMemoryBirthdayRepository>();
    builder.Services.AddSingleton<IPasswordResetOtpRepository, InMemoryPasswordResetOtpRepository>();
}

// =============================================================================
// 4. APPLICATION BUSINESS SERVICES
// AuthService: Manages user registration, BCrypt password hashing, and login.
// BirthdayService: Calculates leap years, upcoming countdowns, and handles photo saving.
// EmailService: Sends security login alerts and birthday reminder notifications.
// OtpRateLimiter: Prevents spam by throttling password reset OTP requests.
// =============================================================================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BirthdayService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<OtpRateLimiter>();


// =============================================================================
// 5. BACKGROUND WORKER SERVICE
// Runs automatically every hour in the background to inspect upcoming birthdays
// and log reminder notifications.
// =============================================================================
builder.Services.AddHostedService<ReminderBackgroundService>();

// =============================================================================
// 6. JWT AUTHENTICATION & AUTHORIZATION
// WHAT IS A JWT (JSON Web Token)?
// When a user logs in, the backend signs a token using a secret key.
// The frontend stores this token and sends it with every request:
// "Authorization: Bearer <token>"
// The backend validates the signature to verify the user's identity without
// having to query the database on every single request.
// =============================================================================
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

// =============================================================================
// 7. SWAGGER / OPENAPI DOCUMENTATION
// Provides an interactive web page at http://localhost:5000/swagger where you
// can test every API endpoint directly in your browser!
// =============================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Birthday Reminder API",
        Version = "v1",
        Description = "ASP.NET Core Web API for Birthday Reminder web application"
    });

    // Add JWT Bearer input to Swagger UI so you can test protected endpoints!
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (do not include the word 'Bearer ')."
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

// Ensure the wwwroot folder exists before building the app
string webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(webRoot))
{
    Directory.CreateDirectory(webRoot);
}
builder.Environment.WebRootPath = webRoot;

// Build the application
var app = builder.Build();

// Automatically create PostgreSQL tables if connected
if (usePostgres)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await dbContext.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Successfully connected to PostgreSQL and ensured database tables exist.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to connect to PostgreSQL database. Please verify your connection string.");
    }
}

// Ensure wwwroot/uploads directory exists to store uploaded profile photos
string uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// =============================================================================
// 8. HTTP REQUEST PIPELINE (MIDDLEWARE)
// The ORDER of these lines is very important!
// Every incoming HTTP request flows down through this pipeline step by step:
// 1. Exception handling catches any unexpected errors.
// 2. Swagger UI displays the interactive documentation.
// 3. StaticFiles serves uploaded images from wwwroot/uploads.
// 4. CORS permits React to make calls.
// 5. Authentication reads and verifies the JWT token.
// 6. Authorization checks if the user has permission to access the endpoint.
// 7. MapControllers routes the request to the matching Controller method!
// =============================================================================

// Global error handler: catches unhandled server crashes and returns a friendly JSON response
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

// Enable Swagger testing page in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Birthday Reminder API v1");
    });
}

// Apply CORS policy for the React app (MUST be before UseStaticFiles so image requests include CORS headers)
app.UseCors("AllowReactApp");

// Serve uploaded profile photos (e.g. http://localhost:5000/uploads/guid.jpg)
app.UseStaticFiles();

// Authentication verifies WHO you are (checks JWT token)
app.UseAuthentication();

// Authorization verifies WHAT you are allowed to do (checks [Authorize] rules)
app.UseAuthorization();

// Connect URL routes to controller actions
app.MapControllers();

// Start listening for HTTP requests!
app.Run();

