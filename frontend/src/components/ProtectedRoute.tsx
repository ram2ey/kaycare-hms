import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { Role } from '../types';

interface Props {
  allowedRoles?: Role[];
}

export default function ProtectedRoute({ allowedRoles }: Props) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) return <Navigate to="/login" replace />;

  // Force password change before accessing any other route
  if (user.mustChangePassword && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />;
  }

  const userRole = user?.role?.toLowerCase() ?? '';
  const isAdminUser = userRole === 'admin' || userRole === 'superadmin' || userRole.includes('admin');

  if (allowedRoles) {
    const hasAccess = allowedRoles.some((r) => {
      const targetRole = r.toLowerCase();
      if (targetRole === userRole) return true;
      if (isAdminUser && (targetRole === 'admin' || targetRole === 'superadmin')) return true;
      return false;
    });
    if (!hasAccess) {
      return <Navigate to="/unauthorized" replace />;
    }
  }

  return <Outlet />;
}
