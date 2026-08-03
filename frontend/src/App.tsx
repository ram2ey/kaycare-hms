import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Layout from './components/Layout';
import SuperAdminLayout from './components/SuperAdminLayout';
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
import BillTemplatesPage from './pages/billing/BillTemplatesPage';
import PayersPage from './pages/billing/PayersPage';
import ARAgingPage from './pages/billing/ARAgingPage';
import RevenueDashboardPage from './pages/billing/RevenueDashboardPage';
import InsuranceClaimsPage from './pages/billing/InsuranceClaimsPage';
import ClaimDetailPage from './pages/billing/ClaimDetailPage';
import CreditNotesPage from './pages/billing/CreditNotesPage';
import CreditNoteDetailPage from './pages/billing/CreditNoteDetailPage';
import RefundsPage from './pages/billing/RefundsPage';
import RefundDetailPage from './pages/billing/RefundDetailPage';
import DocumentsPage from './pages/documents/DocumentsPage';
import LabResultsPage from './pages/lab-results/LabResultsPage';
import LabResultDetailPage from './pages/lab-results/LabResultDetailPage';
import LabWaitingListPage from './pages/lab-orders/LabWaitingListPage';
import PlaceLabOrderPage from './pages/lab-orders/PlaceLabOrderPage';
import LabOrderDetailPage from './pages/lab-orders/LabOrderDetailPage';
import { Hl7InboxPage } from './pages/lab-orders/Hl7InboxPage';
import { RadiologyOrdersPage } from './pages/radiology/RadiologyOrdersPage';
import { NewRadiologyOrderPage } from './pages/radiology/NewRadiologyOrderPage';
import { RadiologyOrderDetailPage } from './pages/radiology/RadiologyOrderDetailPage';
import AuditLogsPage from './pages/audit-logs/AuditLogsPage';
import UsersPage from './pages/admin/UsersPage';
import DepartmentsPage from './pages/admin/DepartmentsPage';
import FacilitySettingsPage from './pages/admin/FacilitySettingsPage';
import LabCatalogPage from './pages/admin/LabCatalogPage';
import ImagingCatalogPage from './pages/admin/ImagingCatalogPage';
import WardSetupPage from './pages/admin/WardSetupPage';
import DrugFormularyPage from './pages/admin/DrugFormularyPage';
import PayerTariffsPage from './pages/admin/PayerTariffsPage';
import TenantsPage from './pages/admin/TenantsPage';
import PharmacyDrugsPage from './pages/pharmacy/PharmacyDrugsPage';
import ReorderAlertsPage from './pages/pharmacy/ReorderAlertsPage';
import SuppliersPage from './pages/pharmacy/SuppliersPage';
import PurchaseOrdersPage from './pages/pharmacy/PurchaseOrdersPage';
import PurchaseOrderDetailPage from './pages/pharmacy/PurchaseOrderDetailPage';
import CSRegisterPage from './pages/pharmacy/CSRegisterPage';
import WardsPage from './pages/inpatient/WardsPage';
import WardDetailPage from './pages/inpatient/WardDetailPage';
import AdmissionsPage from './pages/inpatient/AdmissionsPage';
import AdmissionDetailPage from './pages/inpatient/AdmissionDetailPage';
import InpatientBillingPage from './pages/inpatient/InpatientBillingPage';
import DischargeSummaryPage from './pages/inpatient/DischargeSummaryPage';
import VitalSignsPage from './pages/nursing/VitalSignsPage';
import NursingNotesPage from './pages/nursing/NursingNotesPage';
import MARPage from './pages/nursing/MARPage';
import ReferralsPage from './pages/referrals/ReferralsPage';
import ReferralDetailPage from './pages/referrals/ReferralDetailPage';
import CreateReferralPage from './pages/referrals/CreateReferralPage';
import ReportsPage from './pages/reports/ReportsPage';
import ChangePasswordPage from './pages/ChangePasswordPage';
import { Roles } from './types';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          {/* SuperAdmin platform portal — completely separate layout */}
          <Route element={<ProtectedRoute allowedRoles={[Roles.SuperAdmin]} />}>
            <Route element={<SuperAdminLayout />}>
              <Route path="platform/tenants" element={<TenantsPage />} />
              <Route path="platform/users" element={<UsersPage />} />
              <Route path="platform/audit-logs" element={<AuditLogsPage />} />
            </Route>
          </Route>

          <Route element={<ProtectedRoute />}>
            <Route element={<Layout />}>
              <Route index element={<Navigate to="/patients" replace />} />
              <Route path="patients" element={<PatientsListPage />} />
              <Route path="patients/new" element={<CreatePatientPage />} />
              <Route path="patients/:id" element={<PatientDetailPage />} />
              <Route path="appointments" element={<AppointmentsPage />} />
              <Route path="appointments/new" element={<CreateAppointmentPage />} />
              <Route path="appointments/:id" element={<AppointmentDetailPage />} />

              {/* Consultations */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Doctor, Roles.Nurse]} />}>
                <Route path="consultations" element={<ConsultationsPage />} />
                <Route path="consultations/new" element={<CreateConsultationPage />} />
                <Route path="consultations/:id" element={<ConsultationDetailPage />} />
              </Route>

              {/* Prescriptions */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Doctor, Roles.Nurse, Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin]} />}>
                <Route path="prescriptions" element={<PrescriptionsPage />} />
                <Route path="prescriptions/new" element={<CreatePrescriptionPage />} />
                <Route path="prescriptions/:id" element={<PrescriptionDetailPage />} />
              </Route>

              {/* Rx Templates */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Doctor, Roles.Admin, Roles.SuperAdmin]} />}>
                <Route path="prescriptions/templates" element={<TemplatesPage />} />
                <Route path="prescriptions/templates/new" element={<CreateEditTemplatePage mode="create" />} />
                <Route path="prescriptions/templates/:id/edit" element={<CreateEditTemplatePage mode="edit" />} />
              </Route>

              {/* Billing Main (Biller, Admin, SuperAdmin, Receptionist) */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Admin, Roles.SuperAdmin, Roles.BillingOfficer, Roles.Receptionist]} />}>
                <Route path="billing" element={<BillingPage />} />
                <Route path="billing/new" element={<CreateBillPage />} />
                <Route path="billing/:id" element={<BillDetailPage />} />
              </Route>

              {/* Billing Sub-sections & Financial Logs (Biller, Admin, SuperAdmin only) */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Admin, Roles.SuperAdmin, Roles.BillingOfficer]} />}>
                <Route path="billing/catalog" element={<ServiceCatalogPage />} />
                <Route path="billing/templates" element={<BillTemplatesPage />} />
                <Route path="billing/payers" element={<PayersPage />} />
                <Route path="billing/ar-aging" element={<ARAgingPage />} />
                <Route path="billing/revenue-dashboard" element={<RevenueDashboardPage />} />
                <Route path="billing/claims" element={<InsuranceClaimsPage />} />
                <Route path="billing/claims/:id" element={<ClaimDetailPage />} />
                <Route path="billing/credit-notes" element={<CreditNotesPage />} />
                <Route path="billing/credit-notes/:id" element={<CreditNoteDetailPage />} />
                <Route path="billing/refunds" element={<RefundsPage />} />
                <Route path="billing/refunds/:id" element={<RefundDetailPage />} />
              </Route>

              {/* Pharmacy & Stocks */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin]} />}>
                <Route path="pharmacy/drugs" element={<PharmacyDrugsPage />} />
                <Route path="pharmacy/reorder-alerts" element={<ReorderAlertsPage />} />
                <Route path="pharmacy/suppliers" element={<SuppliersPage />} />
                <Route path="pharmacy/purchase-orders" element={<PurchaseOrdersPage />} />
                <Route path="pharmacy/purchase-orders/:id" element={<PurchaseOrderDetailPage />} />
                <Route path="pharmacy/cs-register" element={<CSRegisterPage />} />
              </Route>

              {/* Inpatient (Wards, Beds, Admissions), Nursing, Referrals & Documents (Doctor, Nurse, Receptionist, Admin, SuperAdmin) */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Doctor, Roles.Nurse, Roles.Receptionist, Roles.Admin, Roles.SuperAdmin]} />}>
                <Route path="inpatient/wards" element={<WardsPage />} />
                <Route path="inpatient/wards/:id" element={<WardDetailPage />} />
                <Route path="inpatient/admissions" element={<AdmissionsPage />} />
                <Route path="inpatient/admissions/:id" element={<AdmissionDetailPage />} />
                <Route path="inpatient/admissions/:id/billing" element={<InpatientBillingPage />} />
                <Route path="inpatient/admissions/:id/discharge-summary" element={<DischargeSummaryPage />} />
                <Route path="patients/:patientId/vitals" element={<VitalSignsPage />} />
                <Route path="patients/:patientId/nursing-notes" element={<NursingNotesPage />} />
                <Route path="inpatient/admissions/:admissionId/mar" element={<MARPage />} />
                <Route path="referrals" element={<ReferralsPage />} />
                <Route path="referrals/new" element={<CreateReferralPage />} />
                <Route path="referrals/:id" element={<ReferralDetailPage />} />
                <Route path="documents" element={<DocumentsPage />} />
              </Route>

              {/* Lab Results (Visible to Doctors, Nurses, Lab Techs, and Admins) */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Doctor, Roles.Nurse, Roles.LabTechnician, Roles.Admin, Roles.SuperAdmin]} />}>
                <Route path="lab-results" element={<LabResultsPage />} />
                <Route path="lab-results/:id" element={<LabResultDetailPage />} />
              </Route>

              {/* Lab & Radiology Worklists (Doctors, Lab Techs, and Admins) */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Doctor, Roles.LabTechnician, Roles.Admin, Roles.SuperAdmin]} />}>
                <Route path="lab-orders" element={<LabWaitingListPage />} />
                <Route path="lab-orders/new" element={<PlaceLabOrderPage />} />
                <Route path="lab-orders/:id" element={<LabOrderDetailPage />} />
                <Route path="radiology" element={<RadiologyOrdersPage />} />
                <Route path="radiology/new" element={<NewRadiologyOrderPage />} />
                <Route path="radiology/:id" element={<RadiologyOrderDetailPage />} />
              </Route>

              {/* HL7 Message Inbox (Lab Techs and Admins only) */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.LabTechnician, Roles.Admin, Roles.SuperAdmin]} />}>
                <Route path="lab-orders/hl7-inbox" element={<Hl7InboxPage />} />
              </Route>

              {/* General Reports */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.Admin, Roles.SuperAdmin, Roles.Doctor]} />}>
                <Route path="reports" element={<ReportsPage />} />
              </Route>

              {/* Admin Setup Panel */}
              <Route element={<ProtectedRoute allowedRoles={[Roles.SuperAdmin, Roles.Admin]} />}>
                <Route path="audit-logs" element={<AuditLogsPage />} />
                <Route path="admin/users" element={<UsersPage />} />
                <Route path="admin/departments" element={<DepartmentsPage />} />
                <Route path="admin/settings" element={<FacilitySettingsPage />} />
                <Route path="admin/lab-catalog" element={<LabCatalogPage />} />
                <Route path="admin/imaging-catalog" element={<ImagingCatalogPage />} />
                <Route path="admin/wards" element={<WardSetupPage />} />
                <Route path="admin/drug-formulary" element={<DrugFormularyPage />} />
                <Route path="admin/payer-tariffs" element={<PayerTariffsPage />} />
              </Route>

              <Route path="change-password" element={<ChangePasswordPage />} />
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
