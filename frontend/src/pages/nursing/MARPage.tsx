import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getMARForAdmission, recordAdministration } from '../../api/nursing';
import type { MARItemResponse, RecordAdministrationRequest } from '../../types/nursing';
import { MAR_STATUS_LABELS, MAR_STATUS_COLORS, ADMIN_ROUTES } from '../../types/nursing';

export default function MARPage() {
  const { admissionId } = useParams<{ admissionId: string }>();
  const [items, setItems] = useState<MARItemResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<MARItemResponse | null>(null);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState('');
  const [form, setForm] = useState<Partial<RecordAdministrationRequest>>({ status: 'Given' });

  useEffect(() => { load(); }, [admissionId]);

  const load = async () => {
    if (!admissionId) return;
    setLoading(true);
    try { setItems(await getMARForAdmission(admissionId)); }
    finally { setLoading(false); }
  };

  const openModal = (item: MARItemResponse) => {
    setSelected(item);
    setForm({ status: 'Given', admissionId });
  };

  const handleRecord = async () => {
    if (!selected || !admissionId) return;
    setSaving(true);
    setSaveError('');
    try {
      await recordAdministration({
        prescriptionId: selected.prescriptionId,
        prescriptionItemId: selected.prescriptionItemId,
        patientId: selected.patientId,
        admissionId,
        status: form.status ?? 'Given',
        doseGiven: form.doseGiven ?? null,
        route: form.route ?? null,
        notes: form.notes ?? null,
      } as RecordAdministrationRequest);
      setSelected(null);
      setForm({ status: 'Given' });
      await load();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setSaveError(msg || 'Failed to record administration.');
    } finally { setSaving(false); }
  };

  const MAR_STATUSES = ['Given', 'Held', 'Refused', 'NotAvailable'] as const;

  return (
    <div className="p-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Medication Administration Record</h1>
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Loading…</div>
      ) : items.length === 0 ? (
        <div className="text-center py-12 text-gray-400">No active prescriptions for this admission.</div>
      ) : (
        <div className="space-y-4">
          {items.map(item => (
            <div key={item.prescriptionItemId} className="bg-white border border-gray-200 rounded-xl overflow-hidden">
              <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
                <div>
                  <div className="font-semibold text-gray-900">{item.medicationName} {item.dosage}</div>
                  <div className="text-sm text-gray-500">{item.frequency}{item.instructions ? ` · ${item.instructions}` : ''}</div>
                </div>
                <button
                  onClick={() => openModal(item)}
                  className="btn-primary text-sm py-1.5 px-4"
                >
                  Record
                </button>
              </div>

              {item.administrations.length > 0 ? (
                <table className="w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      {['Date / Time', 'Status', 'Dose Given', 'Route', 'By', 'Notes'].map(h => (
                        <th key={h} className="px-4 py-2 text-left text-xs font-semibold text-gray-500">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {item.administrations.map(a => (
                      <tr key={a.medicationAdministrationId}>
                        <td className="px-4 py-2 text-gray-600 whitespace-nowrap">
                          {new Date(a.administeredAt).toLocaleDateString()}{' '}
                          {new Date(a.administeredAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </td>
                        <td className="px-4 py-2">
                          <span className={`text-xs font-semibold px-2 py-0.5 rounded-full ${MAR_STATUS_COLORS[a.status]}`}>
                            {MAR_STATUS_LABELS[a.status] ?? a.status}
                          </span>
                        </td>
                        <td className="px-4 py-2 text-gray-600">{a.doseGiven ?? '—'}</td>
                        <td className="px-4 py-2 text-gray-600">{a.route ?? '—'}</td>
                        <td className="px-4 py-2 text-gray-600">{a.administeredByName}</td>
                        <td className="px-4 py-2 text-gray-500">{a.notes ?? '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="px-5 py-3 text-sm text-gray-400">No administrations recorded yet.</div>
              )}
            </div>
          ))}
        </div>
      )}

      {selected && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <div>
                <h2 className="font-semibold text-gray-900">Record Administration</h2>
                <p className="text-sm text-gray-500">{selected.medicationName} {selected.dosage}</p>
              </div>
              <button onClick={() => setSelected(null)} aria-label="Close" className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <span id="mar-status-label" className="block text-xs font-medium text-gray-600 mb-2">Status</span>
                <div role="group" aria-labelledby="mar-status-label" className="flex flex-wrap gap-2">
                  {MAR_STATUSES.map(s => (
                    <button
                      key={s}
                      onClick={() => setForm(f => ({ ...f, status: s }))}
                      className={`px-3 py-1.5 rounded-lg text-sm font-medium border transition ${
                        form.status === s
                          ? 'bg-indigo-600 text-white border-indigo-600'
                          : 'bg-white text-gray-600 border-gray-300 hover:border-indigo-400'
                      }`}
                    >
                      {MAR_STATUS_LABELS[s]}
                    </button>
                  ))}
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label htmlFor="mar-dose-given" className="block text-xs font-medium text-gray-600 mb-1">Dose Given</label>
                  <input
                    id="mar-dose-given"
                    type="text"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
                    placeholder="e.g. 500mg"
                    value={form.doseGiven ?? ''}
                    onChange={e => setForm(f => ({ ...f, doseGiven: e.target.value || null }))}
                  />
                </div>
                <div>
                  <label htmlFor="mar-route" className="block text-xs font-medium text-gray-600 mb-1">Route</label>
                  <select
                    id="mar-route"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
                    value={form.route ?? ''}
                    onChange={e => setForm(f => ({ ...f, route: e.target.value || null }))}
                  >
                    <option value="">Select…</option>
                    {ADMIN_ROUTES.map(r => <option key={r} value={r}>{r}</option>)}
                  </select>
                </div>
              </div>
              <div>
                <label htmlFor="mar-notes" className="block text-xs font-medium text-gray-600 mb-1">Notes</label>
                <textarea
                  id="mar-notes"
                  rows={2}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm resize-none"
                  value={form.notes ?? ''}
                  onChange={e => setForm(f => ({ ...f, notes: e.target.value || null }))}
                />
              </div>
            </div>
            {saveError && <p className="px-6 pb-2 text-sm text-red-600">{saveError}</p>}
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => { setSelected(null); setSaveError(''); }} className="btn-outline">Cancel</button>
              <button onClick={handleRecord} disabled={saving} className="btn-primary">
                {saving ? 'Saving…' : 'Record'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
