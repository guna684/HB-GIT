# 🎂 Birthday Reminder Web Application

A full-stack Birthday Reminder web application built with **ASP.NET Core Web API (.NET 8, C#)** and **React (Vite, JavaScript)**.

---

## 🌟 Key Features

- **Authentication & Security**: User registration and login using BCrypt password hashing and JWT (JSON Web Tokens).
- **Data Isolation**: Each user can only view, edit, and delete their own birthday records.
- **Smart Date & Leap Year Calculations**: Automatically calculates days until next birthday and turning age, with correct handling for leap-year babies born on February 29.
- **Photo Uploads**: Upload profile pictures (.jpg, .jpeg, .png, .webp up to 2 MB) saved to `wwwroot/uploads` with live preview.
- **Background Reminders**: A hosted `BackgroundService` runs every hour and logs alerts for upcoming birthdays.
- **Clean Architecture**: Controller -> Service -> Repository layer pattern with In-Memory storage swappable to PostgreSQL.
- **Modern Responsive UI**: Mobile-first design with light/dark theme toggle, search, relationship filtering, and delete confirmation modal.

---

## 🛠️ Prerequisites

Make sure you have the following installed on your machine:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer)
- [Node.js](https://nodejs.org/) (v18 or newer)
- npm (installed automatically with Node.js)

---

## 🚀 How to Run the Project

The frontend and backend run as two separate projects on different ports.

### Step 1: Run the Backend (ASP.NET Core API)

Open a terminal and run:

```bash
cd birthday-reminder/backend
dotnet run
```

- **Backend API URL**: `http://localhost:5000`
- **Swagger Documentation & Testing UI**: `http://localhost:5000/swagger`

> **Note**: In Swagger, click the green **Authorize** button and paste your JWT token (without the word `Bearer`) to test secured endpoints!

---

### Step 2: Run the Frontend (React + Vite)

Open a **new** terminal window and run:

```bash
cd birthday-reminder/frontend
npm install
npm run dev
```

- **Frontend Web App URL**: `http://localhost:5173`

---

## 📁 Project Structure

```
birthday-reminder/
├── backend/                  # ASP.NET Core Web API (.NET 8)
│   ├── Controllers/          # AuthController, BirthdaysController
│   ├── DTOs/                 # Request & Response Data Transfer Objects
│   ├── Models/               # User and Birthday entity models
│   ├── Repositories/         # Interfaces and In-Memory implementations
│   ├── Services/             # AuthService, BirthdayService, ReminderBackgroundService
│   ├── wwwroot/uploads/      # Stored profile photos
│   ├── appsettings.json      # JWT configuration & settings
│   └── Program.cs            # Dependency Injection, Middleware, CORS, JWT
├── frontend/                 # React Application (Vite)
│   ├── src/
│   │   ├── api/              # Axios client and API calls (authApi, birthdayApi)
│   │   ├── components/       # Navbar, BirthdayCard, BirthdayForm, ProtectedRoute, EmptyState
│   │   ├── context/          # AuthContext (JWT, user state, light/dark mode)
│   │   ├── pages/            # LoginPage, RegisterPage, DashboardPage, AddBirthdayPage, EditBirthdayPage
│   │   └── styles/           # global.css (Design system, CSS variables)
│   └── vite.config.js        # Vite dev server configuration (port 5173)
├── LEARNING.md               # Beginner guide explaining architecture, JWT, CORS & DI
└── README.md                 # Project overview and run instructions
```

---

## 🧪 Testing the Complete Flow

1. Open `http://localhost:5173` in your browser.
2. Click **Sign up** and register a new user account.
3. You will be greeted on the **Dashboard** (`Hi YourName 👋`).
4. Try toggling the **☀️ / 🌙 theme button** in the navbar to switch between dark and light modes.
5. Click **+ Add Birthday** to add a friend or family member:
   - Provide their name and birth date.
   - Choose their relationship (Family, Friend, Work, Other).
   - Upload a profile picture (optional) to see the live preview.
   - Set reminder advance days and click **Save Birthday**.
6. On the Dashboard:
   - Notice the **Upcoming Birthdays** section and countdown badge (`Today! 🎉`, `Tomorrow`, `In X days`).
   - Try searching by name or filtering by category.
   - Click the ✏️ icon to edit details or 🗑️ to delete with confirmation.
