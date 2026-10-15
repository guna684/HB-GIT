import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

/**
 * =============================================================================
 * COMPONENT: ProtectedRoute (Route Security Guard)
 * =============================================================================
 * In single-page applications (SPAs), pages like Dashboard, Add Birthday,
 * and Edit Birthday are strictly private.
 * 
 * Think of ProtectedRoute like a security bouncer at a club door:
 * 1. It asks AuthContext: "Does the user have a valid token? (isAuthenticated)"
 * 2. If NO: It blocks access and redirects them immediately to /login.
 * 3. If YES: It renders the private page (the children).
 */
const ProtectedRoute = ({ children }) => {
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    // replace={true} prevents the protected URL from cluttering browser history
    return <Navigate to="/login" replace />;
  }

  return children;
};

export default ProtectedRoute;

