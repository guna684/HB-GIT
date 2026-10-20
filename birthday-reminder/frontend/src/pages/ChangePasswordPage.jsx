import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { changePassword } from '../api/authApi';
import { useAuth } from '../context/AuthContext';

/**
 * =============================================================================
 * PAGE: ChangePasswordPage
 * =============================================================================
 * Allows authenticated users to change their account password.
 * 
 * Features:
 * - Current password verification.
 * - New password complexity check (min 8 chars, letter & number).
 * - Confirm password match check.
 * - Show / hide password visibility toggles.
 * - Clear success / error feedback messages.
 */
const ChangePasswordPage = () => {
  const { user } = useAuth();

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (!currentPassword) {
      setError('Please enter your current password.');
      return;
    }

    if (!newPassword) {
      setError('Please enter your new password.');
      return;
    }

    if (newPassword.length < 8) {
      setError('New password must be at least 8 characters long.');
      return;
    }

    const hasLetter = /[a-zA-Z]/.test(newPassword);
    const hasDigit = /[0-9]/.test(newPassword);
    if (!hasLetter || !hasDigit) {
      setError('New password must contain both letters and numbers.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setError('New passwords do not match. Please re-enter them.');
      return;
    }

    setLoading(true);

    try {
      const response = await changePassword({
        currentPassword,
        newPassword,
      });

      setSuccess(response.message || 'Password updated successfully! An email confirmation has been sent.');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err) {
      const message = err.response?.data?.message || 'Failed to update password. Please verify your current password.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card" style={{ maxWidth: '440px' }}>
        <div className="auth-header">
          <div style={{ fontSize: '2.5rem', marginBottom: '8px' }}>🛡️</div>
          <h1>Change Password</h1>
          <p>Update your account password for {user?.email || 'your account'}</p>
        </div>

        {error && <div className="alert alert-danger">{error}</div>}
        {success && <div className="alert alert-success">{success}</div>}

        <form onSubmit={handleSubmit}>
          {/* Current Password */}
          <div className="form-group">
            <label className="form-label" htmlFor="current-password">
              Current Password
            </label>
            <div style={{ position: 'relative' }}>
              <input
                id="current-password"
                type={showCurrent ? 'text' : 'password'}
                className="form-input"
                placeholder="Enter current password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                style={{ paddingRight: '44px' }}
                required
                autoFocus
              />
              <button
                type="button"
                onClick={() => setShowCurrent((prev) => !prev)}
                aria-label={showCurrent ? 'Hide password' : 'Show password'}
                style={{
                  position: 'absolute',
                  right: '10px',
                  top: '50%',
                  transform: 'translateY(-50%)',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  fontSize: '1.1rem',
                  color: 'var(--text-secondary)',
                  padding: '4px',
                }}
              >
                {showCurrent ? '🙈' : '👁️'}
              </button>
            </div>
          </div>

          {/* New Password */}
          <div className="form-group">
            <label className="form-label" htmlFor="new-password">
              New Password
            </label>
            <div style={{ position: 'relative' }}>
              <input
                id="new-password"
                type={showNew ? 'text' : 'password'}
                className="form-input"
                placeholder="At least 8 chars (letters & numbers)"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                style={{ paddingRight: '44px' }}
                required
              />
              <button
                type="button"
                onClick={() => setShowNew((prev) => !prev)}
                aria-label={showNew ? 'Hide password' : 'Show password'}
                style={{
                  position: 'absolute',
                  right: '10px',
                  top: '50%',
                  transform: 'translateY(-50%)',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  fontSize: '1.1rem',
                  color: 'var(--text-secondary)',
                  padding: '4px',
                }}
              >
                {showNew ? '🙈' : '👁️'}
              </button>
            </div>
          </div>

          {/* Confirm New Password */}
          <div className="form-group">
            <label className="form-label" htmlFor="confirm-new-password">
              Confirm New Password
            </label>
            <div style={{ position: 'relative' }}>
              <input
                id="confirm-new-password"
                type={showConfirm ? 'text' : 'password'}
                className="form-input"
                placeholder="Re-enter your new password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                style={{ paddingRight: '44px' }}
                required
              />
              <button
                type="button"
                onClick={() => setShowConfirm((prev) => !prev)}
                aria-label={showConfirm ? 'Hide password' : 'Show password'}
                style={{
                  position: 'absolute',
                  right: '10px',
                  top: '50%',
                  transform: 'translateY(-50%)',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  fontSize: '1.1rem',
                  color: 'var(--text-secondary)',
                  padding: '4px',
                }}
              >
                {showConfirm ? '🙈' : '👁️'}
              </button>
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary"
            style={{ width: '100%', marginTop: '8px' }}
            disabled={loading}
          >
            {loading ? 'Updating Password...' : 'Update Password'}
          </button>

          <div style={{ textAlign: 'center', marginTop: '20px', fontSize: '0.9rem' }}>
            <Link to="/" style={{ color: 'var(--color-primary)', fontWeight: '600', textDecoration: 'none' }}>
              &larr; Back to Dashboard
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ChangePasswordPage;
