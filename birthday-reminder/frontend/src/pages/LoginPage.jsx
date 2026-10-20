import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { loginUser } from '../api/authApi';
import { useAuth } from '../context/AuthContext';

/**
 * =============================================================================
 * PAGE: LoginPage
 * =============================================================================
 * Allows registered users to log into their account.
 * 
 * How it works:
 * 1. Collects email and password via controlled <input> elements.
 * 2. Calls loginUser({ email, password }) which hits POST /api/auth/login.
 * 3. The backend checks BCrypt password hash and returns a JWT token.
 * 4. Calls login(data) from our AuthContext to save token to localStorage.
 * 5. Uses navigate('/') to redirect the user to the Dashboard!
 */
const LoginPage = () => {
  // Form input states
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // Auth context login helper and React Router navigation
  const { login } = useAuth();
  const navigate = useNavigate();

  // ---------------------------------------------------------------------------
  // Handle form submission
  // ---------------------------------------------------------------------------
  const handleSubmit = async (event) => {
    event.preventDefault();
    setError('');

    if (!email || !password) {
      setError('Please fill in all fields.');
      return;
    }

    setLoading(true);

    try {
      // Send credentials to backend
      const data = await loginUser({ email, password });

      // Save token & user to context + localStorage
      login(data);

      // Redirect to dashboard
      navigate('/');
    } catch (err) {
      const message = err.response?.data?.message || 'Invalid email or password. Please try again.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  // ---------------------------------------------------------------------------
  // Render Login Card UI
  // ---------------------------------------------------------------------------
  return (
    <div className="auth-page">
      <div className="auth-card">
        {/* Header Icon & Title */}
        <div className="auth-header">
          <div style={{ fontSize: '2.5rem', marginBottom: '8px' }}>🎂</div>
          <h1>Welcome Back</h1>
          <p>Sign in to see your birthday reminders</p>
        </div>

        {/* Error message if login failed */}
        {error && <div className="alert alert-danger">{error}</div>}

        {/* Login Form */}
        <form onSubmit={handleSubmit}>
          {/* Email input */}
          <div className="form-group">
            <label className="form-label" htmlFor="email">
              Email Address
            </label>
            <input
              id="email"
              type="email"
              className="form-input"
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          {/* Password input */}
          <div className="form-group">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
              <label className="form-label" htmlFor="password" style={{ marginBottom: 0 }}>
                Password
              </label>
              <Link
                to="/forgot-password"
                style={{
                  fontSize: '0.85rem',
                  color: 'var(--color-primary)',
                  textDecoration: 'none',
                  fontWeight: '500',
                }}
              >
                Forgot password?
              </Link>
            </div>
            <input
              id="password"
              type="password"
              className="form-input"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>

          {/* Submit button */}
          <button
            type="submit"
            className="btn btn-primary"
            style={{ width: '100%', marginTop: '8px' }}
            disabled={loading}
          >
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        {/* Link to Registration page */}
        <div style={{ textAlign: 'center', marginTop: '20px', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
          Don't have an account?{' '}
          <Link to="/register" style={{ color: 'var(--color-primary)', fontWeight: '600', textDecoration: 'none' }}>
            Sign up
          </Link>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;

