import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { createReferral, sendReferral } from '../../api/referrals';
import { getUsers } from '../../api/users';
import { searchPatients } from '../../api/patients';
import type { CreateReferralRequest } from '../../types/referrals';
import type { UserResponse } from '../../types/users';
import type { PatientResponse } from '../../types/patients';
import { safeArray } from '../../utils/array';


const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

const empty: CreateReferralRequest = {
  patientId: '',
  referralType: 'Internal',
  urgency: 'Routine',
  reason: '',
};

export default function CreateReferralPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const prefillPatient = searchParams.get('patientId') ?? '';
  const prefillConsult = searchParams.get('consultationId') ?? '';

  const [form, setForm]     = useState<CreateReferralRequest>({ ...empty, patientId: prefillPatient, consultationId: prefillConsult || undefined });
  const [doctors, setDoctors]   = useState<UserResponse[]>([]);
  const [patients, setPatients] = useState<PatientResponse[]>([]);
  const [saving, setSaving]     = useState(false);
  const [error, setError]       = useState('');

  useEffect(() => {
    getUsers({ role: 'Doctor' }).then(setDoctors).catch(() => setError('Failed to load doctors list. Please try again.'));
    if (!prefillPatient) {
      searchPatients({ page: 1, pageSize: 200 }).then(r => setPatients(safeArray(r.items))).catch(() => setError('Failed to load patients list. Please try again.'));
    }
  }, [prefillPatient]);

  const set = (k: keyof CreateReferralRequest, v: string) =>
    setForm(f => ({ ...f, [k]: v || undefined }));

  const handleSubmit = async (e: React.FormEvent, sendNow: boolean) => {
    e.preventDefault();
    if (!form.patientId) { setError('Patient is required.'); return; }
    if (!form.reason)    { setError('Reason is required.'); return; }
    setSaving(true); setError('');
    try {
      const ref = await createReferral(form);
      if (sendNow) {
        await sendReferral(ref.referralId);
      }
      navigate(`/referrals/${ref.referralId}`);
    } catch (err: any) {
      setError(err?.response?.data?.message ?? 'Failed to create referral.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/referrals" className="hover:text-blue-600">Referrals</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">New Referral</span>
      </div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">New Referral</h1>

      {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-600 rounded-lg px-4 py-3 text-sm">{error}</div>}

      <form className="space-y-5 bg-white border border-gray-200 rounded-xl p-6">
        {/* Patient */}
        <div>
          <label htmlFor="referral-patient" className="block text-xs font-medium text-gray-600 mb-1">Patient *</label>
          {prefillPatient ? (
            <input id="referral-patient" className={inp} value={form.patientId} readOnly />
          ) : (
            <select id="referral-patient" className={inp} value={form.patientId} onChange={e => set('patientId', e.target.value)} required>
              <option value="">Select patient…</option>
              {patients.map(p => (
                <option key={p.patientId} value={p.patientId}>{p.fullName} — {p.medicalRecordNumber}</option>
              ))}
            </select>
          )}
        </div>

        {/* Referral type */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor="referral-type" className="block text-xs font-medium text-gray-600 mb-1">Referral Type *</label>
            <select id="referral-type" className={inp} value={form.referralType} onChange={e => setForm(f => ({ ...f, referralType: e.target.value }))}>
              <option value="Internal">Internal</option>
              <option value="External">External</option>
            </select>
          </div>
          <div>
            <label htmlFor="referral-urgency" className="block text-xs font-medium text-gray-600 mb-1">Urgency *</label>
            <select id="referral-urgency" className={inp} value={form.urgency} onChange={e => setForm(f => ({ ...f, urgency: e.target.value }))}>
              <option value="Routine">Routine</option>
              <option value="Urgent">Urgent</option>
              <option value="Emergency">Emergency</option>
            </select>
          </div>
        </div>

        {/* Refer to */}
        {form.referralType === 'Internal' ? (
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="referral-doctor" className="block text-xs font-medium text-gray-600 mb-1">Referred-to Doctor</label>
              <select id="referral-doctor" className={inp} value={form.referredToDoctorUserId ?? ''} onChange={e => set('referredToDoctorUserId', e.target.value)}>
                <option value="">Select doctor…</option>
                {doctors.map(d => (
                  <option key={d.userId} value={d.userId}>{d.fullName}</option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="referral-department" className="block text-xs font-medium text-gray-600 mb-1">Department</label>
              <input id="referral-department" className={inp} placeholder="e.g. Cardiology" value={form.referredToDepartment ?? ''} onChange={e => set('referredToDepartment', e.target.value)} />
            </div>
          </div>
        ) : (
          <div>
            <label htmlFor="referral-external-facility" className="block text-xs font-medium text-gray-600 mb-1">External Facility</label>
            <input id="referral-external-facility" className={inp} placeholder="e.g. Korle-Bu Teaching Hospital" value={form.externalFacility ?? ''} onChange={e => set('externalFacility', e.target.value)} />
          </div>
        )}

        {/* Reason */}
        <div>
          <label htmlFor="referral-reason" className="block text-xs font-medium text-gray-600 mb-1">Reason for Referral *</label>
          <textarea id="referral-reason" className={`${inp} resize-none`} rows={3} required value={form.reason} onChange={e => setForm(f => ({ ...f, reason: e.target.value }))} placeholder="Clinical indication for referral…" />
        </div>

        {/* Clinical notes */}
        <div>
          <label htmlFor="referral-clinical-notes" className="block text-xs font-medium text-gray-600 mb-1">Clinical Notes / History</label>
          <textarea id="referral-clinical-notes" className={`${inp} resize-none`} rows={4} value={form.clinicalNotes ?? ''} onChange={e => set('clinicalNotes', e.target.value)} placeholder="Relevant history, findings, current medications…" />
        </div>

        <div className="flex justify-end gap-3 pt-2">
          <button type="button" onClick={() => navigate(-1)} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800">Cancel</button>
          <button type="submit" disabled={saving} onClick={e => handleSubmit(e, false)} className="px-4 py-2 border border-blue-600 text-blue-700 rounded-lg text-sm font-medium hover:bg-blue-50 disabled:opacity-50">
            {saving ? 'Saving…' : 'Save as Draft'}
          </button>
          <button type="submit" disabled={saving} onClick={e => handleSubmit(e, true)} className="px-5 py-2 bg-blue-700 text-white rounded-lg text-sm font-medium hover:bg-blue-800 disabled:opacity-50">
            {saving ? 'Saving…' : 'Save & Send'}
          </button>
        </div>
      </form>
    </div>
  );
}
