import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { createPatient } from '../../api/patients';
import type { CreatePatientRequest } from '../../types/patients';

const empty: CreatePatientRequest = {
  firstName: '',
  middleName: '',
  lastName: '',
  dateOfBirth: '',
  gender: 'Male',
  bloodType: '',
  nationalId: '',
  email: '',
  phoneNumber: '',
  alternatePhone: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  state: '',
  postalCode: '',
  country: 'Ghana',
  emergencyContactName: '',
  emergencyContactPhone: '',
  emergencyContactRelation: '',
  insuranceProvider: '',
  insurancePolicyNumber: '',
  insuranceGroupNumber: '',
};

export default function CreatePatientPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState<CreatePatientRequest>(empty);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  function set(field: keyof CreatePatientRequest, value: string) {
    setForm((f) => ({ ...f, [field]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError('');
    try {
      const patient = await createPatient(form);
      navigate(`/patients/${patient.patientId}`);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Failed to register patient.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="p-6 max-w-3xl">
      {/* Breadcrumb */}
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/patients" className="hover:text-blue-600">Patients</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">Register New Patient</span>
      </div>

      <h2 className="text-2xl font-semibold text-gray-800 mb-6">Register New Patient</h2>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Personal */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Personal Information</h3>
          <div className="grid grid-cols-3 gap-4">
            <Field label="First Name *">
              <input required value={form.firstName} onChange={(e) => set('firstName', e.target.value)} className={input} />
            </Field>
            <Field label="Middle Name">
              <input value={form.middleName} onChange={(e) => set('middleName', e.target.value)} className={input} />
            </Field>
            <Field label="Last Name *">
              <input required value={form.lastName} onChange={(e) => set('lastName', e.target.value)} className={input} />
            </Field>
            <Field label="Date of Birth *">
              <input required type="date" value={form.dateOfBirth} onChange={(e) => set('dateOfBirth', e.target.value)} className={input} />
            </Field>
            <Field label="Gender *">
              <select required value={form.gender} onChange={(e) => set('gender', e.target.value)} className={input}>
                {['Male', 'Female', 'Other'].map((g) => <option key={g}>{g}</option>)}
              </select>
            </Field>
            <Field label="Blood Type">
              <select value={form.bloodType} onChange={(e) => set('bloodType', e.target.value)} className={input}>
                <option value="">Unknown</option>
                {['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'].map((b) => <option key={b}>{b}</option>)}
              </select>
            </Field>
            <Field label="National ID">
              <input value={form.nationalId} onChange={(e) => set('nationalId', e.target.value)} className={input} />
            </Field>
          </div>
        </section>

        {/* Contact */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Contact</h3>
          <div className="grid grid-cols-2 gap-4">
            <Field label="Phone Number">
              <input type="tel" value={form.phoneNumber} onChange={(e) => set('phoneNumber', e.target.value)} className={input} />
            </Field>
            <Field label="Alternate Phone">
              <input type="tel" value={form.alternatePhone} onChange={(e) => set('alternatePhone', e.target.value)} className={input} />
            </Field>
            <Field label="Email" className="col-span-2">
              <input type="email" value={form.email} onChange={(e) => set('email', e.target.value)} className={input} />
            </Field>
            <Field label="Address Line 1" className="col-span-2">
              <input value={form.addressLine1} onChange={(e) => set('addressLine1', e.target.value)} className={input} />
            </Field>
            <Field label="Address Line 2" className="col-span-2">
              <input value={form.addressLine2} onChange={(e) => set('addressLine2', e.target.value)} className={input} />
            </Field>
            <Field label="City">
              <input value={form.city} onChange={(e) => set('city', e.target.value)} className={input} />
            </Field>
            <Field label="State / Region">
              <input value={form.state} onChange={(e) => set('state', e.target.value)} className={input} />
            </Field>
            <Field label="Postal Code">
              <input value={form.postalCode} onChange={(e) => set('postalCode', e.target.value)} className={input} />
            </Field>
            <Field label="Country">
              <input value={form.country} onChange={(e) => set('country', e.target.value)} className={input} />
            </Field>
          </div>
        </section>

        {/* Emergency Contact */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Emergency Contact</h3>
          <div className="grid grid-cols-3 gap-4">
            <Field label="Name" className="col-span-1">
              <input value={form.emergencyContactName} onChange={(e) => set('emergencyContactName', e.target.value)} className={input} />
            </Field>
            <Field label="Phone">
              <input type="tel" value={form.emergencyContactPhone} onChange={(e) => set('emergencyContactPhone', e.target.value)} className={input} />
            </Field>
            <Field label="Relation">
              <input value={form.emergencyContactRelation} onChange={(e) => set('emergencyContactRelation', e.target.value)} placeholder="e.g. Spouse" className={input} />
            </Field>
          </div>
        </section>

        {/* Insurance */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Insurance</h3>
          <div className="grid grid-cols-3 gap-4">
            <Field label="Provider">
              <input value={form.insuranceProvider} onChange={(e) => set('insuranceProvider', e.target.value)} className={input} />
            </Field>
            <Field label="Policy Number">
              <input value={form.insurancePolicyNumber} onChange={(e) => set('insurancePolicyNumber', e.target.value)} className={input} />
            </Field>
            <Field label="Group Number">
              <input value={form.insuranceGroupNumber} onChange={(e) => set('insuranceGroupNumber', e.target.value)} className={input} />
            </Field>
          </div>
        </section>

        {error && (
          <p className="text-sm text-red-600 bg-red-50 px-4 py-3 rounded-lg">{error}</p>
        )}

        <div className="flex gap-3 justify-end">
          <Link
            to="/patients"
            className="px-5 py-2 border border-gray-300 rounded-lg text-sm text-gray-600 hover:bg-gray-50 transition-colors"
          >
            Cancel
          </Link>
          <button
            type="submit"
            disabled={saving}
            className="px-5 py-2 bg-blue-700 hover:bg-blue-800 disabled:bg-blue-400 text-white text-sm font-medium rounded-lg transition-colors"
          >
            {saving ? 'Registering…' : 'Register Patient'}
          </button>
        </div>
      </form>
    </div>
  );
}

const input = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

function Field({ label, children, className = '' }: { label: string; children: React.ReactNode; className?: string }) {
  return (
    <div className={className}>
      <label className="block text-xs text-gray-600 mb-1">{label}</label>
      {children}
    </div>
  );
}
