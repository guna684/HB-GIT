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

