import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { addAllergy, removeAllergy } from '../../../api/patients';
import { getPatientConsultations } from '../../../api/consultations';
import { getPatientPrescriptions } from '../../../api/prescriptions';
import { getPatientBills } from '../../../api/billing';
import { getLatestVitals } from '../../../api/nursing';
import type { PatientDetailResponse, AllergyResponse, AddAllergyRequest } from '../../../types/patients';
import type { ConsultationSummaryResponse } from '../../../types/consultations';
import type { PrescriptionResponse } from '../../../types/prescriptions';
import type { BillResponse } from '../../../types/billing';
import type { VitalSignsResponse } from '../../../types/nursing';
import { useAuth } from '../../../contexts/AuthContext';
import { Roles } from '../../../types';
import { ConfirmDialog } from '../../../components/ConfirmDialog';

const SEVERITY_COLORS: Record<string, string> = {
  Mild: 'bg-yellow-100 text-yellow-700',
  Moderate: 'bg-orange-100 text-orange-700',
  Severe: 'bg-red-100 text-red-700',
  'Life-threatening': 'bg-red-200 text-red-900',
};


interface Props {
  patient: PatientDetailResponse;
  allergies: AllergyResponse[];
  onAllergyChange: (allergies: AllergyResponse[]) => void;
}

const emptyAllergy: AddAllergyRequest = { allergyType: 'Drug', allergenName: '', reaction: '', severity: 'Mild' };

