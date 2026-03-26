import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { createBill } from '../../api/billing';
import { searchPatients } from '../../api/patients';
import type { CreateBillRequest, BillItemRequest } from '../../types/billing';
import type { PatientResponse } from '../../types/patients';
import { BILL_CATEGORIES } from '../../types/billing';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

const emptyItem = (): BillItemRequest => ({ description: '', category: '', quantity: 1, unitPrice: 0 });

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }

export default function CreateBillPage() {
  const navigate = useNavigate();
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [patientQuery, setPatientQuery] = useState('');
  const [patientResults, setPatientResults] = useState<PatientResponse[]>([]);
  const [searching, setSearching] = useState(false);
  const [notes, setNotes] = useState('');
  const [items, setItems] = useState<BillItemRequest[]>([emptyItem()]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function searchForPatient() {
    if (!patientQuery.trim()) return;
    setSearching(true);
    try {
      const res = await searchPatients({ query: patientQuery, pageSize: 5 });
      setPatientResults(res.items);
    } finally {
      setSearching(false);
    }
  }

  function updateItem(i: number, field: keyof BillItemRequest, value: unknown) {
    setItems((prev) => prev.map((item, idx) => idx === i ? { ...item, [field]: value } : item));
  }

  const totalAmount = items.reduce((s, it) => s + it.quantity * it.unitPrice, 0);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedPatient) { setError('Please select a patient.'); return; }
    setSaving(true);
    setError('');
    const payload: CreateBillRequest = {
      patientId: selectedPatient.patientId,
      notes: notes || undefined,
      items: items.map((it) => ({ ...it, category: it.category || undefined })),
    };
    try {
      const bill = await createBill(payload);
      navigate(`/billing/${bill.billId}`);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Failed to create bill.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="p-6 max-w-3xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/billing" className="hover:text-blue-600">Billing</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">New Bill</span>
      </div>

      <h2 className="text-2xl font-semibold text-gray-800 mb-6">Create Bill</h2>

      <form onSubmit={handleSubmit} className="space-y-5">
        {/* Patient */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Patient</h3>
          {selectedPatient ? (
            <div className="flex items-center justify-between bg-blue-50 rounded-lg px-4 py-3">
              <div>
                <p className="font-medium text-gray-800">{selectedPatient.fullName}</p>
                <p className="text-xs text-blue-600 font-mono">{selectedPatient.medicalRecordNumber}</p>
              </div>
              <button type="button" onClick={() => setSelectedPatient(null)} className="text-xs text-gray-500 hover:text-red-500">Change</button>
            </div>
          ) : (
            <div className="relative">
              <div className="flex gap-2">
                <input type="text" value={patientQuery}
                  onChange={(e) => setPatientQuery(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), searchForPatient())}
                  placeholder="Search patient by name, MRN, or phone…"
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button type="button" onClick={searchForPatient} disabled={searching}
                  className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-sm rounded-lg text-gray-700 transition-colors">
                  {searching ? '…' : 'Search'}
                </button>
              </div>
              {patientResults.length > 0 && (
                <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                  {patientResults.map((p) => (
                    <button key={p.patientId} type="button"
                      onClick={() => { setSelectedPatient(p); setPatientResults([]); setPatientQuery(''); }}
                      className="w-full text-left px-4 py-2.5 hover:bg-gray-50 text-sm border-b border-gray-100 last:border-0">
                      <span className="font-medium">{p.fullName}</span>
                      <span className="text-gray-400 font-mono text-xs ml-2">{p.medicalRecordNumber}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}
        </section>

        {/* Line items */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Line Items</h3>
            <button type="button" onClick={() => setItems((p) => [...p, emptyItem()])}
              className="text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 font-medium px-3 py-1.5 rounded-lg transition-colors">
              + Add Item
            </button>
          </div>

          <div className="space-y-3">
            {items.map((item, i) => (
              <div key={i} className="grid grid-cols-12 gap-2 items-start">
                <div className="col-span-4">
                  {i === 0 && <label className="block text-xs text-gray-500 mb-1">Description *</label>}
                  <input required value={item.description}
                    onChange={(e) => updateItem(i, 'description', e.target.value)}
                    placeholder="e.g. Consultation fee" className={inp} />
                </div>
                <div className="col-span-3">
                  {i === 0 && <label className="block text-xs text-gray-500 mb-1">Category</label>}
                  <select value={item.category ?? ''}
                    onChange={(e) => updateItem(i, 'category', e.target.value)} className={inp}>
                    <option value="">—</option>
                    {BILL_CATEGORIES.map((c) => <option key={c}>{c}</option>)}
                  </select>
                </div>
                <div className="col-span-2">
                  {i === 0 && <label className="block text-xs text-gray-500 mb-1">Qty</label>}
                  <input required type="number" min={1} value={item.quantity}
                    onChange={(e) => updateItem(i, 'quantity', Number(e.target.value))} className={inp} />
                </div>
                <div className="col-span-2">
                  {i === 0 && <label className="block text-xs text-gray-500 mb-1">Unit Price</label>}
                  <input required type="number" step="0.01" min={0} value={item.unitPrice || ''}
                    onChange={(e) => updateItem(i, 'unitPrice', Number(e.target.value))}
                    placeholder="0.00" className={inp} />
                </div>
                <div className="col-span-1 flex items-end pb-0.5">
                  {i === 0 && <div className="h-5" />}
                  {items.length > 1 && (
                    <button type="button" onClick={() => setItems((p) => p.filter((_, idx) => idx !== i))}
                      className="w-full text-red-400 hover:text-red-600 text-lg leading-none mt-1">×</button>
                  )}
                </div>
              </div>
            ))}
          </div>

          <div className="mt-4 pt-4 border-t border-gray-100 flex justify-end">
            <div className="text-sm">
              <span className="text-gray-500 mr-3">Total:</span>
              <span className="text-xl font-bold text-gray-900">{fmt(totalAmount)}</span>
            </div>
          </div>
        </section>

        {/* Notes */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-3">Notes</h3>
          <textarea rows={3} value={notes} onChange={(e) => setNotes(e.target.value)}
            placeholder="Optional billing notes…" className={`${inp} resize-none`} />
        </section>

        {error && <p className="text-sm text-red-600 bg-red-50 px-4 py-3 rounded-lg">{error}</p>}

        <div className="flex gap-3 justify-end">
          <Link to="/billing" className="px-5 py-2 border border-gray-300 rounded-lg text-sm text-gray-600 hover:bg-gray-50 transition-colors">
            Cancel
          </Link>
          <button type="submit" disabled={saving}
            className="px-5 py-2 bg-blue-700 hover:bg-blue-800 disabled:bg-blue-400 text-white text-sm font-medium rounded-lg transition-colors">
            {saving ? 'Creating…' : 'Create Bill'}
          </button>
        </div>
      </form>
    </div>
  );
}
