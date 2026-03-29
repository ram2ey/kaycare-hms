import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getBill, issueBill, addPayment, cancelBill, voidBill, downloadInvoice, downloadReceipt, applyDiscount, addAdjustment, writeOff } from '../../api/billing';
import type { BillDetailResponse, AddPaymentRequest, ApplyDiscountRequest, AddAdjustmentRequest, WriteOffRequest } from '../../types/billing';
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
  const [showDiscountModal, setShowDiscountModal] = useState(false);
  const [discountForm, setDiscountForm] = useState<ApplyDiscountRequest>({ discountAmount: 0, discountReason: '' });
  const [showAdjustModal, setShowAdjustModal] = useState(false);
  const [adjustForm, setAdjustForm] = useState<AddAdjustmentRequest>({ amount: 0, reason: '' });
  const [showWriteOffModal, setShowWriteOffModal] = useState(false);
  const [writeOffForm, setWriteOffForm] = useState<WriteOffRequest>({ reason: '' });

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

  async function handleDiscount(e: React.FormEvent) {
    e.preventDefault();
    if (!id) return;
    setActing('discount');
    try {
      setBill(await applyDiscount(id, {
        discountAmount: discountForm.discountAmount,
        discountReason: discountForm.discountReason || undefined,
      }));
      setShowDiscountModal(false);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      alert(msg || 'Failed to apply discount.');
    } finally {
      setActing('');
    }
  }

  async function handleAdjustment(e: React.FormEvent) {
    e.preventDefault();
    if (!id) return;
    setActing('adjust');
    try {
      setBill(await addAdjustment(id, adjustForm));
      setShowAdjustModal(false);
      setAdjustForm({ amount: 0, reason: '' });
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      alert(msg || 'Failed to add adjustment.');
    } finally {
      setActing('');
    }
  }

  async function handleWriteOff(e: React.FormEvent) {
    e.preventDefault();
    if (!id) return;
    setActing('writeoff');
    try {
      setBill(await writeOff(id, writeOffForm));
      setShowWriteOffModal(false);
      setWriteOffForm({ reason: '' });
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      alert(msg || 'Failed to write off bill.');
    } finally {
      setActing('');
    }
  }

  const canBill   = user && BILLING_ROLES.includes(user.role as never);
  const canAdmin  = user && [Roles.Admin, Roles.SuperAdmin].includes(user.role as never);

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (error || !bill) return <div className="p-8 text-red-600">{error || 'Bill not found.'}</div>;

  const isIssuable     = bill.status === 'Draft';
  const isPayable      = bill.status === 'Issued' || bill.status === 'PartiallyPaid';
  const isDiscountable = bill.status === 'Draft' || bill.status === 'Issued';
  const isAdjustable   = bill.status === 'Issued' || bill.status === 'PartiallyPaid';
  const isWriteOffable = (bill.status === 'Issued' || bill.status === 'PartiallyPaid') && bill.balanceDue > 0;
  const isCancellable  = bill.status === 'Draft' || bill.status === 'Issued';
  const isVoidable     = bill.status === 'Paid' || bill.status === 'PartiallyPaid';

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
          {canAdmin && isDiscountable && (
            <button
              onClick={() => {
                setDiscountForm({ discountAmount: bill.discountAmount, discountReason: bill.discountReason ?? '' });
                setShowDiscountModal(true);
              }}
              disabled={!!acting}
              className="px-4 py-2 border border-yellow-300 text-yellow-700 hover:bg-yellow-50 text-sm font-medium rounded-lg disabled:opacity-50 transition-colors">
              {bill.discountAmount > 0 ? 'Edit Discount' : 'Apply Discount'}
            </button>
          )}
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
          {canAdmin && isAdjustable && (
            <button onClick={() => { setAdjustForm({ amount: 0, reason: '' }); setShowAdjustModal(true); }} disabled={!!acting}
              className="px-4 py-2 border border-indigo-300 text-indigo-700 hover:bg-indigo-50 text-sm font-medium rounded-lg disabled:opacity-50 transition-colors">
              Adjustment
            </button>
          )}
          {canAdmin && isWriteOffable && (
            <button onClick={() => { setWriteOffForm({ reason: '' }); setShowWriteOffModal(true); }} disabled={!!acting}
              className="px-4 py-2 border border-purple-300 text-purple-700 hover:bg-purple-50 text-sm font-medium rounded-lg disabled:opacity-50 transition-colors">
              Write Off
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
        <div className={`grid gap-4 ${bill.discountAmount > 0 ? 'grid-cols-4' : 'grid-cols-3'}`}>
          <SummaryCard label="Total Amount" value={fmt(bill.totalAmount)} color="text-gray-800" />
          {bill.discountAmount > 0 && (
            <SummaryCard label={`Discount${bill.discountReason ? ` — ${bill.discountReason}` : ''}`} value={`− ${fmt(bill.discountAmount)}`} color="text-green-600" />
          )}
          <SummaryCard label="Paid" value={fmt(bill.paidAmount)} color="text-green-700" />
          <SummaryCard label="Balance Due" value={fmt(bill.balanceDue)} color={bill.balanceDue > 0 ? 'text-red-600' : 'text-gray-400'} />
        </div>

        {/* Payer info */}
        {bill.payerName && (
          <section className="bg-blue-50 border border-blue-100 rounded-xl px-5 py-3 flex items-center gap-3">
            <span className="text-xs font-semibold text-blue-600 uppercase tracking-wide">Payer</span>
            <span className="text-sm font-medium text-blue-800">{bill.payerName}</span>
          </section>
        )}

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

        {/* Adjustments */}
        {bill.adjustments.length > 0 && (
          <section className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
              <h3 className="text-sm font-semibold text-gray-700">
                Adjustments ({bill.adjustments.length})
                <span className={`ml-2 font-mono text-xs ${bill.adjustmentTotal >= 0 ? 'text-red-600' : 'text-green-700'}`}>
                  {bill.adjustmentTotal >= 0 ? '+' : ''}{fmt(bill.adjustmentTotal)}
                </span>
              </h3>
            </div>
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100">
                <tr>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Date</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Reason</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">By</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Amount</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {bill.adjustments.map((a) => (
                  <tr key={a.billAdjustmentId}>
                    <td className="px-5 py-3 text-gray-500 text-xs">
                      {new Date(a.adjustedAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}
                    </td>
                    <td className="px-5 py-3 text-gray-700">{a.reason}</td>
                    <td className="px-5 py-3 text-gray-500">{a.adjustedByName}</td>
                    <td className={`px-5 py-3 text-right font-medium font-mono ${a.amount >= 0 ? 'text-red-600' : 'text-green-700'}`}>
                      {a.amount >= 0 ? '+' : ''}{fmt(a.amount)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        )}

        {/* Write-off banner */}
        {bill.status === 'WrittenOff' && (
          <section className="bg-purple-50 border border-purple-200 rounded-xl px-5 py-3">
            <p className="text-sm font-semibold text-purple-700">Written Off — {fmt(bill.writeOffAmount)}</p>
            {bill.writeOffReason && <p className="text-xs text-purple-500 mt-0.5">{bill.writeOffReason}</p>}
          </section>
        )}

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

      {/* Discount / Waiver modal */}
      {showDiscountModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-1">Apply Discount / Waiver</h3>
            <p className="text-sm text-gray-500 mb-4">
              Bill total: <span className="font-semibold text-gray-700">{fmt(bill.totalAmount)}</span>
              {bill.paidAmount > 0 && (
                <> · Already paid: <span className="font-semibold text-green-700">{fmt(bill.paidAmount)}</span></>
              )}
            </p>

            {/* Quick waiver button */}
            <button
              type="button"
              onClick={() => setDiscountForm((f) => ({ ...f, discountAmount: bill.totalAmount, discountReason: f.discountReason || 'Full waiver' }))}
              className="w-full mb-4 py-2 text-sm font-medium border border-dashed border-yellow-400 text-yellow-700 hover:bg-yellow-50 rounded-lg transition-colors"
            >
              Full Waiver — set to {fmt(bill.totalAmount)}
            </button>

            <form onSubmit={handleDiscount} className="space-y-3">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Discount Amount (GHS) *</label>
                <input
                  required
                  type="number"
                  step="0.01"
                  min={0}
                  max={bill.totalAmount}
                  value={discountForm.discountAmount || ''}
                  onChange={(e) => setDiscountForm((f) => ({ ...f, discountAmount: Number(e.target.value) }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-yellow-400"
                />
                {discountForm.discountAmount > 0 && (
                  <p className="text-xs text-gray-400 mt-1">
                    Net payable: <span className="font-semibold text-gray-700">{fmt(Math.max(0, bill.totalAmount - discountForm.discountAmount))}</span>
                  </p>
                )}
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Reason</label>
                <input
                  value={discountForm.discountReason ?? ''}
                  onChange={(e) => setDiscountForm((f) => ({ ...f, discountReason: e.target.value }))}
                  placeholder="e.g. Staff discount, Charity care, NHIS waiver…"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-yellow-400"
                />
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowDiscountModal(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">
                  Cancel
                </button>
                <button type="submit" disabled={acting === 'discount'}
                  className="px-4 py-2 text-sm font-medium bg-yellow-500 hover:bg-yellow-600 disabled:opacity-50 text-white rounded-lg transition-colors">
                  {acting === 'discount' ? 'Applying…' : 'Apply'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Adjustment modal */}
      {showAdjustModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-1">Post-Issuance Adjustment</h3>
            <p className="text-sm text-gray-500 mb-4">
              Positive = extra charge &nbsp;·&nbsp; Negative = credit reduction
            </p>
            <form onSubmit={handleAdjustment} className="space-y-3">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Amount (GHS) *</label>
                <input
                  required
                  type="number"
                  step="0.01"
                  value={adjustForm.amount || ''}
                  onChange={(e) => setAdjustForm((f) => ({ ...f, amount: Number(e.target.value) }))}
                  placeholder="e.g. 50.00 or -20.00"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
                />
                {adjustForm.amount !== 0 && (
                  <p className="text-xs text-gray-400 mt-1">
                    New balance would be: <span className="font-semibold text-gray-700">{fmt(Math.max(0, bill.balanceDue + adjustForm.amount))}</span>
                  </p>
                )}
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Reason *</label>
                <input
                  required
                  value={adjustForm.reason}
                  onChange={(e) => setAdjustForm((f) => ({ ...f, reason: e.target.value }))}
                  placeholder="e.g. Late fee, Overcharge correction…"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
                />
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowAdjustModal(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">
                  Cancel
                </button>
                <button type="submit" disabled={acting === 'adjust'}
                  className="px-4 py-2 text-sm font-medium bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white rounded-lg transition-colors">
                  {acting === 'adjust' ? 'Saving…' : 'Save Adjustment'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Write-off modal */}
      {showWriteOffModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-1">Write Off Balance</h3>
            <p className="text-sm text-gray-500 mb-4">
              This will write off the remaining balance of{' '}
              <span className="font-semibold text-purple-700">{fmt(bill.balanceDue)}</span> as uncollectable.
              The bill will be marked <span className="font-semibold">WrittenOff</span>.
            </p>
            <form onSubmit={handleWriteOff} className="space-y-3">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Reason *</label>
                <input
                  required
                  value={writeOffForm.reason}
                  onChange={(e) => setWriteOffForm({ reason: e.target.value })}
                  placeholder="e.g. Patient deceased, Bad debt, Charity care…"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-purple-400"
                />
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowWriteOffModal(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">
                  Cancel
                </button>
                <button type="submit" disabled={acting === 'writeoff'}
                  className="px-4 py-2 text-sm font-medium bg-purple-600 hover:bg-purple-700 disabled:opacity-50 text-white rounded-lg transition-colors">
                  {acting === 'writeoff' ? 'Processing…' : 'Confirm Write-Off'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

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
