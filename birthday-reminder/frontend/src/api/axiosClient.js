import axios from 'axios';

/**
 * =============================================================================
 * API CLIENT: axiosClient
 * =============================================================================
 * Axios is a popular JavaScript library used to send HTTP requests (GET, POST,
 * PUT, DELETE) to our backend server.
 * 
 * Instead of configuring URL, headers, and error handling on every single API call,
 * we create this single "axiosClient" instance that handles it automatically!
 */

// 1. Get backend API base URL from Vite's environment variables (.env file)
// If not specified, default to http://localhost:5000/api
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const axiosClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Accept': 'application/json',
  },
});

/**
 * 2. REQUEST INTERCEPTOR: The "Security Badge Stamper"
 * Before ANY request leaves the browser and goes to the backend, this code runs:
 * - Checks if we have a JWT token saved in localStorage.
 * - If yes, it attaches the header: "Authorization: Bearer <token>"
 * - This allows the ASP.NET Core backend to know who you are!
 */
axiosClient.interceptors.request.use(
  (config) => {
    const savedToken = localStorage.getItem('token');
    if (savedToken) {
      config.headers.Authorization = `Bearer ${savedToken}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

/**
 * 3. RESPONSE INTERCEPTOR: The "Expired Session Guard"
 * Every time the backend sends a response back to the browser, this code runs:
 * - If status is 401 Unauthorized (meaning your token expired or is fake):
 *   it automatically clears your credentials and redirects you to the login screen!
 */
axiosClient.interceptors.response.use(
  (response) => {
    // If request succeeded (200 OK, 201 Created), simply return response data
    return response;
  },
  (error) => {
    if (error.response && error.response.status === 401) {
      // Clear expired credentials
      localStorage.removeItem('token');
      localStorage.removeItem('user');

      // Redirect to login page if we are not already on login or register
      const currentPath = window.location.pathname;
      if (!currentPath.includes('/login') && !currentPath.includes('/register')) {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default axiosClient;

