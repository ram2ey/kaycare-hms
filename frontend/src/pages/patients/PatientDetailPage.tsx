import { useState, useEffect } from 'react';
import { useParams, Link, useSearchParams } from 'react-router-dom';
import { getPatient, getAllergies } from '../../api/patients';
import type { PatientDetailResponse, AllergyResponse } from '../../types/patients';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';
import OverviewTab from './tabs/OverviewTab';
import ConsultationsTab from './tabs/ConsultationsTab';
import PrescriptionsTab from './tabs/PrescriptionsTab';
import LabTab from './tabs/LabTab';
import BillingTab from './tabs/BillingTab';
import DocumentsTab from './tabs/DocumentsTab';
import AdmissionsTab from './tabs/AdmissionsTab';
import ReferralsTab from './tabs/ReferralsTab';
import VitalsTab from './tabs/VitalsTab';
import NursingNotesTab from './tabs/NursingNotesTab';

const TABS = [
  { key: 'overview',       label: 'Overview' },
  { key: 'consultations',  label: 'Consultations' },
  { key: 'prescriptions',  label: 'Prescriptions' },
  { key: 'lab',            label: 'Lab' },
  { key: 'billing',        label: 'Billing' },
  { key: 'admissions',     label: 'Admissions' },
  { key: 'referrals',      label: 'Referrals' },
  { key: 'vitals',         label: 'Vitals' },
  { key: 'nursing',        label: 'Nursing Notes' },
  { key: 'documents',      label: 'Documents' },
];

export default function PatientDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get('tab') ?? 'overview';

  const [patient, setPatient]   = useState<PatientDetailResponse | null>(null);
  const [allergies, setAllergies] = useState<AllergyResponse[]>([]);
  const [loading, setLoading]   = useState(true);
  const [error, setError]       = useState('');

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    Promise.all([getPatient(id), getAllergies(id)])
      .then(([p, a]) => { setPatient(p); setAllergies(a); })
      .catch(() => setError('Failed to load patient.'))
      .finally(() => setLoading(false));
  }, [id]);

  function setTab(key: string) {
    setSearchParams({ tab: key }, { replace: true });
  }

  const isDoctor    = user?.role === Roles.Doctor;
  const isAdmin     = user?.role === Roles.Admin || user?.role === Roles.SuperAdmin;
  const isReception = user?.role === Roles.Receptionist || user?.role === Roles.BillingOfficer;

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (error || !patient) return <div className="p-8 text-red-600">{error || 'Patient not found.'}</div>;

  return (
    <div className="flex flex-col h-full">
      {/* ── Header ── */}
      <div className="px-6 pt-6 pb-0 bg-white border-b border-gray-200">
        <div className="text-sm text-gray-500 mb-3">
          <Link to="/patients" className="hover:text-blue-600">Patients</Link>
          <span className="mx-2">/</span>
          <span className="text-gray-800">{patient.fullName}</span>
        </div>

        <div className="flex items-start justify-between mb-4">
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 font-bold text-lg">
              {patient.fullName.charAt(0)}
            </div>
            <div>
              <h2 className="text-xl font-semibold text-gray-900">{patient.fullName}</h2>
              <div className="flex items-center gap-3 mt-0.5">
                <span className="font-mono text-xs text-blue-700 bg-blue-50 px-2 py-0.5 rounded">{patient.medicalRecordNumber}</span>
                <span className="text-xs text-gray-500">{patient.gender} · Age {patient.age} · {patient.bloodType || 'Blood type unknown'}</span>
                {patient.hasAllergies && (
                  <span className="bg-red-100 text-red-700 text-xs font-medium px-2 py-0.5 rounded-full">⚠ Allergies</span>
                )}
                {!patient.isActive && (
                  <span className="bg-gray-100 text-gray-500 text-xs font-medium px-2 py-0.5 rounded-full">Inactive</span>
                )}
              </div>
            </div>
          </div>

          <div className="flex gap-2 flex-wrap justify-end">
            {(isDoctor || isAdmin) && (
              <Link to={`/consultations/new?patientId=${patient.patientId}`}
                className="px-3 py-1.5 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium">
                + Consultation
              </Link>
            )}
            {(isDoctor || isAdmin || isReception) && (
              <Link to={`/appointments/new?patientId=${patient.patientId}`}
                className="px-3 py-1.5 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">
                + Appointment
              </Link>
            )}
            {(isDoctor || isAdmin || isReception) && (
              <Link to={`/billing/new?patientId=${patient.patientId}`}
                className="px-3 py-1.5 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">
                + Bill
              </Link>
            )}
            {(isDoctor || isAdmin) && (
              <Link to={`/referrals/new?patientId=${patient.patientId}`}
                className="px-3 py-1.5 border border-indigo-300 text-indigo-700 rounded-lg hover:bg-indigo-50 text-sm font-medium">
                + Referral
              </Link>
            )}
          </div>
        </div>

        {/* ── Tabs ── */}
        <div className="flex gap-0 overflow-x-auto">
          {TABS.map(t => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`px-4 py-2.5 text-sm font-medium whitespace-nowrap border-b-2 transition-colors ${
                activeTab === t.key
                  ? 'border-blue-600 text-blue-700'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              {t.label}
            </button>
          ))}
        </div>
      </div>

      {/* ── Tab Content ── */}
      <div className="flex-1 overflow-auto p-6">
        {activeTab === 'overview'      && <OverviewTab      patient={patient} allergies={allergies} onAllergyChange={setAllergies} />}
        {activeTab === 'consultations' && <ConsultationsTab patientId={patient.patientId} />}
        {activeTab === 'prescriptions' && <PrescriptionsTab patientId={patient.patientId} />}
        {activeTab === 'lab'           && <LabTab           patientId={patient.patientId} />}
        {activeTab === 'billing'       && <BillingTab       patientId={patient.patientId} />}
        {activeTab === 'admissions'    && <AdmissionsTab    patientId={patient.patientId} />}
        {activeTab === 'referrals'     && <ReferralsTab     patientId={patient.patientId} />}
        {activeTab === 'vitals'        && <VitalsTab        patientId={patient.patientId} />}
        {activeTab === 'nursing'       && <NursingNotesTab  patientId={patient.patientId} />}
        {activeTab === 'documents'     && <DocumentsTab     patientId={patient.patientId} />}
      </div>
    </div>
  );
}
