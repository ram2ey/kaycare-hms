import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { Role } from '../types';

interface Props {
  allowedRoles?: Role[];
}

export default function ProtectedRoute(props?: Props) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) return <Navigate to="/login" replace />;

  // Force password change before accessing any other route
  if (user.mustChangePassword && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />;
  }

  // Enforce role-based access control
  if (props?.allowedRoles && !props.allowedRoles.includes(user.role as never)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <Outlet />;
}
