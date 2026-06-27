import { NavLink, Outlet, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Roles } from '../types';
import { ErrorBoundary } from './ErrorBoundary';

const CLINICAL = [Roles.Doctor, Roles.Nurse, Roles.Pharmacist, Roles.LabTechnician, Roles.Admin, Roles.SuperAdmin];
const BILLING  = [Roles.Admin, Roles.SuperAdmin, Roles.BillingOfficer];

const navItems = [
  { to: '/patients',                   label: 'Patients',         roles: null },
  { to: '/appointments',               label: 'Appointments',     roles: null },
  { to: '/consultations',              label: 'Consultations',    roles: [Roles.Doctor, Roles.Nurse] },
  { to: '/prescriptions',              label: 'Prescriptions',    roles: [Roles.Doctor, Roles.Nurse, Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/prescriptions/templates',    label: 'Rx Templates',     roles: [Roles.Doctor, Roles.Admin, Roles.SuperAdmin] },
  { to: '/billing',                    label: 'Billing',          roles: [...BILLING, Roles.Doctor] },
  { to: '/billing/catalog',            label: 'Price Catalog',    roles: [Roles.Admin, Roles.SuperAdmin] },
  { to: '/billing/templates',          label: 'Bill Templates',   roles: [Roles.Admin, Roles.SuperAdmin] },
  { to: '/billing/payers',             label: 'Payers',           roles: [Roles.Admin, Roles.SuperAdmin] },
  { to: '/billing/claims',             label: 'Claims',           roles: [...BILLING] },
  { to: '/billing/credit-notes',       label: 'Credit Notes',     roles: [...BILLING] },
  { to: '/billing/refunds',            label: 'Refunds',          roles: [...BILLING] },
  { to: '/billing/ar-aging',           label: 'AR Aging',         roles: [...BILLING] },
  { to: '/billing/revenue-dashboard',  label: 'Revenue',          roles: [...BILLING] },
  { to: '/pharmacy/drugs',             label: 'Drug Inventory',   roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/pharmacy/reorder-alerts',    label: 'Reorder Alerts',   roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/pharmacy/suppliers',         label: 'Suppliers',        roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/pharmacy/purchase-orders',   label: 'Purchase Orders',  roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/pharmacy/cs-register',       label: 'CS Register',      roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/inpatient/wards',            label: 'Wards & Beds',     roles: [...CLINICAL] },
  { to: '/inpatient/admissions',       label: 'Admissions',       roles: [...CLINICAL] },
  { to: '/referrals',                  label: 'Referrals',        roles: [...CLINICAL] },
  { to: '/reports',                    label: 'Reports',          roles: [Roles.Admin, Roles.SuperAdmin, Roles.Doctor] },
  { to: '/documents',                  label: 'Documents',        roles: [...CLINICAL] },
  { to: '/lab-orders',                 label: 'Lab Worklist',     roles: [Roles.Doctor, Roles.Nurse, Roles.LabTechnician, Roles.Admin, Roles.SuperAdmin] },
  { to: '/lab-results',                label: 'Lab Results',      roles: [Roles.Doctor, Roles.Nurse, Roles.LabTechnician, Roles.Admin, Roles.SuperAdmin] },
  { to: '/lab-orders/hl7-inbox',       label: 'HL7 Inbox',        roles: [Roles.Doctor, Roles.Nurse, Roles.LabTechnician, Roles.Admin, Roles.SuperAdmin] },
  { to: '/radiology',                  label: 'Radiology Worklist', roles: [Roles.Doctor, Roles.Nurse, Roles.LabTechnician, Roles.Admin, Roles.SuperAdmin] },
  { to: '/audit-logs',                 label: 'Audit Logs',       roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/lab-catalog',          label: 'Lab Test Catalog', roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/imaging-catalog',      label: 'Imaging Catalog',  roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/wards',                label: 'Ward Setup',       roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/drug-formulary',       label: 'Drug Formulary',   roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/payer-tariffs',        label: 'Payer Tariffs',    roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/users',                label: 'Staff Users',      roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/departments',          label: 'Departments',      roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/settings',             label: 'Facility Settings',roles: [Roles.SuperAdmin, Roles.Admin] },
];

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

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login');
  }

  const rawRole = extractRole(user);
  const userRole = rawRole.toLowerCase();
  const isAdminUser = userRole === 'admin' || userRole === 'superadmin' || userRole.includes('admin') || (user?.email && user.email.toLowerCase().includes('admin'));

  const visibleNav = navItems.filter((item) => {
    if (!item.roles) return true;
    if (isAdminUser) return true; // Admins have access to all facility modules
    if (!userRole) return false;
    return item.roles.some((r) => r.toLowerCase() === userRole);
  });

  return (
    <div className="flex h-screen bg-gray-100">
      {/* Sidebar */}
      <aside className="w-64 bg-blue-900 text-white flex flex-col">
        <div className="px-6 py-5 border-b border-blue-800">
          <h1 className="text-xl font-bold tracking-wide">KayCare HMS</h1>
          <p className="text-blue-300 text-sm mt-1">{user?.tenantCode}</p>
        </div>

        <nav className="flex-1 overflow-y-auto py-4">
          {visibleNav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `block px-6 py-2.5 text-sm transition-colors ${
                  isActive
                    ? 'bg-blue-700 text-white font-medium'
                    : 'text-blue-200 hover:bg-blue-800 hover:text-white'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="px-6 py-4 border-t border-blue-800">
          <p className="text-sm text-blue-200 truncate">{user?.fullName}</p>
          <p className="text-xs text-blue-400">{user?.role}</p>
          <Link to="/change-password"
            className="mt-2 block text-xs text-blue-300 hover:text-white transition-colors">
            Change password
          </Link>
          <button
            onClick={handleLogout}
            className="mt-1 w-full text-left text-xs text-blue-300 hover:text-white transition-colors"
          >
            Sign out
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-y-auto">
        <ErrorBoundary>
          <Outlet />
        </ErrorBoundary>
      </main>
    </div>
  );
}
