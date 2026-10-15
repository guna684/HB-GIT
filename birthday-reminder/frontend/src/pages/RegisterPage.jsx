import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { registerUser } from '../api/authApi';
import { useAuth } from '../context/AuthContext';

/**
 * =============================================================================
 * PAGE: RegisterPage
 * =============================================================================
 * Allows new users to create an account with their Name, Email, and Password.
 * 
 * How it works:
 * 1. Collects name, email, and password.
 * 2. Validates that the password is at least 6 characters.
 * 3. Sends POST /api/auth/register.
 * 4. Backend creates the user record, securely hashes the password with BCrypt,
 *    and returns a JWT token.
 * 5. Frontend logs the user in immediately and navigates to the Dashboard!
 */
const RegisterPage = () => {
  // Form input states
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  // ---------------------------------------------------------------------------
  // Handle form submission
  // ---------------------------------------------------------------------------
  const handleSubmit = async (event) => {
    event.preventDefault();
    setError('');

    // Rule 1: Name required
    if (!name.trim()) {
      setError('Please enter your name.');
      return;
    }

    // Rule 2: Email required
    if (!email.trim()) {
      setError('Please enter your email.');
      return;
    }

    // Rule 3: Password minimum length
    if (password.length < 6) {
      setError('Password must be at least 6 characters long.');
      return;
    }

    setLoading(true);

    try {
      // Send registration request
      const data = await registerUser({
        name: name.trim(),
        email: email.trim(),
        password,
      });

      // Automatically log the user in with the returned JWT token
      login(data);

      // Redirect to dashboard
      navigate('/');
    } catch (err) {
      const message = err.response?.data?.message || 'Registration failed. Please try again.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  // ---------------------------------------------------------------------------
  // Render Registration Card UI
  // ---------------------------------------------------------------------------
  return (
    <div className="auth-page">
      <div className="auth-card">
        {/* Header Icon & Title */}
        <div className="auth-header">
          <div style={{ fontSize: '2.5rem', marginBottom: '8px' }}>🎉</div>
          <h1>Create an Account</h1>
          <p>Start tracking important birthdays today</p>
        </div>

        {/* Error alert */}
        {error && <div className="alert alert-danger">{error}</div>}

        {/* Registration Form */}
        <form onSubmit={handleSubmit}>
          {/* Full Name input */}
          <div className="form-group">
            <label className="form-label" htmlFor="name">
              Your Full Name
            </label>
            <input
              id="name"
              type="text"
              className="form-input"
              placeholder="e.g. Ravi Kumar"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
            />
          </div>

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
            <label className="form-label" htmlFor="password">
              Password (min. 6 characters)
            </label>
            <input
              id="password"
              type="password"
              className="form-input"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              minLength={6}
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
            {loading ? 'Creating Account...' : 'Sign Up'}
          </button>
        </form>

        {/* Link back to Login page */}
        <div style={{ textAlign: 'center', marginTop: '20px', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
          Already have an account?{' '}
          <Link to="/login" style={{ color: 'var(--color-primary)', fontWeight: '600', textDecoration: 'none' }}>
            Log in
          </Link>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
