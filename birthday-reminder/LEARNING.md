# Learning Guide: Birthday Reminder Application

Welcome! This guide is written specially for you as a beginner in C# and ASP.NET Core. It explains how this application is structured, how data flows through the system, and why each part exists.

---

## 1. What This Project Does & Why We Need a Backend

### What the Application Does
The **Birthday Reminder** application lets users create accounts, store birthdays of friends, family, and coworkers, upload photos, calculate countdowns and upcoming ages (including leap-year handling), and automatically logs hourly reminder notifications when someone's birthday is near.

### Why We Need a Separate Backend
You might wonder: *“Why not save birthdays directly in the React frontend using localStorage?”*

1. **Multi-Device Sync**: If you add a birthday on your laptop, you want to see it when you open the app on your phone. Local browser storage cannot share data across devices.
2. **Security & Privacy**: User passwords and personal data must be stored safely. The backend hashes passwords with BCrypt so even if someone steals the database, they cannot read the passwords.
3. **Automated Reminders in the Background**: When your browser tab is closed or your phone is asleep, the frontend cannot run. The backend runs 24/7 on a server, allowing background tasks (like `ReminderBackgroundService`) to inspect upcoming dates and send alerts at any time.
4. **Mobile App Ready**: Because the backend is a REST API that exchanges pure JSON with JWT tokens, a future Android or iOS app can connect to the exact same backend without changing a single line of backend code.

---

## 2. Request Flow: Adding a Birthday (Step-by-Step)

When a user fills out the "Add Birthday" form and clicks **Save Birthday**, here is the journey that request takes:

```
[ Browser / User ]
       │
       ▼
 1. AddBirthdayPage.jsx ──(collects form fields & photo into FormData)
       │
       ▼
 2. birthdayApi.js ──────(calls createBirthday(formData))
       │
       ▼
 3. axiosClient.js ──────(attaches 'Authorization: Bearer <JWT>' header)
       │
       ▼ HTTP POST http://localhost:5000/api/birthdays
 4. BirthdaysController.cs [Authorize]
       │  • Checks JWT validity
       │  • Extracts logged-in userId
       │  • Calls BirthdayService.CreateForUserAsync(...)
       ▼
 5. BirthdayService.cs
       │  • Validates image extension (.jpg, .png, etc.) and size (<= 2MB)
       │  • Saves photo to wwwroot/uploads/guid.jpg
       │  • Creates Birthday model with UserId
       │  • Calls IBirthdayRepository.CreateAsync(...)
       ▼
 6. InMemoryBirthdayRepository.cs
       │  • Assigns unique ID (1, 2, 3...)
       │  • Adds Birthday object to in-memory List
       │  • Returns saved entity
       ▼
 (Returns back through Service -> Controller maps to BirthdayResponse with full photoUrl)
       │
       ▼ HTTP 201 Created (JSON Response)
 7. React receives response, updates state, and navigates back to Dashboard!
```

---

## 3. Project Structure: What Each Folder & File Does

