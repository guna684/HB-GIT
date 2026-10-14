import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

/**
 * Top navigation bar containing the application title, theme toggle,
 * and user logout action.
 */
const Navbar = () => {
  const { isAuthenticated, user, logout, theme, toggleTheme } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="navbar">
      <div className="navbar-inner">
        <Link to="/" className="nav-brand">
          <span className="nav-brand-icon">🎂</span>
          <span className="nav-brand-text">Birthday Reminder</span>
        </Link>

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
