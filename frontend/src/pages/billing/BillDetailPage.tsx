import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getBill, issueBill, addPayment, cancelBill, voidBill, downloadInvoice, downloadReceipt } from '../../api/billing';
import type { BillDetailResponse, AddPaymentRequest } from '../../types/billing';
import { STATUS_COLORS, PAYMENT_METHODS } from '../../types/billing';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const BILLING_ROLES = [Roles.Admin, Roles.SuperAdmin, Roles.Receptionist];

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }

const emptyPayment: AddPaymentRequest = { amount: 0, paymentMethod: 'Cash', reference: '', notes: '' };

export default function BillDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();

  const [bill, setBill] = useState<BillDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [acting, setActing] = useState('');
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [paymentForm, setPaymentForm] = useState<AddPaymentRequest>(emptyPayment);

  useEffect(() => {
    if (!id) return;
    getBill(id).then(setBill).catch(() => setError('Failed to load bill.')).finally(() => setLoading(false));
  }, [id]);

  async function doAction(action: string, fn: () => Promise<BillDetailResponse>) {
    setActing(action);
    try {
      setBill(await fn());
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      alert(msg || `${action} failed.`);
    } finally {
      setActing('');
    }
  }

  async function handlePayment(e: React.FormEvent) {
    e.preventDefault();
    if (!id) return;
    setActing('pay');
    try {
      setBill(await addPayment(id, paymentForm));
      setShowPaymentModal(false);
      setPaymentForm(emptyPayment);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number; data?: { message?: string } } })?.response?.status;
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      if (status === 400) alert(msg || 'Payment exceeds balance due.');
      else alert(msg || 'Payment failed.');
    } finally {
      setActing('');
    }
  }

  function openBlob(blob: Blob, filename: string) {
    const url = URL.createObjectURL(blob);
    const a   = document.createElement('a');
    a.href = url; a.download = filename; a.click();
    URL.revokeObjectURL(url);
  }

  async function handleDownloadInvoice() {
    if (!id) return;
    setActing('invoice');
    try { openBlob(await downloadInvoice(id), `${bill!.billNumber}.pdf`); }
    catch { alert('Failed to download invoice.'); }
    finally { setActing(''); }
  }

  async function handleDownloadReceipt(paymentId: string, index: number) {
    setActing(`receipt-${paymentId}`);
    try { openBlob(await downloadReceipt(paymentId), `Receipt-${index + 1}.pdf`); }
    catch { alert('Failed to download receipt.'); }
    finally { setActing(''); }
  }

  const canBill   = user && BILLING_ROLES.includes(user.role as never);
  const canAdmin  = user && [Roles.Admin, Roles.SuperAdmin].includes(user.role as never);

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (error || !bill) return <div className="p-8 text-red-600">{error || 'Bill not found.'}</div>;

  const isIssuable  = bill.status === 'Draft';
  const isPayable   = bill.status === 'Issued' || bill.status === 'PartiallyPaid';
  const isCancellable = bill.status === 'Draft' || bill.status === 'Issued';
  const isVoidable  = bill.status === 'Paid' || bill.status === 'PartiallyPaid';

  return (
    <div className="p-6 max-w-4xl">
      {/* Breadcrumb */}
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/billing" className="hover:text-blue-600">Billing</Link>
        <span className="mx-2">/</span>
        <span className="font-mono text-gray-800">{bill.billNumber}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800 font-mono">{bill.billNumber}</h2>
          <p className="text-sm text-gray-500 mt-0.5">
            {bill.patientName} · Created by {bill.createdByName} ·{' '}
            {new Date(bill.createdAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <span className={`text-sm font-medium px-3 py-1.5 rounded-full ${STATUS_COLORS[bill.status] ?? 'bg-gray-100 text-gray-600'}`}>
            {bill.status}
          </span>
          <button onClick={handleDownloadInvoice} disabled={!!acting}
            className="px-4 py-2 border border-gray-300 text-gray-600 hover:bg-gray-50 text-sm font-medium rounded-lg disabled:opacity-50 transition-colors">
            {acting === 'invoice' ? 'Downloading…' : 'Download Invoice'}
          </button>
          {canBill && isIssuable && (
            <button onClick={() => doAction('issue', () => issueBill(id!))} disabled={!!acting}
              className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg disabled:opacity-50 transition-colors">
              {acting === 'issue' ? 'Issuing…' : 'Issue Bill'}
            </button>
          )}
          {canBill && isPayable && (
            <button onClick={() => setShowPaymentModal(true)}
              className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-lg transition-colors">
              Record Payment
            </button>
          )}
          {canAdmin && isCancellable && (
            <button onClick={() => confirm('Cancel this bill?') && doAction('cancel', () => cancelBill(id!))} disabled={!!acting}
              className="px-4 py-2 border border-red-300 text-red-600 hover:bg-red-50 text-sm font-medium rounded-lg disabled:opacity-50 transition-colors">
              {acting === 'cancel' ? 'Cancelling…' : 'Cancel'}
            </button>
          )}
          {canAdmin && isVoidable && (
            <button onClick={() => confirm('Void this bill?') && doAction('void', () => voidBill(id!))} disabled={!!acting}
              className="px-4 py-2 border border-red-300 text-red-600 hover:bg-red-50 text-sm font-medium rounded-lg disabled:opacity-50 transition-colors">
              {acting === 'void' ? 'Voiding…' : 'Void'}
            </button>
          )}
        </div>
      </div>

      <div className="space-y-5">
        {/* Summary cards */}
        <div className="grid grid-cols-3 gap-4">
          <SummaryCard label="Total Amount" value={fmt(bill.totalAmount)} color="text-gray-800" />
          <SummaryCard label="Paid" value={fmt(bill.paidAmount)} color="text-green-700" />
          <SummaryCard label="Balance Due" value={fmt(bill.balanceDue)} color={bill.balanceDue > 0 ? 'text-red-600' : 'text-gray-400'} />
        </div>

        {/* Line items */}
        <section className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
            <h3 className="text-sm font-semibold text-gray-700">Line Items ({bill.items.length})</h3>
          </div>
          <table className="w-full text-sm">
            <thead className="border-b border-gray-100">
              <tr>
                <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Description</th>
                <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Category</th>
                <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Qty</th>
                <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Unit Price</th>
                <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Total</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {bill.items.map((item) => (
                <tr key={item.itemId}>
                  <td className="px-5 py-3 text-gray-800">
                    <span>{item.description}</span>
                    {item.sourceType && (
                      <span title={`Auto-captured from ${item.sourceType}`}
                        className="ml-2 text-xs font-medium px-1.5 py-0.5 bg-indigo-50 text-indigo-600 border border-indigo-100">
                        Auto
                      </span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-gray-500">{item.category ?? '—'}</td>
                  <td className="px-5 py-3 text-right text-gray-600">{item.quantity}</td>
                  <td className="px-5 py-3 text-right text-gray-600">{fmt(item.unitPrice)}</td>
                  <td className="px-5 py-3 text-right font-medium text-gray-800">{fmt(item.totalPrice)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot className="border-t border-gray-200 bg-gray-50">
              <tr>
                <td colSpan={4} className="px-5 py-3 text-right font-semibold text-gray-700">Total</td>
                <td className="px-5 py-3 text-right font-bold text-gray-900">{fmt(bill.totalAmount)}</td>
              </tr>
            </tfoot>
          </table>
        </section>

        {/* Payments */}
        <section className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
            <h3 className="text-sm font-semibold text-gray-700">Payments ({bill.payments.length})</h3>
          </div>
          {bill.payments.length === 0 ? (
            <p className="px-5 py-6 text-sm text-gray-400">No payments recorded.</p>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100">
                <tr>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Date</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Method</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Reference</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Received By</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Amount</th>
                  <th className="px-5 py-2.5"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {bill.payments.map((p, idx) => (
                  <tr key={p.paymentId}>
                    <td className="px-5 py-3 text-gray-600">
                      {new Date(p.paymentDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}
                    </td>
                    <td className="px-5 py-3 text-gray-700">{p.paymentMethod}</td>
                    <td className="px-5 py-3 text-gray-500">{p.reference ?? '—'}</td>
                    <td className="px-5 py-3 text-gray-600">{p.receivedByName}</td>
                    <td className="px-5 py-3 text-right font-medium text-green-700">{fmt(p.amount)}</td>
                    <td className="px-5 py-3 text-right">
                      <button
                        onClick={() => handleDownloadReceipt(p.paymentId, idx)}
                        disabled={acting === `receipt-${p.paymentId}`}
                        className="text-xs text-blue-600 hover:underline disabled:opacity-50"
                      >
                        {acting === `receipt-${p.paymentId}` ? 'Downloading…' : 'Receipt'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="border-t border-gray-200 bg-gray-50">
                <tr>
                  <td colSpan={4} className="px-5 py-3 text-right font-semibold text-gray-700">Total Paid</td>
                  <td className="px-5 py-3 text-right font-bold text-green-700">{fmt(bill.paidAmount)}</td>
                </tr>
              </tfoot>
            </table>
          )}
        </section>

        {bill.notes && (
          <section className="bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-2">Notes</h3>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{bill.notes}</p>
          </section>
        )}

        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex justify-between text-xs text-gray-400">
            <span>Created: {new Date(bill.createdAt).toLocaleString('en-GB')}</span>
            <span>Updated: {new Date(bill.updatedAt).toLocaleString('en-GB')}</span>
          </div>
          <div className="mt-2 flex gap-4">
            <Link to={`/patients/${bill.patientId}`} className="text-sm text-blue-600 hover:underline">Patient Record →</Link>
            {bill.consultationId && (
              <Link to={`/consultations/${bill.consultationId}`} className="text-sm text-blue-600 hover:underline">Consultation →</Link>
            )}
          </div>
        </section>
      </div>

      {/* Payment modal */}
      {showPaymentModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-1">Record Payment</h3>
            <p className="text-sm text-gray-500 mb-4">Balance due: <span className="font-semibold text-red-600">{fmt(bill.balanceDue)}</span></p>
            <form onSubmit={handlePayment} className="space-y-3">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Amount (GHS) *</label>
                <input required type="number" step="0.01" min={0.01} max={bill.balanceDue}
                  value={paymentForm.amount || ''}
                  onChange={(e) => setPaymentForm((f) => ({ ...f, amount: Number(e.target.value) }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Payment Method *</label>
                <select value={paymentForm.paymentMethod}
                  onChange={(e) => setPaymentForm((f) => ({ ...f, paymentMethod: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                  {PAYMENT_METHODS.map((m) => <option key={m}>{m}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Reference</label>
                <input value={paymentForm.reference ?? ''}
                  onChange={(e) => setPaymentForm((f) => ({ ...f, reference: e.target.value }))}
                  placeholder="e.g. transaction ID, cheque #"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Notes</label>
                <input value={paymentForm.notes ?? ''}
                  onChange={(e) => setPaymentForm((f) => ({ ...f, notes: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowPaymentModal(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">
                  Cancel
                </button>
                <button type="submit" disabled={acting === 'pay'}
                  className="px-4 py-2 text-sm font-medium bg-green-600 hover:bg-green-700 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {acting === 'pay' ? 'Saving…' : 'Record Payment'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

function SummaryCard({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-4">
      <p className="text-xs text-gray-500 uppercase font-semibold tracking-wide mb-1">{label}</p>
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
    </div>
  );
}
