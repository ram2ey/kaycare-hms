import { useState, useEffect, useCallback } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import {
  getInpatientBillingSummary,
  addInpatientCharge,
  updateInpatientCharge,
  removeInpatientCharge,
  applyAccommodationCharges,
  generateInpatientBill,
} from '../../api/inpatient';
import type {
  InpatientBillSummaryResponse,
  InpatientChargeResponse,
  SaveInpatientChargeRequest,
} from '../../types/inpatient';
import {
  INPATIENT_CHARGE_CATEGORIES,
  CHARGE_CATEGORY_COLORS,
} from '../../types/inpatient';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const fmt  = (d: string) => new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
const fmtM = (n: number) => n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const today = () => new Date().toISOString().split('T')[0];

const blankForm = (): SaveInpatientChargeRequest => ({
  chargeDate:  today(),
  category:    'Procedure',
  description: '',
  quantity:    1,
  unitPrice:   0,
  notes:       '',
});

export default function InpatientBillingPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const canEdit = [Roles.Admin, Roles.SuperAdmin, Roles.Receptionist].includes(user?.role as any);

  const [summary, setSummary]   = useState<InpatientBillSummaryResponse | null>(null);
  const [loading, setLoading]   = useState(true);
  const [error, setError]       = useState('');
  const [applyingAccom, setApplyingAccom] = useState(false);
  const [generating, setGenerating]       = useState(false);

  // Charge modal
  const [showModal, setShowModal]     = useState(false);
  const [editCharge, setEditCharge]   = useState<InpatientChargeResponse | null>(null);
  const [form, setForm]               = useState<SaveInpatientChargeRequest>(blankForm());
  const [saving, setSaving]           = useState(false);
  const [modalError, setModalError]   = useState('');

  const [deleteId, setDeleteId]       = useState<string | null>(null);
  const [deleting, setDeleting]       = useState(false);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true); setError('');
    try { setSummary(await getInpatientBillingSummary(id)); }
    catch { setError('Failed to load billing summary.'); }
    finally { setLoading(false); }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditCharge(null);
    setForm(blankForm());
    setModalError('');
    setShowModal(true);
  };

  const openEdit = (c: InpatientChargeResponse) => {
    setEditCharge(c);
    setForm({
      chargeDate:  c.chargeDate,
      category:    c.category,
      description: c.description,
      quantity:    c.quantity,
      unitPrice:   c.unitPrice,
      notes:       c.notes ?? '',
    });
    setModalError('');
    setShowModal(true);
  };

  const handleSave = async () => {
    if (!id) return;
    if (!form.description.trim()) { setModalError('Description is required.'); return; }
    if (form.quantity < 1)        { setModalError('Quantity must be at least 1.'); return; }
    if (form.unitPrice < 0)       { setModalError('Unit price cannot be negative.'); return; }

    setSaving(true); setModalError('');
    try {
      if (editCharge) {
        await updateInpatientCharge(id, editCharge.inpatientChargeId, form);
      } else {
        await addInpatientCharge(id, form);
      }
      setShowModal(false);
      await load();
    } catch (e: any) {
      setModalError(e?.response?.data?.message ?? 'Failed to save charge.');
    } finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!id || !deleteId) return;
    setDeleting(true);
    try {
      await removeInpatientCharge(id, deleteId);
      setDeleteId(null);
      await load();
    } catch {
      setError('Failed to delete charge.');
      setDeleteId(null);
    } finally { setDeleting(false); }
  };

  const handleApplyAccom = async () => {
    if (!id) return;
    setApplyingAccom(true); setError('');
    try {
      await applyAccommodationCharges(id);
      await load();
    } catch (e: any) {
      setError(e?.response?.data?.message ?? 'Failed to apply accommodation charges.');
    } finally { setApplyingAccom(false); }
  };

  const handleGenerateBill = async () => {
    if (!id) return;
    setGenerating(true); setError('');
    try {
      const { billId } = await generateInpatientBill(id);
      navigate(`/billing/${billId}`);
    } catch (e: any) {
      setError(e?.response?.data?.message ?? 'Failed to generate bill.');
    } finally { setGenerating(false); }
  };

  const total = (f: SaveInpatientChargeRequest) => (f.quantity * f.unitPrice).toFixed(2);

  if (loading) return <div className="p-6 text-center text-gray-400">Loading…</div>;
  if (error && !summary) return <div className="p-6 text-center text-red-500">{error}</div>;
  if (!summary)          return <div className="p-6 text-center text-gray-400">Not found.</div>;

  const byCategory = summary.charges.reduce<Record<string, InpatientChargeResponse[]>>((acc, c) => {
    (acc[c.category] ??= []).push(c);
    return acc;
  }, {});

  return (
    <div className="p-6 max-w-5xl mx-auto">
      <div className="flex items-center gap-3 mb-2">
        <Link to={`/inpatient/admissions/${id}`} className="text-sm text-gray-500 hover:text-gray-700">
          ← {summary.admissionNumber}
        </Link>
      </div>

      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Inpatient Billing</h1>
          <p className="text-gray-500 mt-1">{summary.patientName} · {summary.patientMrn}</p>
        </div>
        {canEdit && (
          <div className="flex gap-2 flex-wrap justify-end">
            <button
              onClick={handleApplyAccom}
              disabled={applyingAccom}
              className="px-4 py-2 border border-blue-500 text-blue-600 rounded-lg hover:bg-blue-50 text-sm font-medium disabled:opacity-50"
            >
              {applyingAccom ? 'Calculating…' : 'Apply Accommodation'}
            </button>
            <button onClick={openAdd}
              className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">
              + Add Charge
            </button>
            <button
              onClick={handleGenerateBill}
              disabled={generating || summary.charges.length === 0}
              className="px-5 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm font-medium disabled:opacity-50"
            >
              {generating ? 'Generating…' : summary.billId ? 'Update Bill' : 'Generate Bill'}
            </button>
          </div>
        )}
      </div>

      {error && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{error}</div>
      )}

      {/* Totals + Bill status */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-xs text-gray-500 uppercase tracking-wide mb-1">Total Charges</div>
          <div className="text-2xl font-bold text-gray-900">{fmtM(summary.totalCharges)}</div>
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-xs text-gray-500 uppercase tracking-wide mb-1">Charge Items</div>
          <div className="text-2xl font-bold text-gray-900">{summary.charges.length}</div>
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-xs text-gray-500 uppercase tracking-wide mb-1">Bill</div>
          {summary.billId ? (
            <div className="flex items-center gap-2">
              <Link to={`/billing/${summary.billId}`}
                className="text-blue-600 hover:underline font-semibold text-lg">{summary.billNumber}</Link>
              <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded">{summary.billStatus}</span>
            </div>
          ) : (
            <div className="text-gray-400 text-sm pt-1">No bill generated yet</div>
          )}
        </div>
      </div>

      {/* Accommodation breakdown */}
      {summary.accommodationBreakdown.length > 0 && (
        <div className="bg-blue-50 border border-blue-200 rounded-xl p-5 mb-6">
          <h2 className="font-semibold text-blue-800 mb-3 text-sm uppercase tracking-wide">
            Accommodation Breakdown
          </h2>
          <div className="space-y-2">
            {summary.accommodationBreakdown.map((seg, i) => (
              <div key={i} className="flex items-center justify-between text-sm">
                <div className="text-blue-900">
                  <span className="font-medium">{seg.wardName}</span>
                  <span className="text-blue-600 ml-2">({seg.wardType})</span>
                  <span className="text-gray-500 ml-3">{fmt(seg.from)} → {fmt(seg.to)}</span>
                </div>
                <div className="text-blue-900 font-medium">
                  {seg.days} day{seg.days !== 1 ? 's' : ''} × {fmtM(seg.dailyRate)} = <span className="font-bold">{fmtM(seg.total)}</span>
                </div>
              </div>
            ))}
          </div>
          <div className="mt-3 pt-3 border-t border-blue-200 flex justify-end text-sm font-bold text-blue-900">
            Accommodation Total: {fmtM(summary.accommodationBreakdown.reduce((s, x) => s + x.total, 0))}
          </div>
        </div>
      )}

      {/* Charges by category */}
      {summary.charges.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          <p className="text-lg font-medium">No charges recorded</p>
          {canEdit && (
            <p className="text-sm mt-1">
              Click <span className="font-medium text-blue-600">Apply Accommodation</span> to auto-calculate room charges,
              or <span className="font-medium text-blue-600">+ Add Charge</span> to add a manual charge.
            </p>
          )}
        </div>
      ) : (
        <div className="space-y-6">
          {INPATIENT_CHARGE_CATEGORIES.filter(cat => byCategory[cat]?.length).map(cat => (
            <div key={cat} className="bg-white border border-gray-200 rounded-xl overflow-hidden">
              <div className="flex items-center justify-between px-5 py-3 bg-gray-50 border-b border-gray-200">
                <div className="flex items-center gap-2">
                  <span className={`text-xs font-semibold px-2 py-0.5 rounded ${CHARGE_CATEGORY_COLORS[cat] ?? 'bg-gray-100 text-gray-600'}`}>
                    {cat}
                  </span>
                  <span className="text-sm text-gray-500">{byCategory[cat].length} item{byCategory[cat].length !== 1 ? 's' : ''}</span>
                </div>
                <span className="text-sm font-semibold text-gray-800">
                  {fmtM(byCategory[cat].reduce((s, c) => s + c.totalPrice, 0))}
                </span>
              </div>
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-xs text-gray-400 border-b border-gray-100">
                    <th className="px-5 py-2 text-left">Date</th>
                    <th className="px-5 py-2 text-left">Description</th>
                    <th className="px-5 py-2 text-right">Qty</th>
                    <th className="px-5 py-2 text-right">Unit Price</th>
                    <th className="px-5 py-2 text-right">Total</th>
                    <th className="px-5 py-2 text-left">Recorded By</th>
                    {canEdit && <th className="px-5 py-2" />}
                  </tr>
                </thead>
                <tbody>
                  {byCategory[cat].map(c => (
                    <tr key={c.inpatientChargeId} className="border-b border-gray-50 hover:bg-gray-50">
                      <td className="px-5 py-2 text-gray-600 whitespace-nowrap">{fmt(c.chargeDate)}</td>
                      <td className="px-5 py-2 text-gray-800">
                        {c.description}
                        {c.notes && <div className="text-xs text-gray-400 mt-0.5">{c.notes}</div>}
                      </td>
                      <td className="px-5 py-2 text-right text-gray-700">{c.quantity}</td>
                      <td className="px-5 py-2 text-right text-gray-700">{fmtM(c.unitPrice)}</td>
                      <td className="px-5 py-2 text-right font-semibold text-gray-900">{fmtM(c.totalPrice)}</td>
                      <td className="px-5 py-2 text-gray-500 text-xs">{c.createdByName}</td>
                      {canEdit && (
                        <td className="px-5 py-2 whitespace-nowrap">
                          <button onClick={() => openEdit(c)}
                            className="text-xs text-blue-500 hover:underline mr-3">Edit</button>
                          <button onClick={() => setDeleteId(c.inpatientChargeId)}
                            className="text-xs text-red-500 hover:underline">Delete</button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}

          {/* Grand total */}
          <div className="flex justify-end">
            <div className="bg-gray-50 border border-gray-200 rounded-xl px-6 py-4 text-right">
              <div className="text-xs text-gray-500 uppercase tracking-wide">Grand Total</div>
              <div className="text-2xl font-bold text-gray-900 mt-1">{fmtM(summary.totalCharges)}</div>
            </div>
          </div>
        </div>
      )}

      {/* Add / Edit Charge modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">{editCharge ? 'Edit Charge' : 'Add Charge'}</h2>
              <button onClick={() => setShowModal(false)} className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="px-6 py-4 space-y-4">
              {modalError && (
                <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-3 py-2 text-sm">{modalError}</div>
              )}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Date *</label>
                  <input type="date" className={inp} value={form.chargeDate}
                    onChange={e => setForm(f => ({ ...f, chargeDate: e.target.value }))} />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Category *</label>
                  <select className={inp} value={form.category}
                    onChange={e => setForm(f => ({ ...f, category: e.target.value }))}>
                    {INPATIENT_CHARGE_CATEGORIES.filter(c => c !== 'Accommodation').map(c => (
                      <option key={c} value={c}>{c}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Description *</label>
                <input className={inp} value={form.description}
                  onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                  placeholder="e.g. Wound dressing, IV cannulation…" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Quantity *</label>
                  <input type="number" min={1} className={inp} value={form.quantity}
                    onChange={e => setForm(f => ({ ...f, quantity: parseInt(e.target.value) || 1 }))} />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Unit Price *</label>
                  <input type="number" min={0} step="0.01" className={inp} value={form.unitPrice}
                    onChange={e => setForm(f => ({ ...f, unitPrice: parseFloat(e.target.value) || 0 }))} />
                </div>
              </div>
              <div className="text-right text-sm text-gray-600">
                Total: <span className="font-bold text-gray-900">{total(form)}</span>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Notes</label>
                <input className={inp} value={form.notes ?? ''}
                  onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setShowModal(false)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleSave} disabled={saving}
                className="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm font-medium">
                {saving ? 'Saving…' : 'Save Charge'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete confirmation */}
      {deleteId && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm p-6">
            <h2 className="font-semibold text-gray-900 mb-2">Delete Charge?</h2>
            <p className="text-sm text-gray-500 mb-5">This action cannot be undone.</p>
            <div className="flex justify-end gap-3">
              <button onClick={() => setDeleteId(null)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleDelete} disabled={deleting}
                className="px-5 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 text-sm font-medium">
                {deleting ? 'Deleting…' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