### Backend (`birthday-reminder/backend/`)
- `Program.cs`: The starting point of the backend; registers services, configures JWT, CORS, Static Files, Swagger, and request pipeline.
- `appsettings.json`: Stores configuration settings (JWT secret key, issuer, token expiry).
- `appsettings.Development.json`: Overrides configuration settings for local development.
- `backend.csproj`: The project configuration file listing the .NET SDK target (`net8.0`) and NuGet package references.
- `Models/User.cs`: Defines the user entity (`Id`, `Name`, `Email`, `PasswordHash`).
- `Models/Birthday.cs`: Defines the birthday record (`Id`, `UserId`, `PersonName`, `DateOfBirth`, `Relationship`, `Notes`, `PhotoFileName`, `RemindDaysBefore`).
- `DTOs/RegisterRequest.cs`: Data Transfer Object containing validated inputs for user registration.
- `DTOs/LoginRequest.cs`: Data Transfer Object containing email and password for login.
- `DTOs/AuthResponse.cs`: Data Transfer Object returned upon login containing the JWT token and user details.
- `DTOs/BirthdayRequest.cs`: Data Transfer Object for creating or updating a birthday with optional photo upload.
- `DTOs/BirthdayResponse.cs`: Data Transfer Object sent back to the frontend with computed `daysUntilBirthday`, `turningAge`, and full `photoUrl`.
- `Repositories/IUserRepository.cs`: Interface defining methods to query and save users.
- `Repositories/IBirthdayRepository.cs`: Interface defining methods for birthday data operations.
- `Repositories/InMemoryUserRepository.cs`: In-memory implementation of `IUserRepository` using a C# `List<User>`.
- `Repositories/InMemoryBirthdayRepository.cs`: In-memory implementation of `IBirthdayRepository` using a C# `List<Birthday>`.
- `Services/AuthService.cs`: Business logic for registration, BCrypt password hashing/verification, and JWT generation.
- `Services/BirthdayService.cs`: Business logic for age calculations, leap year handling, photo uploads, and user isolation.
- `Services/ReminderBackgroundService.cs`: Background worker running every hour to check upcoming birthdays and log reminder messages.
- `Controllers/AuthController.cs`: Exposes `POST /api/auth/register` and `POST /api/auth/login`.
- `Controllers/BirthdaysController.cs`: Exposes secured endpoints (`GET`, `POST`, `PUT`, `DELETE` on `/api/birthdays`).
- `wwwroot/uploads/`: Physical folder where uploaded profile pictures are stored and served publicly.

### Frontend (`birthday-reminder/frontend/`)
- `index.html`: Main HTML template containing Google Font "Inter" and the root `<div>`.
- `vite.config.js`: Configuration for the Vite development and build tool.
- `.env`: Environment file containing `VITE_API_URL=http://localhost:5000/api`.
- `package.json`: Project manifest listing dependencies (`react`, `react-router-dom`, `axios`).
- `src/main.jsx`: React entry point that mounts the application to the DOM.
- `src/App.jsx`: Top-level component defining routes (`/`, `/login`, `/register`, `/add`, `/edit/:id`).
- `src/styles/global.css`: CSS styling with custom variables, light/dark themes, responsive layout, and cards.
- `src/context/AuthContext.jsx`: React Context managing user authentication state, token storage, and theme toggling.
- `src/api/axiosClient.js`: Configured Axios instance with request/response interceptors for JWT tokens and 401 handling.
- `src/api/authApi.js`: Frontend functions to call registration and login API endpoints.
- `src/api/birthdayApi.js`: Frontend functions to perform CRUD operations on birthdays.
- `src/components/Navbar.jsx`: Header bar with app branding, dashboard link, dark-mode switch, and logout button.
- `src/components/BirthdayCard.jsx`: Card showing avatar photo/initials, name, date, relationship, turning age, and countdown badge.
- `src/components/BirthdayForm.jsx`: Form with live photo preview and validation for adding or editing birthdays.
- `src/components/ProtectedRoute.jsx`: Wrapper that redirects unauthenticated visitors to the login page.
- `src/components/EmptyState.jsx`: Cheerful placeholder displayed when no birthdays match or exist.
- `src/pages/LoginPage.jsx`: Centered login card with email and password inputs.
- `src/pages/RegisterPage.jsx`: Registration card for creating a new account.
- `src/pages/DashboardPage.jsx`: Main dashboard with greeting, today's celebration banner, upcoming cards, search, and category filter.
- `src/pages/AddBirthdayPage.jsx`: Page hosting the form to create a new birthday.
- `src/pages/EditBirthdayPage.jsx`: Page hosting the form to edit an existing birthday.

---

## 4. How Login and JWT Work, Step by Step

JWT stands for **JSON Web Token**. It is a secure, signed string that proves who the user is.