export default function OverviewTab({ patient, allergies, onAllergyChange }: Props) {
  const { user } = useAuth();
  const [lastConsult, setLastConsult]       = useState<ConsultationSummaryResponse | null>(null);
  const [activePrescriptions, setActivePrescriptions] = useState<PrescriptionResponse[]>([]);
  const [outstandingBill, setOutstandingBill] = useState<BillResponse | null>(null);
  const [latestVitals, setLatestVitals]     = useState<VitalSignsResponse | null>(null);
  const [showAllergyForm, setShowAllergyForm] = useState(false);
  const [allergyForm, setAllergyForm]       = useState<AddAllergyRequest>(emptyAllergy);
  const [savingAllergy, setSavingAllergy]   = useState(false);
  const [allergyError, setAllergyError]     = useState('');
  const [removingAllergy, setRemovingAllergy] = useState(false);
  const [confirmAction, setConfirmAction]   = useState<null | { message: string; run: () => void }>(null);

  useEffect(() => {
    const pid = patient.patientId;
    getPatientConsultations(pid).then(c => setLastConsult(Array.isArray(c) ? (c[0] ?? null) : null)).catch(() => {});
    getPatientPrescriptions(pid).then(p => setActivePrescriptions(Array.isArray(p) ? p.filter(x => x.status === 'Active' || x.status === 'PartiallyDispensed') : [])).catch(() => {});
    getPatientBills(pid).then(b => setOutstandingBill(Array.isArray(b) ? (b.find(x => x.status === 'Issued' || x.status === 'PartiallyPaid') ?? null) : null)).catch(() => {});
    getLatestVitals(pid).then(setLatestVitals).catch(() => {});
  }, [patient.patientId]);

  async function handleAddAllergy(e: React.FormEvent) {
    e.preventDefault();
    setSavingAllergy(true);
    setAllergyError('');
    try {
      const a = await addAllergy(patient.patientId, allergyForm);
      onAllergyChange([...allergies, a]);
      setAllergyForm(emptyAllergy);
      setShowAllergyForm(false);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setAllergyError(msg || 'Failed to add allergy.');
    } finally { setSavingAllergy(false); }
  }

  async function handleRemoveAllergy(allergyId: string) {
    setRemovingAllergy(true);
    try {
      await removeAllergy(patient.patientId, allergyId);
      onAllergyChange(allergies.filter(a => a.allergyId !== allergyId));
    } finally {
      setRemovingAllergy(false);
    }
  }

  const canManageAllergies = [Roles.SuperAdmin, Roles.Admin, Roles.Doctor, Roles.Nurse].includes(user?.role as never);
  const canRemoveAllergy   = [Roles.SuperAdmin, Roles.Admin, Roles.Doctor].includes(user?.role as never);

  return (
    <div className="space-y-5 max-w-5xl">
      {/* ── Summary Cards ── */}
      <div className="grid grid-cols-4 gap-4">
        <SummaryCard
          title="Last Consultation"
          value={lastConsult ? new Date(lastConsult.createdAt).toLocaleDateString() : '—'}
          sub={lastConsult ? `Dr. ${lastConsult.doctorName.split(' ').pop()}` : 'No consultations yet'}
          color="blue"
          link={lastConsult ? `/consultations/${lastConsult.consultationId}` : undefined}
        />
        <SummaryCard
          title="Active Prescriptions"
          value={String(activePrescriptions.length)}
          sub={activePrescriptions.length === 0 ? 'None active' : `${activePrescriptions[0]?.itemCount ?? 0} medication(s)`}
          color="green"
        />
        <SummaryCard
          title="Outstanding Balance"
          value={outstandingBill ? `GHS ${outstandingBill.balanceDue.toFixed(2)}` : 'GHS 0.00'}
          sub={outstandingBill ? `Bill ${outstandingBill.billNumber}` : 'No outstanding bills'}
          color={outstandingBill ? 'red' : 'gray'}
          link={outstandingBill ? `/billing/${outstandingBill.billId}` : undefined}
        />
        <SummaryCard
          title="Latest BP"
          value={latestVitals?.bloodPressureSystolic ? `${latestVitals.bloodPressureSystolic}/${latestVitals.bloodPressureDiastolic}` : '—'}
          sub={latestVitals ? `Pulse ${latestVitals.pulseRate ?? '—'} · SpO2 ${latestVitals.spO2 ?? '—'}%` : 'No vitals recorded'}
          color="teal"
        />
      </div>

      <div className="grid grid-cols-2 gap-5">
        {/* Demographics */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-4">Demographics</h3>
          <dl className="space-y-2.5">
            <Row label="Date of Birth" value={`${patient.dateOfBirth} (age ${patient.age})`} />
            <Row label="Gender"        value={patient.gender} />
            <Row label="Blood Type"    value={patient.bloodType} />
            <Row label="National ID"   value={patient.nationalId} />
          </dl>
        </section>

        {/* Contact */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-4">Contact</h3>
          <dl className="space-y-2.5">
            <Row label="Phone"     value={patient.phoneNumber} />
            <Row label="Alt Phone" value={patient.alternatePhone} />
            <Row label="Email"     value={patient.email} />
            <Row label="Address"   value={[patient.addressLine1, patient.city, patient.country].filter(Boolean).join(', ')} />
          </dl>
        </section>

        {/* Emergency Contact */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-4">Emergency Contact</h3>
          <dl className="space-y-2.5">
            <Row label="Name"     value={patient.emergencyContactName} />
            <Row label="Phone"    value={patient.emergencyContactPhone} />
            <Row label="Relation" value={patient.emergencyContactRelation} />
          </dl>
        </section>

        {/* Insurance */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-4">Insurance</h3>
          <dl className="space-y-2.5">
            <Row label="NHIS Number" value={patient.nhisNumber} />
            <Row label="Provider"    value={patient.insuranceProvider} />
            <Row label="Policy #"    value={patient.insurancePolicyNumber} />
            <Row label="Group #"     value={patient.insuranceGroupNumber} />
          </dl>
        </section>

        {/* Allergies — full width */}
        <section className="col-span-2 bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Allergies</h3>
            {canManageAllergies && (
              <button onClick={() => setShowAllergyForm(s => !s)}
                className="text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 font-medium px-3 py-1.5 rounded-lg">
                {showAllergyForm ? 'Cancel' : '+ Add Allergy'}
              </button>
            )}
          </div>

          {showAllergyForm && (
            <form onSubmit={handleAddAllergy} className="grid grid-cols-4 gap-3 mb-4 p-4 bg-gray-50 rounded-lg">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Type</label>
                <select value={allergyForm.allergyType}
                  onChange={e => setAllergyForm(f => ({ ...f, allergyType: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">
                  {['Drug','Food','Environmental','Other'].map(t => <option key={t}>{t}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Allergen *</label>
                <input required value={allergyForm.allergenName}
                  onChange={e => setAllergyForm(f => ({ ...f, allergenName: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" placeholder="e.g. Penicillin" />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Severity</label>
                <select value={allergyForm.severity}
                  onChange={e => setAllergyForm(f => ({ ...f, severity: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">
                  {['Mild','Moderate','Severe','Life-threatening'].map(s => <option key={s}>{s}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Reaction</label>
                <input value={allergyForm.reaction}
                  onChange={e => setAllergyForm(f => ({ ...f, reaction: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" placeholder="e.g. Rash" />
              </div>
              <div className="col-span-4 flex items-center justify-between">
                {allergyError ? <p className="text-sm text-red-600">{allergyError}</p> : <span />}
                <button type="submit" disabled={savingAllergy}
                  className="bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium px-4 py-2 rounded-lg disabled:opacity-50">
                  {savingAllergy ? 'Saving…' : 'Save Allergy'}
                </button>
              </div>
            </form>
          )}

          {allergies.length === 0 ? (
            <p className="text-sm text-gray-400">No known allergies.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs text-gray-500 border-b border-gray-100">
                  <th className="pb-2 font-medium">Type</th>
                  <th className="pb-2 font-medium">Allergen</th>
                  <th className="pb-2 font-medium">Severity</th>
                  <th className="pb-2 font-medium">Reaction</th>
                  <th className="pb-2 font-medium">Recorded</th>
                  {canRemoveAllergy && <th />}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {allergies.map(a => (
                  <tr key={a.allergyId}>
                    <td className="py-2 text-gray-600">{a.allergyType}</td>
                    <td className="py-2 font-medium text-gray-800">{a.allergenName}</td>
                    <td className="py-2">
                      <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${SEVERITY_COLORS[a.severity] ?? 'bg-gray-100 text-gray-600'}`}>
                        {a.severity}
                      </span>
                    </td>
                    <td className="py-2 text-gray-500">{a.reaction ?? '—'}</td>
                    <td className="py-2 text-gray-400 text-xs">{new Date(a.recordedAt).toLocaleDateString()}</td>
                    {canRemoveAllergy && (
                      <td className="py-2 text-right">
                        <button onClick={() => setConfirmAction({ message: 'Remove this allergy?', run: () => handleRemoveAllergy(a.allergyId) })} className="text-xs text-red-500 hover:text-red-700">Remove</button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </div>

      <ConfirmDialog
        open={!!confirmAction}
        title="Confirm"
        message={confirmAction?.message ?? ''}
        danger
        busy={removingAllergy}
        onConfirm={() => { confirmAction?.run(); setConfirmAction(null); }}
        onCancel={() => setConfirmAction(null)}
      />
    </div>
  );
}

function SummaryCard({ title, value, sub, color, link }: {
  title: string; value: string; sub: string; color: string; link?: string;
}) {
  const colors: Record<string, string> = {
    blue: 'border-blue-200 bg-blue-50',
    green: 'border-green-200 bg-green-50',
    red: 'border-red-200 bg-red-50',
    teal: 'border-teal-200 bg-teal-50',
    gray: 'border-gray-200 bg-gray-50',
  };
  const content = (
    <div className={`rounded-xl border p-4 ${colors[color] ?? colors.gray}`}>
      <p className="text-xs text-gray-500 font-medium uppercase tracking-wide mb-1">{title}</p>
      <p className="text-xl font-bold text-gray-900">{value}</p>
      <p className="text-xs text-gray-500 mt-0.5 truncate">{sub}</p>
    </div>
  );
  return link ? <Link to={link}>{content}</Link> : content;
}

function Row({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div className="flex text-sm">
      <dt className="w-36 text-gray-500 shrink-0">{label}</dt>
      <dd className="text-gray-800">{value || '—'}</dd>
    </div>
  );
}
