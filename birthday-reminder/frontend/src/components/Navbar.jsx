import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

/**
 * =============================================================================
 * COMPONENT: Navbar
 * =============================================================================
 * The top navigation header shown on every page of the app.
 * 
 * Features:
 * 1. Brand logo and title (links back to Dashboard).
 * 2. Light / Dark theme toggle button (persists in localStorage).
 * 3. Conditional links:
 *    - If user is logged in: shows "Dashboard" link and "Log Out" button.
 *    - If user is NOT logged in: shows "Log In" link and "Sign Up" button.
 */
const Navbar = () => {
  // Grab state & functions from our global AuthContext
  const { isAuthenticated, logout, theme, toggleTheme } = useAuth();
  const navigate = useNavigate();

  // Handle logging out and redirecting to the login page
  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="navbar">
      <div className="navbar-inner">
        {/* Brand Link */}
        <Link to="/" className="nav-brand">
          <span className="nav-brand-icon">🎂</span>
          <span className="nav-brand-text">Birthday Reminder</span>
        </Link>

        {/* Right Actions: Theme Toggle & Navigation Links */}
        <div className="nav-actions">
          {/* Light / Dark Mode Toggle Button */}
          <button
            type="button"
            className="btn btn-icon"
            onClick={toggleTheme}
            title={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
            aria-label="Toggle dark mode"
          >
            {theme === 'dark' ? '☀️' : '🌙'}
          </button>

          {/* Conditional auth links */}
          {isAuthenticated ? (
            <>
              <Link to="/" className="nav-link">
                Dashboard
              </Link>
              <button
                type="button"
                className="btn btn-outline"
                onClick={handleLogout}
                style={{ padding: '6px 14px', minHeight: '36px' }}
              >
                Log Out
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="nav-link">
                Log In
              </Link>
              <Link to="/register" className="btn btn-primary" style={{ padding: '6px 14px', minHeight: '36px' }}>
                Sign Up
              </Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
};

export default Navbar;

