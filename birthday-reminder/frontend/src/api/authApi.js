import axiosClient from './axiosClient';

/**
 * Authentication API calls for User registration and login.
 */

// Register a new user account
export const registerUser = async (userData) => {
  const response = await axiosClient.post('/auth/register', userData);
  return response.data;
};

// Log in an existing user and obtain a JWT token
export const loginUser = async (credentials) => {
  const response = await axiosClient.post('/auth/login', credentials);
  return response.data;
};
