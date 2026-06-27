import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { Role } from '../types';

interface Props {
  allowedRoles?: Role[];
}

function extractRole(user: any): string {
  if (!user) return '';
  if (typeof user.role === 'string' && user.role) return user.role;
  if (typeof user.roleName === 'string' && user.roleName) return user.roleName;
  if (Array.isArray(user.roles) && user.roles.length > 0) return String(user.roles[0]);
  if (user.token && typeof user.token === 'string') {
    try {
      const base64Url = user.token.split('.')[1];
      if (base64Url) {
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join(''));
        const payload = JSON.parse(jsonPayload);
        const jwtRole = payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload['roles'];
        if (typeof jwtRole === 'string') return jwtRole;
        if (Array.isArray(jwtRole) && jwtRole.length > 0) return String(jwtRole[0]);
      }
    } catch {}
  }
  if (user.email && user.email.toLowerCase().includes('admin')) return 'Admin';
  return '';
}

export default function ProtectedRoute({ allowedRoles }: Props) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) return <Navigate to="/login" replace />;

  // Force password change before accessing any other route
  if (user.mustChangePassword && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />;
  }

  const rawRole = extractRole(user);
  const userRole = rawRole.toLowerCase();
  const isAdminUser = userRole === 'admin' || userRole === 'superadmin' || userRole.includes('admin') || (user?.email && user.email.toLowerCase().includes('admin'));

  if (allowedRoles) {
    if (isAdminUser) return <Outlet />;
    const hasAccess = allowedRoles.some((r) => r.toLowerCase() === userRole);
    if (!hasAccess) {
      return <Navigate to="/unauthorized" replace />;
    }
  }

  return <Outlet />;
}
