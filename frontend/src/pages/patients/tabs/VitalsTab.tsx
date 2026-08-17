import { useState, useEffect } from 'react';
import { getVitalsForPatient, recordVitals } from '../../../api/nursing';
import type { VitalSignsResponse, RecordVitalSignsRequest } from '../../../types/nursing';
import { useAuth } from '../../../contexts/AuthContext';
import { Roles } from '../../../types';

const emptyForm: RecordVitalSignsRequest = {
  bloodPressureSystolic: null, bloodPressureDiastolic: null,
  pulseRate: null, temperature: null, spO2: null,
  respiratoryRate: null, weight: null, height: null, notes: null,
};

export default function VitalsTab({ patientId }: { patientId: string }) {
  const { user } = useAuth();
  const [vitals, setVitals]       = useState<VitalSignsResponse[]>([]);
  const [loading, setLoading]     = useState(true);
  const [showForm, setShowForm]   = useState(false);
  const [form, setForm]           = useState<RecordVitalSignsRequest>(emptyForm);
  const [saving, setSaving]       = useState(false);
  const [saveError, setSaveError] = useState('');

  useEffect(() => {
    getVitalsForPatient(patientId, 50)
      .then((data) => setVitals(Array.isArray(data) ? data : []))
      .catch(() => setVitals([]))
      .finally(() => setLoading(false));
  }, [patientId]);

  const canRecord = [Roles.Nurse, Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user?.role as never);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setSaveError('');
    try {
      const v = await recordVitals(patientId, form);
      setVitals(prev => [v, ...prev]);
      setForm(emptyForm);
      setShowForm(false);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setSaveError(msg || 'Failed to record vitals.');
    } finally { setSaving(false); }
  }

  function num(val: string) { return val === '' ? null : Number(val); }

  if (loading) return <div className="text-gray-400 text-sm">Loading…</div>;

  return (
    <div className="max-w-4xl">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold text-gray-700">Vital Signs ({vitals.length})</h3>
        {canRecord && (
          <button onClick={() => setShowForm(s => !s)}
            className="px-3 py-1.5 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium">
            {showForm ? 'Cancel' : '+ Record Vitals'}
          </button>
        )}
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="bg-white rounded-xl border border-gray-200 p-5 mb-4">
          <h4 className="text-sm font-semibold text-gray-700 mb-4">New Vitals Entry</h4>
          <div className="grid grid-cols-4 gap-4">
            {[
              { label: 'BP Systolic (mmHg)',  key: 'bloodPressureSystolic' },
              { label: 'BP Diastolic (mmHg)', key: 'bloodPressureDiastolic' },
              { label: 'Pulse (bpm)',          key: 'pulseRate' },
              { label: 'Temp (°C)',            key: 'temperature' },
              { label: 'SpO2 (%)',             key: 'spO2' },
              { label: 'Resp Rate (/min)',     key: 'respiratoryRate' },
              { label: 'Weight (kg)',          key: 'weight' },
              { label: 'Height (cm)',          key: 'height' },
            ].map(({ label, key }) => (
              <div key={key}>
                <label htmlFor={`vitals-${key}`} className="block text-xs text-gray-600 mb-1">{label}</label>
                <input id={`vitals-${key}`} type="number" step="any"
                  value={(form as Record<string, number | null>)[key] ?? ''}
                  onChange={e => setForm(f => ({ ...f, [key]: num(e.target.value) }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
              </div>
            ))}
            <div className="col-span-4">
              <label htmlFor="vitals-notes" className="block text-xs text-gray-600 mb-1">Notes</label>
              <input id="vitals-notes" value={form.notes ?? ''}
                onChange={e => setForm(f => ({ ...f, notes: e.target.value || null }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
            </div>
          </div>
          {saveError && <p className="mt-2 text-sm text-red-600">{saveError}</p>}
          <div className="flex justify-end mt-4">
            <button type="submit" disabled={saving}
              className="bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium px-4 py-2 rounded-lg disabled:opacity-50">
              {saving ? 'Saving…' : 'Save Vitals'}
            </button>
          </div>
        </form>
      )}

      {vitals.length === 0 ? (
        <p className="text-sm text-gray-400">No vitals recorded.</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Date / Time</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">BP</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Pulse</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Temp</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">SpO2</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">RR</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Wt/Ht</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">By</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {vitals.map(v => (
                <tr key={v.vitalSignsId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-600 text-xs">{new Date(v.recordedAt).toLocaleString()}</td>
                  <td className="px-4 py-3 text-gray-800">{v.bloodPressureSystolic ? `${v.bloodPressureSystolic}/${v.bloodPressureDiastolic}` : '—'}</td>
                  <td className="px-4 py-3 text-gray-700">{v.pulseRate ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-700">{v.temperature ? `${v.temperature}°C` : '—'}</td>
                  <td className="px-4 py-3 text-gray-700">{v.spO2 ? `${v.spO2}%` : '—'}</td>
                  <td className="px-4 py-3 text-gray-700">{v.respiratoryRate ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-600 text-xs">{v.weight ? `${v.weight}kg` : '—'} / {v.height ? `${v.height}cm` : '—'}</td>
                  <td className="px-4 py-3 text-gray-500 text-xs">{v.recordedByName}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
