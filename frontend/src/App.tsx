import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Layout from './components/Layout';
import LoginPage from './pages/auth/LoginPage';
import PatientsListPage from './pages/patients/PatientsListPage';
import PatientDetailPage from './pages/patients/PatientDetailPage';
import CreatePatientPage from './pages/patients/CreatePatientPage';
import AppointmentsPage from './pages/appointments/AppointmentsPage';
import AppointmentDetailPage from './pages/appointments/AppointmentDetailPage';
import CreateAppointmentPage from './pages/appointments/CreateAppointmentPage';
import ConsultationsPage from './pages/consultations/ConsultationsPage';
import ConsultationDetailPage from './pages/consultations/ConsultationDetailPage';
import CreateConsultationPage from './pages/consultations/CreateConsultationPage';
import PrescriptionsPage from './pages/prescriptions/PrescriptionsPage';
import PrescriptionDetailPage from './pages/prescriptions/PrescriptionDetailPage';
import CreatePrescriptionPage from './pages/prescriptions/CreatePrescriptionPage';
import TemplatesPage from './pages/prescriptions/TemplatesPage';
import CreateEditTemplatePage from './pages/prescriptions/CreateEditTemplatePage';
import BillingPage from './pages/billing/BillingPage';
import BillDetailPage from './pages/billing/BillDetailPage';
import CreateBillPage from './pages/billing/CreateBillPage';
import ServiceCatalogPage from './pages/billing/ServiceCatalogPage';
import PayersPage from './pages/billing/PayersPage';
import DocumentsPage from './pages/documents/DocumentsPage';
import LabResultsPage from './pages/lab-results/LabResultsPage';
import LabResultDetailPage from './pages/lab-results/LabResultDetailPage';
import LabWaitingListPage from './pages/lab-orders/LabWaitingListPage';
import PlaceLabOrderPage from './pages/lab-orders/PlaceLabOrderPage';
import LabOrderDetailPage from './pages/lab-orders/LabOrderDetailPage';
import AuditLogsPage from './pages/audit-logs/AuditLogsPage';
import { Roles } from './types';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          <Route element={<ProtectedRoute />}>
            <Route element={<Layout />}>
              <Route index element={<Navigate to="/patients" replace />} />
              <Route path="patients" element={<PatientsListPage />} />
              <Route path="patients/new" element={<CreatePatientPage />} />
              <Route path="patients/:id" element={<PatientDetailPage />} />
              <Route path="appointments" element={<AppointmentsPage />} />
              <Route path="appointments/new" element={<CreateAppointmentPage />} />
              <Route path="appointments/:id" element={<AppointmentDetailPage />} />
              <Route path="consultations" element={<ConsultationsPage />} />
              <Route path="consultations/new" element={<CreateConsultationPage />} />
              <Route path="consultations/:id" element={<ConsultationDetailPage />} />
              <Route path="prescriptions" element={<PrescriptionsPage />} />
              <Route path="prescriptions/new" element={<CreatePrescriptionPage />} />
              <Route path="prescriptions/templates" element={<TemplatesPage />} />
              <Route path="prescriptions/templates/new" element={<CreateEditTemplatePage mode="create" />} />
              <Route path="prescriptions/templates/:id/edit" element={<CreateEditTemplatePage mode="edit" />} />
              <Route path="prescriptions/:id" element={<PrescriptionDetailPage />} />
              <Route path="billing" element={<BillingPage />} />
              <Route path="billing/new" element={<CreateBillPage />} />
              <Route path="billing/catalog" element={<ProtectedRoute allowedRoles={[Roles.Admin, Roles.SuperAdmin]} />}>
                <Route index element={<ServiceCatalogPage />} />
              </Route>
              <Route path="billing/payers" element={<ProtectedRoute allowedRoles={[Roles.Admin, Roles.SuperAdmin]} />}>
                <Route index element={<PayersPage />} />
              </Route>
              <Route path="billing/:id" element={<BillDetailPage />} />
              <Route path="documents" element={<DocumentsPage />} />
              <Route path="lab-results" element={<LabResultsPage />} />
              <Route path="lab-results/:id" element={<LabResultDetailPage />} />
              <Route path="lab-orders" element={<LabWaitingListPage />} />
              <Route path="lab-orders/new" element={<PlaceLabOrderPage />} />
              <Route path="lab-orders/:id" element={<LabOrderDetailPage />} />
              <Route
                path="audit-logs"
                element={<ProtectedRoute allowedRoles={[Roles.SuperAdmin, Roles.Admin]} />}
              >
                <Route index element={<AuditLogsPage />} />
              </Route>
            </Route>
          </Route>

          <Route
            path="/unauthorized"
            element={
              <div className="p-8 text-center">
                <h2 className="text-xl font-semibold text-red-600">Access Denied</h2>
                <p className="text-gray-500 mt-2">You don't have permission to view this page.</p>
              </div>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
