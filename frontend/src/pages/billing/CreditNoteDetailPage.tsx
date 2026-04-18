import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getCreditNote, approveCreditNote, applyCreditNote, voidCreditNote } from '../../api/creditNotes';
import type { CreditNoteResponse } from '../../types/creditNotes';
import { CREDIT_NOTE_STATUS_LABELS } from '../../types/creditNotes';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const STATUS_COLORS: Record<string, string> = {
  Draft:    'bg-gray-100 text-gray-700',
  Approved: 'bg-blue-100 text-blue-700',
  Applied:  'bg-green-100 text-green-700',
  Voided:   'bg-gray-100 text-gray-400',
};

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }
function fmtDate(s: string | null) {
  if (!s) return '—';
  return new Date(s).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

export default function CreditNoteDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const isAdmin = user?.role === Roles.Admin || user?.role === Roles.SuperAdmin;

  const [cn, setCn]           = useState<CreditNoteResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');
  const [busy, setBusy]       = useState(false);

  useEffect(() => {
    if (!id) return;
    getCreditNote(id).then(setCn).catch(() => setError('Credit note not found.')).finally(() => setLoading(false));
  }, [id]);

  async function handle(fn: () => Promise<CreditNoteResponse>) {
    setBusy(true);
    setError('');
    try { setCn(await fn()); }
    catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Action failed.');
    } finally { setBusy(false); }
  }

  if (loading) return <div className="p-8 text-center text-gray-500">Loading…</div>;
  if (!cn)     return <div className="p-8 text-center text-red-500">{error || 'Not found.'}</div>;

  const btn = 'px-4 py-2 rounded text-sm font-medium transition-colors disabled:opacity-50';

  return (
    <div className="max-w-3xl mx-auto p-6 space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <h1 className="text-2xl font-bold text-gray-900 font-mono">{cn.creditNoteNumber}</h1>
            <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${STATUS_COLORS[cn.status] ?? 'bg-gray-100'}`}>
              {CREDIT_NOTE_STATUS_LABELS[cn.status] ?? cn.status}
            </span>
          </div>
          <p className="text-sm text-gray-500">Created {fmtDate(cn.createdAt)} by {cn.createdByName}</p>
        </div>

        {isAdmin && (
          <div className="flex gap-2 flex-wrap justify-end">
            {cn.status === 'Draft' && (
              <>
                <button onClick={() => handle(() => approveCreditNote(id!))} disabled={busy}
                  className={`${btn} bg-blue-600 text-white hover:bg-blue-700`}>Approve</button>
                <button onClick={() => confirm('Void this credit note?') && handle(() => voidCreditNote(id!))} disabled={busy}
                  className={`${btn} bg-white border border-gray-300 text-gray-600 hover:bg-gray-50`}>Void</button>
              </>
            )}
            {cn.status === 'Approved' && (
              <>
                <button onClick={() => confirm('Apply this credit note to the bill?') && handle(() => applyCreditNote(id!))} disabled={busy}
                  className={`${btn} bg-green-600 text-white hover:bg-green-700`}>Apply to Bill</button>
                <button onClick={() => confirm('Void this credit note?') && handle(() => voidCreditNote(id!))} disabled={busy}
                  className={`${btn} bg-white border border-gray-300 text-gray-600 hover:bg-gray-50`}>Void</button>
              </>
            )}
          </div>
        )}
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded text-sm">{error}</div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Patient</h2>
          <p className="font-semibold">{cn.patientName}</p>
          <p className="text-sm text-gray-600">{cn.patientMrn}</p>
        </div>

        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Invoice</h2>
          <Link to={`/billing/${cn.billId}`} className="font-mono text-blue-600 hover:underline font-semibold">
            {cn.billNumber}
          </Link>
        </div>

        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Credit Amount</h2>
          <p className="text-2xl font-bold text-green-700">{fmt(cn.amount)}</p>
        </div>

        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Timeline</h2>
          <div className="space-y-1 text-sm">
            <div className="flex gap-4"><span className="text-gray-500 w-24">Created</span><span>{fmtDate(cn.createdAt)}</span></div>
            {cn.approvedAt && (
              <div className="flex gap-4">
                <span className="text-gray-500 w-24">Approved</span>
                <span>{fmtDate(cn.approvedAt)}{cn.approvedByName ? ` by ${cn.approvedByName}` : ''}</span>
              </div>
            )}
            {cn.appliedAt && (
              <div className="flex gap-4"><span className="text-gray-500 w-24">Applied</span><span>{fmtDate(cn.appliedAt)}</span></div>
            )}
          </div>
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg p-5">
        <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Reason</h2>
        <p className="text-gray-800">{cn.reason}</p>
      </div>

      {cn.notes && (
        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Notes</h2>
          <p className="text-gray-700 text-sm">{cn.notes}</p>
        </div>
      )}

      {cn.status === 'Applied' && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4 text-sm text-green-800">
          Credit of {fmt(cn.amount)} has been applied to invoice{' '}
          <Link to={`/billing/${cn.billId}`} className="font-semibold underline">{cn.billNumber}</Link>.
        </div>
      )}
    </div>
  );
}
