import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { createBill } from '../../api/billing';
import { getCatalog } from '../../api/serviceCatalog';
import { searchPatients } from '../../api/patients';
import { getPayers } from '../../api/payers';
import type { CreateBillRequest, BillItemRequest } from '../../types/billing';
import type { PatientResponse } from '../../types/patients';
import type { ServiceCatalogItem } from '../../types/serviceCatalog';
import type { PayerResponse } from '../../types/payers';
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
  const [payerId, setPayerId] = useState('');
  const [discountAmount, setDiscountAmount] = useState(0);
  const [discountReason, setDiscountReason] = useState('');
  const [items, setItems] = useState<BillItemRequest[]>([emptyItem()]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  // Catalog picker
  const [catalog, setCatalog] = useState<ServiceCatalogItem[]>([]);
  const [showCatalog, setShowCatalog] = useState(false);
  const [catalogSearch, setCatalogSearch] = useState('');

  // Payers
  const [payers, setPayers] = useState<PayerResponse[]>([]);

  useEffect(() => {
    getCatalog(true).then(setCatalog).catch(() => {});
    getPayers(true).then(setPayers).catch(() => {});
  }, []);

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

  function addFromCatalog(catalogItem: ServiceCatalogItem) {
    // If last row is still empty, replace it; otherwise append
    setItems((prev) => {
      const last = prev[prev.length - 1];
      const newItem: BillItemRequest = {
        description: catalogItem.name,
        category: catalogItem.category,
        quantity: 1,
        unitPrice: catalogItem.unitPrice,
      };
      if (!last.description && last.quantity === 1 && last.unitPrice === 0) {
        return [...prev.slice(0, -1), newItem];
      }
      return [...prev, newItem];
    });
    setShowCatalog(false);
    setCatalogSearch('');
  }

  const filteredCatalog = catalogSearch
    ? catalog.filter((c) =>
        c.name.toLowerCase().includes(catalogSearch.toLowerCase()) ||
        c.category.toLowerCase().includes(catalogSearch.toLowerCase())
      )
    : catalog;

  const totalAmount = items.reduce((s, it) => s + it.quantity * it.unitPrice, 0);
  const netAmount   = Math.max(0, totalAmount - discountAmount);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedPatient) { setError('Please select a patient.'); return; }
    setSaving(true);
    setError('');
    const payload: CreateBillRequest = {
      patientId:      selectedPatient.patientId,
      payerId:        payerId || undefined,
      discountAmount: discountAmount || undefined,
      discountReason: discountReason || undefined,
      notes:          notes || undefined,
      items:          items.map((it) => ({ ...it, category: it.category || undefined })),
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

        {/* Payer & Discount */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Payer & Discount</h3>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs text-gray-500 mb-1">Payer / Insurance</label>
              <select value={payerId} onChange={(e) => setPayerId(e.target.value)} className={inp}>
                <option value="">— Self-pay —</option>
                {payers.map((p) => (
                  <option key={p.payerId} value={p.payerId}>{p.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs text-gray-500 mb-1">Discount Amount (GHS)</label>
              <input type="number" step="0.01" min={0} value={discountAmount || ''}
                onChange={(e) => setDiscountAmount(Number(e.target.value))}
                placeholder="0.00" className={inp} />
            </div>
          </div>
          {discountAmount > 0 && (
            <div className="mt-3">
              <label className="block text-xs text-gray-500 mb-1">Discount Reason</label>
              <input value={discountReason} onChange={(e) => setDiscountReason(e.target.value)}
                placeholder="e.g. Staff discount, NHIS waiver…" className={inp} />
            </div>
          )}
        </section>

        {/* Line items */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Line Items</h3>
            <div className="flex gap-2">
              {catalog.length > 0 && (
                <button type="button" onClick={() => setShowCatalog(true)}
                  className="text-xs bg-gray-50 hover:bg-gray-100 text-gray-700 font-medium px-3 py-1.5 rounded-lg border border-gray-300 transition-colors">
                  From Catalog
                </button>
              )}
              <button type="button" onClick={() => setItems((p) => [...p, emptyItem()])}
                className="text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 font-medium px-3 py-1.5 rounded-lg transition-colors">
                + Add Item
              </button>
            </div>
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
            <div className="text-sm text-right space-y-1">
              <div className="flex gap-6 justify-end">
                <span className="text-gray-500">Subtotal:</span>
                <span className="font-medium text-gray-800 w-24 text-right">{fmt(totalAmount)}</span>
              </div>
              {discountAmount > 0 && (
                <div className="flex gap-6 justify-end text-green-700">
                  <span>Discount:</span>
                  <span className="font-medium w-24 text-right">− {fmt(discountAmount)}</span>
                </div>
              )}
              <div className="flex gap-6 justify-end border-t border-gray-200 pt-1">
                <span className="text-gray-700 font-semibold">Net Total:</span>
                <span className="text-xl font-bold text-gray-900 w-24 text-right">{fmt(netAmount)}</span>
              </div>
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

      {/* Catalog picker modal */}
      {showCatalog && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg max-h-[80vh] flex flex-col">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-800">Price Catalog</h3>
              <button onClick={() => { setShowCatalog(false); setCatalogSearch(''); }}
                className="text-gray-400 hover:text-gray-600 text-xl leading-none">×</button>
            </div>
            <input
              type="text"
              value={catalogSearch}
              onChange={(e) => setCatalogSearch(e.target.value)}
              placeholder="Search by name or category…"
              autoFocus
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 mb-3"
            />
            <div className="overflow-y-auto flex-1 -mx-1">
              {filteredCatalog.length === 0 ? (
                <p className="text-sm text-gray-400 px-1 py-4">No items match.</p>
              ) : (
                filteredCatalog.map((item) => (
                  <button
                    key={item.serviceCatalogItemId}
                    type="button"
                    onClick={() => addFromCatalog(item)}
                    className="w-full text-left px-3 py-3 hover:bg-blue-50 rounded-lg transition-colors flex items-center justify-between group"
                  >
                    <div>
                      <p className="text-sm font-medium text-gray-800 group-hover:text-blue-700">{item.name}</p>
                      <p className="text-xs text-gray-400">{item.category}</p>
                    </div>
                    <span className="text-sm font-mono font-semibold text-gray-700 group-hover:text-blue-700">
                      {fmt(item.unitPrice)}
                    </span>
                  </button>
                ))
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
