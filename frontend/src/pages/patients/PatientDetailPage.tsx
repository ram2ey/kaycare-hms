import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getPatient, getAllergies, addAllergy, removeAllergy } from '../../api/patients';
import type { PatientDetailResponse, AllergyResponse, AddAllergyRequest } from '../../types/patients';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const SEVERITY_COLORS: Record<string, string> = {
  Mild: 'bg-yellow-100 text-yellow-700',
  Moderate: 'bg-orange-100 text-orange-700',
  Severe: 'bg-red-100 text-red-700',
  'Life-threatening': 'bg-red-200 text-red-900',
};

const emptyAllergy: AddAllergyRequest = {
  allergyType: 'Drug',
  allergenName: '',
  reaction: '',
  severity: 'Mild',
};

export default function PatientDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();

  const [patient, setPatient] = useState<PatientDetailResponse | null>(null);
  const [allergies, setAllergies] = useState<AllergyResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [showAllergyForm, setShowAllergyForm] = useState(false);
  const [allergyForm, setAllergyForm] = useState<AddAllergyRequest>(emptyAllergy);
  const [savingAllergy, setSavingAllergy] = useState(false);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    Promise.all([getPatient(id), getAllergies(id)])
      .then(([p, a]) => {
        setPatient(p);
        setAllergies(a);
      })
      .catch(() => setError('Failed to load patient.'))
      .finally(() => setLoading(false));
  }, [id]);

  async function handleAddAllergy(e: React.FormEvent) {
    e.preventDefault();
    if (!id) return;
    setSavingAllergy(true);
    try {
      const newAllergy = await addAllergy(id, allergyForm);
      setAllergies((prev) => [...prev, newAllergy]);
      setAllergyForm(emptyAllergy);
      setShowAllergyForm(false);
    } catch {
      alert('Failed to add allergy.');
    } finally {
      setSavingAllergy(false);
    }
  }

  async function handleRemoveAllergy(allergyId: string) {
    if (!id || !confirm('Remove this allergy?')) return;
    try {
      await removeAllergy(id, allergyId);
      setAllergies((prev) => prev.filter((a) => a.allergyId !== allergyId));
    } catch {
      alert('Failed to remove allergy.');
    }
  }

  const canManageAllergies = user && [Roles.SuperAdmin, Roles.Admin, Roles.Doctor, Roles.Nurse].includes(user.role as never);
  const canRemoveAllergy = user && [Roles.SuperAdmin, Roles.Admin, Roles.Doctor].includes(user.role as never);

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (error || !patient) return <div className="p-8 text-red-600">{error || 'Patient not found.'}</div>;

  return (
    <div className="p-6 max-w-5xl">
      {/* Breadcrumb */}
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/patients" className="hover:text-blue-600">Patients</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">{patient.fullName}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">{patient.fullName}</h2>
          <p className="font-mono text-sm text-blue-700 mt-0.5">{patient.medicalRecordNumber}</p>
        </div>
        <div className="flex gap-2">
          {patient.hasAllergies && (
            <span className="bg-red-100 text-red-700 text-xs font-medium px-3 py-1.5 rounded-full">
              Has Allergies
            </span>
          )}
          {patient.hasChronicConditions && (
            <span className="bg-orange-100 text-orange-700 text-xs font-medium px-3 py-1.5 rounded-full">
              Chronic Conditions
            </span>
          )}
          {!patient.isActive && (
            <span className="bg-gray-100 text-gray-500 text-xs font-medium px-3 py-1.5 rounded-full">
              Inactive
            </span>
          )}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-5">
        {/* Demographics */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4 uppercase tracking-wide">Demographics</h3>
          <dl className="space-y-2.5">
            <Row label="Date of Birth" value={`${patient.dateOfBirth} (age ${patient.age})`} />
            <Row label="Gender" value={patient.gender} />
            <Row label="Blood Type" value={patient.bloodType} />
            <Row label="National ID" value={patient.nationalId} />
          </dl>
        </section>

        {/* Contact */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4 uppercase tracking-wide">Contact</h3>
          <dl className="space-y-2.5">
            <Row label="Phone" value={patient.phoneNumber} />
            <Row label="Alt. Phone" value={patient.alternatePhone} />
            <Row label="Email" value={patient.email} />
            <Row
              label="Address"
              value={[patient.addressLine1, patient.addressLine2, patient.city, patient.state, patient.country]
                .filter(Boolean)
                .join(', ')}
            />
          </dl>
        </section>

        {/* Emergency Contact */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4 uppercase tracking-wide">Emergency Contact</h3>
          <dl className="space-y-2.5">
            <Row label="Name" value={patient.emergencyContactName} />
            <Row label="Phone" value={patient.emergencyContactPhone} />
            <Row label="Relation" value={patient.emergencyContactRelation} />
          </dl>
        </section>

        {/* Insurance */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4 uppercase tracking-wide">Insurance</h3>
          <dl className="space-y-2.5">
            <Row label="NHIS Number" value={patient.nhisNumber} />
            <Row label="Provider" value={patient.insuranceProvider} />
            <Row label="Policy #" value={patient.insurancePolicyNumber} />
            <Row label="Group #" value={patient.insuranceGroupNumber} />
          </dl>
        </section>

        {/* Allergies — full width */}
        <section className="col-span-2 bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Allergies</h3>
            {canManageAllergies && (
              <button
                onClick={() => setShowAllergyForm((s) => !s)}
                className="text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 font-medium px-3 py-1.5 rounded-lg transition-colors"
              >
                {showAllergyForm ? 'Cancel' : '+ Add Allergy'}
              </button>
            )}
          </div>

          {/* Add allergy form */}
          {showAllergyForm && (
            <form onSubmit={handleAddAllergy} className="grid grid-cols-4 gap-3 mb-4 p-4 bg-gray-50 rounded-lg">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Type</label>
                <select
                  value={allergyForm.allergyType}
                  onChange={(e) => setAllergyForm((f) => ({ ...f, allergyType: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  {['Drug', 'Food', 'Environmental', 'Other'].map((t) => (
                    <option key={t}>{t}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Allergen *</label>
                <input
                  required
                  value={allergyForm.allergenName}
                  onChange={(e) => setAllergyForm((f) => ({ ...f, allergenName: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="e.g. Penicillin"
                />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Severity *</label>
                <select
                  value={allergyForm.severity}
                  onChange={(e) => setAllergyForm((f) => ({ ...f, severity: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  {['Mild', 'Moderate', 'Severe', 'Life-threatening'].map((s) => (
                    <option key={s}>{s}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Reaction</label>
                <input
                  value={allergyForm.reaction}
                  onChange={(e) => setAllergyForm((f) => ({ ...f, reaction: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="e.g. Rash, Anaphylaxis"
                />
              </div>
              <div className="col-span-4 flex justify-end">
                <button
                  type="submit"
                  disabled={savingAllergy}
                  className="bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium px-4 py-2 rounded-lg disabled:opacity-50 transition-colors"
                >
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
                {allergies.map((a) => (
                  <tr key={a.allergyId}>
                    <td className="py-2 text-gray-600">{a.allergyType}</td>
                    <td className="py-2 font-medium text-gray-800">{a.allergenName}</td>
                    <td className="py-2">
                      <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${SEVERITY_COLORS[a.severity] ?? 'bg-gray-100 text-gray-600'}`}>
                        {a.severity}
                      </span>
                    </td>
                    <td className="py-2 text-gray-500">{a.reaction ?? '—'}</td>
                    <td className="py-2 text-gray-400 text-xs">
                      {new Date(a.recordedAt).toLocaleDateString()}
                    </td>
                    {canRemoveAllergy && (
                      <td className="py-2 text-right">
                        <button
                          onClick={() => handleRemoveAllergy(a.allergyId)}
                          className="text-xs text-red-500 hover:text-red-700"
                        >
                          Remove
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div className="flex text-sm">
      <dt className="w-36 text-gray-500 shrink-0">{label}</dt>
      <dd className="text-gray-800">{value || '—'}</dd>
    </div>
  );
}
