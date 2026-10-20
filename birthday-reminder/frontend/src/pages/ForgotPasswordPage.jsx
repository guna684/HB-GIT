import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { forgotPassword, resetPassword } from '../api/authApi';

/**
 * =============================================================================
 * PAGE: ForgotPasswordPage
 * =============================================================================
 * Provides a 2-step password recovery workflow:
 * Step 1: User enters their email -> API sends a 6-digit OTP code.
 * Step 2: User enters the 6-digit OTP + new password + confirmation.
 * 
 * Features:
 * - 60-second resend countdown timer.
 * - Password show/hide toggle buttons.
 * - Password complexity validation before submitting.
 * - Friendly success / error messages.
 * - Automatically redirects to /login on success.
 */
const ForgotPasswordPage = () => {
  const navigate = useNavigate();

  // Wizard Step: 1 = Enter Email, 2 = Enter OTP and New Password
  const [step, setStep] = useState(1);

  // Form Fields
  const [email, setEmail] = useState('');
  const [otp, setOtp] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  // Password Visibility Toggles
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  // Feedback states
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  // 60-second Resend Timer
  const [countdown, setCountdown] = useState(0);

  // Countdown timer effect
  useEffect(() => {
    let timer;
    if (countdown > 0) {
      timer = setInterval(() => {
        setCountdown((prev) => prev - 1);
      }, 1000);
    }
    return () => clearInterval(timer);
  }, [countdown]);

  // ---------------------------------------------------------------------------
  // STEP 1: Request OTP for Email
  // ---------------------------------------------------------------------------
  const handleRequestOtp = async (e) => {
    if (e) e.preventDefault();
    setError('');
    setSuccess('');

    if (!email.trim()) {
      setError('Please enter your email address.');
      return;
    }

    setLoading(true);

    try {
      const response = await forgotPassword(email.trim());
      setSuccess(response.message || 'If this email is registered, an OTP has been sent.');
      setStep(2);
      setCountdown(60); // Start 60-second timer
    } catch (err) {
      const message = err.response?.data?.message || 'Failed to send verification code. Please try again.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  // ---------------------------------------------------------------------------
  // STEP 2: Verify OTP and Reset Password
  // ---------------------------------------------------------------------------
  const handleResetPassword = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (!otp.trim()) {
      setError('Please enter the 6-digit verification code.');
      return;
    }

    if (otp.trim().length !== 6) {
      setError('Verification code must be exactly 6 digits.');
      return;
    }

    if (!newPassword) {
      setError('Please enter a new password.');
      return;
    }

    // Complexity validation: min 8 chars, at least one letter and one number
    if (newPassword.length < 8) {
      setError('Password must be at least 8 characters long.');
      return;
    }

    const hasLetter = /[a-zA-Z]/.test(newPassword);
    const hasDigit = /[0-9]/.test(newPassword);
    if (!hasLetter || !hasDigit) {
      setError('Password must contain both letters and numbers.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match. Please re-type them.');
      return;
    }

    setLoading(true);

    try {
      const response = await resetPassword({
        email: email.trim(),
        otp: otp.trim(),
        newPassword,
      });

      setSuccess(response.message || 'Your password has been reset successfully! Redirecting to login...');

      // Redirect to /login after 2.5 seconds
      setTimeout(() => {
        navigate('/login');
      }, 2500);
    } catch (err) {
      const message = err.response?.data?.message || 'Failed to reset password. Please verify the code and try again.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card" style={{ maxWidth: '440px' }}>
        {/* Header */}
        <div className="auth-header">
          <div style={{ fontSize: '2.5rem', marginBottom: '8px' }}>🔐</div>
          <h1>{step === 1 ? 'Forgot Password' : 'Reset Password'}</h1>
          <p>
            {step === 1
              ? "Enter your email and we'll send you a 6-digit verification code"
              : `Enter the 6-digit code sent to ${email}`}
          </p>
        </div>

        {/* Feedback Alerts */}
        {error && <div className="alert alert-danger">{error}</div>}
        {success && <div className="alert alert-success">{success}</div>}

        {/* ===================================================================
            STEP 1 FORM: Enter Email Address
            =================================================================== */}
        {step === 1 && (
          <form onSubmit={handleRequestOtp}>
            <div className="form-group">
              <label className="form-label" htmlFor="reset-email">
                Email Address
              </label>
              <input
                id="reset-email"
                type="email"
                className="form-input"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoFocus
              />
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              style={{ width: '100%', marginTop: '8px' }}
              disabled={loading}
            >
              {loading ? 'Sending Code...' : 'Send Verification Code'}
            </button>

            <div style={{ textAlign: 'center', marginTop: '20px', fontSize: '0.9rem' }}>
              <Link to="/login" style={{ color: 'var(--color-primary)', fontWeight: '600', textDecoration: 'none' }}>
                &larr; Back to Sign In
              </Link>
            </div>
          </form>
        )}

        {/* ===================================================================
            STEP 2 FORM: Enter OTP & New Password
            =================================================================== */}
        {step === 2 && (
          <form onSubmit={handleResetPassword}>
            {/* OTP Field */}
            <div className="form-group">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                <label className="form-label" htmlFor="otp-code" style={{ marginBottom: 0 }}>
                  6-Digit Verification Code
                </label>
                <button
                  type="button"
                  onClick={() => {
                    setStep(1);
                    setError('');
                    setSuccess('');
                  }}
                  style={{
                    background: 'none',
                    border: 'none',
                    color: 'var(--color-primary)',
                    fontSize: '0.82rem',
                    cursor: 'pointer',
                    padding: 0,
                  }}
                >
                  Change Email
                </button>
              </div>
              <input
                id="otp-code"
                type="text"
                inputMode="numeric"
                maxLength={6}
                pattern="[0-9]*"
                className="form-input"
                placeholder="123456"
                value={otp}
                onChange={(e) => setOtp(e.target.value.replace(/\D/g, ''))}
                style={{
                  letterSpacing: '8px',
                  fontSize: '1.4rem',
                  fontWeight: '700',
                  textAlign: 'center',
                }}
                required
                autoFocus
              />
              <small style={{ color: 'var(--text-secondary)', display: 'block', marginTop: '4px', fontSize: '0.8rem' }}>
                Expires in 10 minutes. Check your inbox (or backend terminal in simulator mode).
              </small>
            </div>

            {/* New Password Field */}
            <div className="form-group">
              <label className="form-label" htmlFor="new-password">
                New Password
              </label>
              <div style={{ position: 'relative' }}>
                <input
                  id="new-password"
                  type={showPassword ? 'text' : 'password'}
                  className="form-input"
                  placeholder="At least 8 chars (letters & numbers)"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  style={{ paddingRight: '44px' }}
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
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
                  {showPassword ? '🙈' : '👁️'}
                </button>
              </div>
            </div>

            {/* Confirm New Password Field */}
            <div className="form-group">
              <label className="form-label" htmlFor="confirm-password">
                Confirm New Password
              </label>
              <div style={{ position: 'relative' }}>
                <input
                  id="confirm-password"
                  type={showConfirmPassword ? 'text' : 'password'}
                  className="form-input"
                  placeholder="Re-enter your new password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  style={{ paddingRight: '44px' }}
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword((prev) => !prev)}
                  aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
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
                  {showConfirmPassword ? '🙈' : '👁️'}
                </button>
              </div>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              className="btn btn-primary"
              style={{ width: '100%', marginTop: '8px' }}
              disabled={loading}
            >
              {loading ? 'Resetting Password...' : 'Reset Password'}
            </button>

            {/* Resend OTP Section with 60-Second Countdown */}
            <div style={{ textAlign: 'center', marginTop: '16px', fontSize: '0.88rem' }}>
              {countdown > 0 ? (
                <span style={{ color: 'var(--text-secondary)' }}>
                  Resend code in <strong>{countdown}s</strong>
                </span>
              ) : (
                <button
                  type="button"
                  onClick={handleRequestOtp}
                  disabled={loading}
                  style={{
                    background: 'none',
                    border: 'none',
                    color: 'var(--color-primary)',
                    fontWeight: '600',
                    cursor: 'pointer',
                    textDecoration: 'underline',
                    padding: 0,
                  }}
                >
                  Didn't get the code? Resend Code
                </button>
              )}
            </div>

            <div style={{ textAlign: 'center', marginTop: '16px', fontSize: '0.9rem' }}>
              <Link to="/login" style={{ color: 'var(--color-primary)', fontWeight: '600', textDecoration: 'none' }}>
                &larr; Back to Sign In
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};

export default ForgotPasswordPage;