```
Step 1: User types email & password in LoginPage.jsx and clicks "Sign In".
Step 2: Frontend sends POST /api/auth/login with { email, password }.
Step 3: AuthService in backend looks up user by email.
Step 4: AuthService uses BCrypt.Verify(password, storedHash) to check password.
Step 5: If correct, backend creates a JWT token containing:
        - Header (algorithm: HMAC-SHA256)
        - Payload/Claims (UserId: 1, Name: "Ananya", Email: "ananya@example.com", Expiration: 7 days)
        - Signature (computed using the Secret Key from appsettings.json)
Step 6: Backend returns the JWT string in the JSON response.
Step 7: Frontend stores token in localStorage ('token').
Step 8: On every future API request, axiosClient interceptor adds:
        Authorization: Bearer eyJhbGciOiJIUzI1Ni...
Step 9: ASP.NET Core JwtBearer middleware reads the header, validates the signature
        with the secret key, and automatically populates HttpContext.User.
Step 10: If token expires or is invalid, backend returns 401 Unauthorized,
         and the frontend automatically redirects the user back to the login page.
```

---

## 5. What CORS Is & Why It Is in Program.cs

**CORS** stands for **Cross-Origin Resource Sharing**.

By default, web browsers enforce a security rule called the **Same-Origin Policy**:
- Your frontend runs on `http://localhost:5173`.
- Your backend runs on `http://localhost:5000`.
- Because the ports are different (`5173` vs `5000`), the browser considers them different "origins".
- Without CORS configured on the backend, the browser will block the frontend from reading responses.

In `Program.cs`, we explicitly allow our React frontend:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Allow only our React frontend
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```
We intentionally avoid `AllowAnyOrigin()` to prevent unknown third-party websites from making authenticated requests on behalf of our users.

---

## 6. How Dependency Injection (DI) Works & Swapping to PostgreSQL

In ASP.NET Core, classes do not create their own dependencies using `new`. Instead, the system injects them through their constructors.

In `Program.cs`:
```csharp
// Currently using In-Memory repositories (registered as Singleton):
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IBirthdayRepository, InMemoryBirthdayRepository>();
```

Because `AuthService` and `BirthdayService` only know about `IUserRepository` and `IBirthdayRepository` (the interfaces), they have **zero dependency** on the in-memory lists!

### How You Will Switch to PostgreSQL Later:
1. Install `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Create an EF Core `AppDbContext`.
3. Create `PostgresUserRepository` and `PostgresBirthdayRepository` implementing `IUserRepository` and `IBirthdayRepository`.
4. Replace the lines in `Program.cs`:
   ```csharp
   builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

   builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
   builder.Services.AddScoped<IBirthdayRepository, PostgresBirthdayRepository>();
   ```
None of your controllers or services need to change!

---

## 7. How the Reminder BackgroundService Works

`ReminderBackgroundService` inherits from ASP.NET Core's built-in `BackgroundService`.

- When the backend starts, ASP.NET Core automatically starts `ExecuteAsync()`.
- It executes a loop while the application is alive:
  ```csharp
  while (!stoppingToken.IsCancellationRequested)
  {
      await CheckBirthdaysAsync();
      await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Pauses for 1 hour
  }
  ```
- In `CheckBirthdaysAsync()`, it creates a scope (`using var scope = _serviceProvider.CreateScope()`), resolves `IBirthdayRepository`, and inspects all birthdays.
- It calculates `daysUntil` for each birthday using `BirthdayService.CalculateBirthdayStats`.
- If `daysUntil <= birthday.RemindDaysBefore`, it logs an alert with `ILogger`:
  `Reminder: Ravi's birthday is tomorrow (turning 25)`.
- This is where an email notification service (`IEmailService`) can be plugged in later to send automated emails.

---

## 8. Where to Read the Code First (Recommended Order)

