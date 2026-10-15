import React, { createContext, useContext, useState, useEffect } from 'react';

/**
 * =============================================================================
 * CONTEXT: AuthContext
 * =============================================================================
 * In React, "Context" solves a common problem called "Prop Drilling".
 * 
 * Without Context:
 * If the Navbar, Dashboard, and BirthdayForm all need to know "Who is logged in?",
 * you would have to manually pass `user` and `token` through every single component.
 * 
 * With Context:
 * We create a single "AuthContext" at the top of our app. Any component anywhere
 * in the tree can just call `useAuth()` to get the current user, log in, log out,
 * or toggle light/dark mode!
 */
const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  // ---------------------------------------------------------------------------
  // 1. JWT Token State
  // We check localStorage on startup. If a token was saved previously, the user
  // stays logged in even if they refresh the browser page!
  // ---------------------------------------------------------------------------
  const [token, setToken] = useState(() => {
    return localStorage.getItem('token') || null;
  });

  // ---------------------------------------------------------------------------
  // 2. User Info State (Id, Name, Email)
  // Stored as a JSON string in localStorage, so we parse it back to an object.
  // ---------------------------------------------------------------------------
  const [user, setUser] = useState(() => {
    const savedUserJson = localStorage.getItem('user');
    return savedUserJson ? JSON.parse(savedUserJson) : null;
  });

  // ---------------------------------------------------------------------------
  // 3. Theme Mode (Light / Dark)
  // ---------------------------------------------------------------------------
  const [theme, setTheme] = useState(() => {
    return localStorage.getItem('theme') || 'light';
  });

  // Whenever the theme changes, update the HTML <html data-theme="..."> attribute
  // and persist the preference in localStorage so it remembers next time!
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  // Function to switch between 'light' and 'dark'
  const toggleTheme = () => {
    setTheme((currentTheme) => (currentTheme === 'light' ? 'dark' : 'light'));
  };

  // ---------------------------------------------------------------------------
  // 4. Log In Action
  // Called after a successful response from /api/auth/login or /api/auth/register.
  // Saves the JWT token and user info to both localStorage and React state.
  // ---------------------------------------------------------------------------
  const login = (authData) => {
    const { token: jwtToken, userId, name, email } = authData;
    const userInfo = { id: userId, name, email };

    localStorage.setItem('token', jwtToken);
    localStorage.setItem('user', JSON.stringify(userInfo));

    setToken(jwtToken);
    setUser(userInfo);
  };

  // ---------------------------------------------------------------------------
  // 5. Log Out Action
  // Wipes the JWT token and user info from localStorage and resets state to null.
  // ---------------------------------------------------------------------------
  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
  };

  // Helper boolean: If a token exists, the user is authenticated!
  const isAuthenticated = Boolean(token);

  // ---------------------------------------------------------------------------
  // 6. Provide values to all child components
  // ---------------------------------------------------------------------------
  return (
    <AuthContext.Provider
      value={{
        token,
        user,
        isAuthenticated,
        login,
        logout,
        theme,
        toggleTheme,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

/**
 * Custom Hook: useAuth
 * Usage in any component:
 *   const { user, isAuthenticated, logout } = useAuth();
 */
// oxlint-disable-next-line react/only-export-components, react-refresh/only-export-components
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside an <AuthProvider>');
  }
  return context;
};

