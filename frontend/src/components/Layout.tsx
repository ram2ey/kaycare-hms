import { NavLink, Outlet, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Roles } from '../types';

const navItems = [
  { to: '/patients', label: 'Patients', roles: null },
  { to: '/appointments', label: 'Appointments', roles: null },
  { to: '/consultations', label: 'Consultations', roles: [Roles.Doctor, Roles.Nurse] },
  { to: '/prescriptions', label: 'Prescriptions', roles: null },
  { to: '/prescriptions/templates', label: 'Rx Templates', roles: [Roles.Doctor, Roles.Admin, Roles.SuperAdmin] },
  { to: '/billing', label: 'Billing', roles: [Roles.Admin, Roles.Receptionist, Roles.SuperAdmin, Roles.BillingOfficer] },
  { to: '/billing/catalog', label: 'Price Catalog', roles: [Roles.Admin, Roles.SuperAdmin] },
  { to: '/billing/templates', label: 'Bill Templates', roles: [Roles.Admin, Roles.SuperAdmin] },
  { to: '/billing/payers', label: 'Payers', roles: [Roles.Admin, Roles.SuperAdmin] },
  { to: '/billing/claims', label: 'Claims', roles: [Roles.Admin, Roles.SuperAdmin, Roles.Receptionist, Roles.BillingOfficer] },
  { to: '/billing/credit-notes', label: 'Credit Notes', roles: [Roles.Admin, Roles.SuperAdmin, Roles.BillingOfficer] },
  { to: '/billing/refunds', label: 'Refunds', roles: [Roles.Admin, Roles.SuperAdmin, Roles.BillingOfficer] },
  { to: '/billing/ar-aging', label: 'AR Aging', roles: [Roles.Admin, Roles.SuperAdmin, Roles.BillingOfficer] },
  { to: '/billing/revenue-dashboard', label: 'Revenue', roles: [Roles.Admin, Roles.SuperAdmin, Roles.BillingOfficer] },
  { to: '/pharmacy/drugs', label: 'Drug Inventory', roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/pharmacy/reorder-alerts', label: 'Reorder Alerts', roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/pharmacy/suppliers', label: 'Suppliers', roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/pharmacy/purchase-orders', label: 'Purchase Orders', roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/pharmacy/cs-register', label: 'CS Register', roles: [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin] },
  { to: '/inpatient/wards', label: 'Wards & Beds', roles: null },
  { to: '/inpatient/admissions', label: 'Admissions', roles: null },
  { to: '/referrals', label: 'Referrals', roles: null },
  { to: '/reports', label: 'Reports', roles: [Roles.Admin, Roles.SuperAdmin, Roles.Doctor] },
  { to: '/documents', label: 'Documents', roles: null },
  { to: '/lab-orders', label: 'Lab Worklist', roles: null },
  { to: '/lab-results', label: 'Lab Results', roles: null },
  { to: '/audit-logs', label: 'Audit Logs', roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/users', label: 'Staff Users', roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/departments', label: 'Departments', roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/settings', label: 'Facility Settings', roles: [Roles.SuperAdmin, Roles.Admin] },
  { to: '/admin/tenants', label: 'Tenants', roles: [Roles.SuperAdmin] },
];

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login');
  }

  const visibleNav = navItems.filter(
    (item) => !item.roles || (user && item.roles.includes(user.role as never))
  );

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
        <Outlet />
      </main>
    </div>
  );
}
