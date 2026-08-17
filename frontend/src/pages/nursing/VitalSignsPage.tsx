import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getVitalsForPatient, recordVitals } from '../../api/nursing';
import type { VitalSignsResponse, RecordVitalSignsRequest } from '../../types/nursing';

export default function VitalSignsPage() {
  const { patientId } = useParams<{ patientId: string }>();
  const [vitals, setVitals] = useState<VitalSignsResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState('');
  const [form, setForm] = useState<RecordVitalSignsRequest>({});

  useEffect(() => { load(); }, [patientId]);

  const load = async () => {
    if (!patientId) return;
    setLoading(true);
    try { setVitals(await getVitalsForPatient(patientId)); }
    finally { setLoading(false); }
  };

  const handleSave = async () => {
    if (!patientId) return;
    setSaving(true);
    setSaveError('');
    try {
      await recordVitals(patientId, form);
      setShowModal(false);
      setForm({});
      await load();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setSaveError(msg || 'Failed to record vitals.');
    } finally { setSaving(false); }
  };

  const latest = vitals[0];

  const num = (v: number | null | undefined) =>
    v != null ? <span className="font-semibold text-gray-900">{v}</span> : <span className="text-gray-400">—</span>;

  return (
    <div className="p-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Vital Signs</h1>
        <button onClick={() => setShowModal(true)} className="btn-primary">+ Record Vitals</button>
      </div>

      {/* Latest vitals summary */}
      {latest && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-8">
          {[
            { label: 'Blood Pressure', value: latest.bloodPressureSystolic != null ? `${latest.bloodPressureSystolic}/${latest.bloodPressureDiastolic} mmHg` : null },
            { label: 'Pulse', value: latest.pulseRate != null ? `${latest.pulseRate} bpm` : null },
            { label: 'Temp', value: latest.temperature != null ? `${latest.temperature} °C` : null },
            { label: 'SpO₂', value: latest.spO2 != null ? `${latest.spO2}%` : null },
            { label: 'Resp. Rate', value: latest.respiratoryRate != null ? `${latest.respiratoryRate} /min` : null },
            { label: 'Weight', value: latest.weight != null ? `${latest.weight} kg` : null },
            { label: 'Height', value: latest.height != null ? `${latest.height} cm` : null },
            { label: 'BMI', value: latest.bmi != null ? `${latest.bmi}` : null },
          ].map(({ label, value }) => (
            <div key={label} className="bg-white border border-gray-200 rounded-xl p-4">
              <div className="text-xs text-gray-500 mb-1">{label}</div>
              <div className="font-semibold text-gray-900">{value ?? '—'}</div>
            </div>
          ))}
        </div>
      )}

      {/* History table */}
      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              {['Date / Time', 'BP', 'Pulse', 'Temp', 'SpO₂', 'RR', 'Weight', 'BMI', 'By'].map(h => (
                <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-gray-600">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {loading ? (
              <tr><td colSpan={9} className="px-4 py-8 text-center text-gray-400">Loading…</td></tr>
            ) : vitals.length === 0 ? (
              <tr><td colSpan={9} className="px-4 py-8 text-center text-gray-400">No vitals recorded yet.</td></tr>
            ) : vitals.map(v => (
              <tr key={v.vitalSignsId} className="hover:bg-gray-50">
                <td className="px-4 py-3 text-gray-600 whitespace-nowrap">
                  {new Date(v.recordedAt).toLocaleDateString()}{' '}
                  <span className="text-gray-400">{new Date(v.recordedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                </td>
                <td className="px-4 py-3">{v.bloodPressureSystolic != null ? `${v.bloodPressureSystolic}/${v.bloodPressureDiastolic}` : num(null)}</td>
                <td className="px-4 py-3">{num(v.pulseRate)}</td>
                <td className="px-4 py-3">{num(v.temperature)}</td>
                <td className="px-4 py-3">{v.spO2 != null ? `${v.spO2}%` : '—'}</td>
                <td className="px-4 py-3">{num(v.respiratoryRate)}</td>
                <td className="px-4 py-3">{v.weight != null ? `${v.weight} kg` : '—'}</td>
                <td className="px-4 py-3">{num(v.bmi)}</td>
                <td className="px-4 py-3 text-gray-500">{v.recordedByName}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Record modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Record Vitals</h2>
              <button onClick={() => setShowModal(false)} aria-label="Close" className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="p-6 grid grid-cols-2 gap-4">
              {([
                ['bloodPressureSystolic', 'Systolic BP (mmHg)'],
                ['bloodPressureDiastolic', 'Diastolic BP (mmHg)'],
                ['pulseRate', 'Pulse Rate (bpm)'],
                ['temperature', 'Temperature (°C)'],
                ['spO2', 'SpO₂ (%)'],
                ['respiratoryRate', 'Resp. Rate (/min)'],
                ['weight', 'Weight (kg)'],
                ['height', 'Height (cm)'],
              ] as [keyof RecordVitalSignsRequest, string][]).map(([field, label]) => (
                <div key={field}>
                  <label htmlFor={`vitalsig-${field}`} className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
                  <input
                    id={`vitalsig-${field}`}
                    type="number"
                    step={field === 'temperature' || field === 'weight' || field === 'height' ? '0.1' : '1'}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
                    value={(form[field] as number | undefined) ?? ''}
                    onChange={e => setForm(f => ({ ...f, [field]: e.target.value === '' ? null : Number(e.target.value) }))}
                  />
                </div>
              ))}
              <div className="col-span-2">
                <label htmlFor="vitalsig-notes" className="block text-xs font-medium text-gray-600 mb-1">Notes</label>
                <textarea
                  id="vitalsig-notes"
                  rows={2}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm resize-none"
                  value={form.notes ?? ''}
                  onChange={e => setForm(f => ({ ...f, notes: e.target.value || null }))}
                />
              </div>
            </div>
            {saveError && <p className="px-6 pb-2 text-sm text-red-600">{saveError}</p>}
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => { setShowModal(false); setSaveError(''); }} className="btn-outline">Cancel</button>
              <button onClick={handleSave} disabled={saving} className="btn-primary">
                {saving ? 'Saving…' : 'Save Vitals'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
