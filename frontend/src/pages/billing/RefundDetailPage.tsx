import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getRefund, processRefund, cancelRefund } from '../../api/refunds';
import type { RefundResponse } from '../../types/refunds';
import { REFUND_METHOD_LABELS } from '../../types/refunds';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const STATUS_COLORS: Record<string, string> = {
  Pending:   'bg-yellow-100 text-yellow-700',
  Processed: 'bg-green-100 text-green-700',
  Cancelled: 'bg-gray-100 text-gray-500',
};

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }
function fmtDate(s: string | null) {
  if (!s) return '—';
  return new Date(s).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

export default function RefundDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const isAdmin = user?.role === Roles.Admin || user?.role === Roles.SuperAdmin;

  const [refund, setRefund]   = useState<RefundResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');
  const [busy, setBusy]       = useState(false);

  useEffect(() => {
    if (!id) return;
    getRefund(id).then(setRefund).catch(() => setError('Refund not found.')).finally(() => setLoading(false));
  }, [id]);

  async function handle(fn: () => Promise<RefundResponse>) {
    setBusy(true);
    setError('');
    try { setRefund(await fn()); }
    catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Action failed.');
    } finally { setBusy(false); }
  }

  if (loading) return <div className="p-8 text-center text-gray-500">Loading…</div>;
  if (!refund) return <div className="p-8 text-center text-red-500">{error || 'Not found.'}</div>;

  const btn = 'px-4 py-2 rounded text-sm font-medium transition-colors disabled:opacity-50';

  return (
    <div className="max-w-3xl mx-auto p-6 space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <h1 className="text-2xl font-bold text-gray-900 font-mono">{refund.refundNumber}</h1>
            <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${STATUS_COLORS[refund.status] ?? 'bg-gray-100'}`}>
              {refund.status}
            </span>
          </div>
          <p className="text-sm text-gray-500">Created {fmtDate(refund.createdAt)} by {refund.createdByName}</p>
        </div>

        {isAdmin && refund.status === 'Pending' && (
          <div className="flex gap-2">
            <button onClick={() => confirm('Mark this refund as processed?') && handle(() => processRefund(id!))} disabled={busy}
              className={`${btn} bg-green-600 text-white hover:bg-green-700`}>Mark Processed</button>
            <button onClick={() => confirm('Cancel this refund?') && handle(() => cancelRefund(id!))} disabled={busy}
              className={`${btn} bg-white border border-gray-300 text-gray-600 hover:bg-gray-50`}>Cancel</button>
          </div>
        )}
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded text-sm">{error}</div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Patient</h2>
          <p className="font-semibold">{refund.patientName}</p>
          <p className="text-sm text-gray-600">{refund.patientMrn}</p>
        </div>

        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Invoice</h2>
          <Link to={`/billing/${refund.billId}`} className="font-mono text-blue-600 hover:underline font-semibold">
            {refund.billNumber}
          </Link>
          {refund.creditNoteNumber && (
            <p className="text-xs text-gray-500 mt-1">
              Credit Note:{' '}
              <Link to={`/billing/credit-notes/${refund.creditNoteId}`} className="text-blue-600 hover:underline">
                {refund.creditNoteNumber}
              </Link>
            </p>
          )}
        </div>

        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Refund Amount</h2>
          <p className="text-2xl font-bold text-orange-600">{fmt(refund.amount)}</p>
          <p className="text-sm text-gray-600 mt-1">{REFUND_METHOD_LABELS[refund.refundMethod] ?? refund.refundMethod}</p>
          {refund.reference && <p className="text-sm text-gray-500">Ref: {refund.reference}</p>}
        </div>

        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Timeline</h2>
          <div className="space-y-1 text-sm">
            <div className="flex gap-4"><span className="text-gray-500 w-24">Created</span><span>{fmtDate(refund.createdAt)}</span></div>
            {refund.processedAt && (
              <div className="flex gap-4">
                <span className="text-gray-500 w-24">Processed</span>
                <span>{fmtDate(refund.processedAt)}{refund.processedByName ? ` by ${refund.processedByName}` : ''}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg p-5">
        <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Reason</h2>
        <p className="text-gray-800">{refund.reason}</p>
      </div>

      {refund.notes && (
        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Notes</h2>
          <p className="text-gray-700 text-sm">{refund.notes}</p>
        </div>
      )}
    </div>
  );
}
