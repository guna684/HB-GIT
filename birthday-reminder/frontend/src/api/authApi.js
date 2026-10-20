import axiosClient from './axiosClient';

/**
 * =============================================================================
 * AUTH API FUNCTIONS
 * =============================================================================
 * These functions handle registering and logging in users.
 * Both send standard JSON payloads to the ASP.NET Core AuthController.
 */

/**
 * 1. POST /api/auth/register
 * Sends { name, email, password } to create a new user account.
 * Returns { token, userId, name, email } upon success.
 */
export const registerUser = async (userData) => {
  const response = await axiosClient.post('/auth/register', userData);
  return response.data;
};

/**
 * 2. POST /api/auth/login
 * Sends { email, password } to verify credentials.
 * Returns { token, userId, name, email } upon success.
 */
export const loginUser = async (credentials) => {
  const response = await axiosClient.post('/auth/login', credentials);
  return response.data;
};

/**
 * 3. POST /api/auth/forgot-password
 * Sends { email } to request a 6-digit OTP code sent via email.
 */
export const forgotPassword = async (email) => {
  const response = await axiosClient.post('/auth/forgot-password', { email });
  return response.data;
};

/**
 * 4. POST /api/auth/reset-password
 * Sends { email, otp, newPassword } to verify code and set a new password.
 */
export const resetPassword = async (payload) => {
  const response = await axiosClient.post('/auth/reset-password', payload);
  return response.data;
};

/**
 * 5. POST /api/auth/change-password
 * Sends { currentPassword, newPassword } for a logged-in user.
 */
export const changePassword = async (payload) => {
  const response = await axiosClient.post('/auth/change-password', payload);
  return response.data;
};