To understand this codebase step-by-step, read these 10 files in this exact order:

1. `backend/Models/User.cs` & `backend/Models/Birthday.cs` — The core entities and properties.
2. `backend/Repositories/IUserRepository.cs` & `backend/Repositories/IBirthdayRepository.cs` — The data access contracts.
3. `backend/Repositories/InMemoryBirthdayRepository.cs` — How data is stored in memory using plain C# loops.
4. `backend/DTOs/BirthdayResponse.cs` — The data shape sent to the client.
5. `backend/Services/BirthdayService.cs` — How countdowns, leap years, and photos are handled.
6. `backend/Controllers/BirthdaysController.cs` — How HTTP requests are mapped to service calls.
7. `backend/Program.cs` — How everything is wired together with DI, CORS, and JWT.
8. `frontend/src/api/axiosClient.js` — How Axios sends the JWT token automatically.
9. `frontend/src/context/AuthContext.jsx` — How React manages login status and theme.
10. `frontend/src/pages/DashboardPage.jsx` — How birthdays, search, filters, and today's banner are rendered.

---

## 9. Ten Questions to Test Your Understanding

1. Why do we store passwords using BCrypt instead of plain text or MD5?
2. What is the difference between a `Model` (like `Birthday`) and a `DTO` (like `BirthdayResponse`)?
3. How does the backend know which user is making a request to `/api/birthdays`?
4. What happens when a user born on February 29 has a birthday in a non-leap year?
5. Why are the in-memory repositories registered as `AddSingleton` instead of `AddScoped`?
6. What HTTP status code does the backend return when a token is missing or expired, and what does the frontend do in response?
7. Why do we use `multipart/form-data` instead of `application/json` when creating a birthday with a photo?
8. Where are uploaded photo files stored, and how does the browser access them?
9. What would happen in the browser if we did not configure CORS in `Program.cs`?
10. Why does `ReminderBackgroundService` use `_serviceProvider.CreateScope()` to get the repository instead of injecting it directly in the constructor?

---

---

## 10. PostgreSQL Database Setup & Connection String

We have fully integrated PostgreSQL using **Entity Framework Core** and **Npgsql**!

### How It Works:
- **`AppDbContext.cs`**: Maps `User` and `Birthday` models to PostgreSQL tables, adds foreign keys, unique email constraints, and clean timestamp mapping.
- **`PostgresUserRepository.cs` & `PostgresBirthdayRepository.cs`**: Implement `IUserRepository` and `IBirthdayRepository` via EF Core.
- **Smart Fallback**: If `DefaultConnection` in `appsettings.json` is left empty, the app automatically runs in In-Memory mode without crashing.
- **Auto Table Creation**: When a valid PostgreSQL connection string is supplied, the app automatically executes `EnsureCreatedAsync()` on startup to create the `Users` and `Birthdays` tables with indexes and foreign keys!

### How to Connect Your PostgreSQL Database:
1. Open [`backend/appsettings.json`](file:///e:/d/HB/birthday-reminder/backend/appsettings.json).
2. Set your PostgreSQL connection string in `ConnectionStrings:DefaultConnection`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=birthday_db;Username=postgres;Password=your_password"
   }
   ```
   *(Works with local PostgreSQL, Neon.tech, Supabase, Railway, ElephantSQL, etc.)*
3. Restart the backend (`dotnet run`). The tables will be created automatically!

---

## 11. Next Steps for Growth

1. **Email Reminders via SendGrid or MailKit**:
   - Create an `IEmailService`.
   - Send email notifications inside `ReminderBackgroundService` when a birthday is coming up.
2. **Deployment**:
   - Deploy backend to Render, Railway, or Azure App Service with PostgreSQL.
   - Deploy frontend to Vercel or Netlify.
3. **Mobile App or Progressive Web App (PWA)**:
   - Convert the React app into an installable PWA or build an Android app with React Native / Kotlin!

