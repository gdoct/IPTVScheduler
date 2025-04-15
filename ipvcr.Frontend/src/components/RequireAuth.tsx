import React, { useMemo } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { AuthService } from '../services/AuthService';

interface RequireAuthProps {
  children: React.ReactElement;
}

const RequireAuth: React.FC<RequireAuthProps> = ({ children }) => {
  const location = useLocation();
  const isAuthenticated = useMemo(() => AuthService.isAuthenticated(), []);

  // Memoize the state to ensure stability
  const redirectState = useMemo(() => ({ from: location }), [location]);

  // If not authenticated, redirect to login page
  if (!isAuthenticated) {
    return <Navigate to="/login" state={redirectState} replace />;
  }

  // If authenticated, render the child component
  return children;
};

export default RequireAuth;