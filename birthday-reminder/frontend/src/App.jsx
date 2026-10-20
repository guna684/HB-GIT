import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import Navbar from './components/Navbar';
import ProtectedRoute from './components/ProtectedRoute';

import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import DashboardPage from './pages/DashboardPage';
import AddBirthdayPage from './pages/AddBirthdayPage';
import EditBirthdayPage from './pages/EditBirthdayPage';
import ChangePasswordPage from './pages/ChangePasswordPage';

/**
 * =============================================================================
 * ROOT COMPONENT: App
 * =============================================================================
 * This is the main entry point for our React user interface.
 * 
 * Hierarchy:
 * 1. <AuthProvider>: Wraps the whole app so any page can access login/theme state.
 * 2. <BrowserRouter>: Manages browser URLs and back/forward navigation.
 * 3. <Navbar>: Displayed permanently at the top.
 * 4. <Routes>: Defines all URL paths:
 *    - Public pages: /login, /register, /forgot-password (anyone can visit)
 *    - Protected pages: /, /add, /edit/:id, /change-password (must be logged in)
 *    - Fallback: Any unknown URL redirects to the home page (/).
 */
function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <div className="app-container">
          {/* Top navigation bar */}
          <Navbar />

          {/* Main page content area */}
          <main className="main-content">
            <Routes>
              {/* 1. Public Routes */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />

              {/* 2. Protected Routes (Wrapped in ProtectedRoute guard) */}
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <DashboardPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/add"
                element={
                  <ProtectedRoute>
                    <AddBirthdayPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/edit/:id"
                element={
                  <ProtectedRoute>
                    <EditBirthdayPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/change-password"
                element={
                  <ProtectedRoute>
                    <ChangePasswordPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/add"
                element={
                  <ProtectedRoute>
                    <AddBirthdayPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/edit/:id"
                element={
                  <ProtectedRoute>
                    <EditBirthdayPage />
                  </ProtectedRoute>
                }
              />

              {/* 3. Fallback Route: Redirects any unknown route to "/" */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>
        </div>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;

