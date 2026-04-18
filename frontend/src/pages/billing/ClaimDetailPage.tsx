import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  getClaim,
  submitClaim,
  approveClaim,
  rejectClaim,
  cancelClaim,
  downloadClaimPdf,
} from '../../api/claims';
import type { InsuranceClaimResponse } from '../../types/claims';
import { CLAIM_STATUS_LABELS } from '../../types/claims';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const STATUS_COLORS: Record<string, string> = {
  Draft:             'bg-gray-100 text-gray-700',
  Submitted:         'bg-blue-100 text-blue-700',
  Approved:          'bg-green-100 text-green-700',
  PartiallyApproved: 'bg-yellow-100 text-yellow-700',
  Rejected:          'bg-red-100 text-red-700',
  Cancelled:         'bg-gray-100 text-gray-500',
};

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }
function fmtDate(s: string | null) {
  if (!s) return '—';
  return new Date(s).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

export default function ClaimDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const isAdmin = user?.role === Roles.Admin || user?.role === Roles.SuperAdmin;
  const canManage = isAdmin || user?.role === Roles.Receptionist;

  const [claim, setClaim]         = useState<InsuranceClaimResponse | null>(null);
  const [loading, setLoading]     = useState(true);
  const [error, setError]         = useState('');
  const [busy, setBusy]           = useState(false);

  // Approve modal
  const [showApprove, setShowApprove]           = useState(false);
  const [approvedAmount, setApprovedAmount]     = useState('');
  const [approveNotes, setApproveNotes]         = useState('');

  // Reject modal
  const [showReject, setShowReject]             = useState(false);
  const [rejectionReason, setRejectionReason]   = useState('');
  const [rejectNotes, setRejectNotes]           = useState('');

  useEffect(() => {
    if (!id) return;
    getClaim(id).then(setClaim).catch(() => setError('Claim not found.')).finally(() => setLoading(false));
  }, [id]);

  async function handleSubmit() {
    if (!id || !claim) return;
    setBusy(true);
    try {
      setClaim(await submitClaim(id));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to submit claim.');
    } finally {
      setBusy(false);
    }
  }

  async function handleApprove() {
    if (!id) return;
    const amount = parseFloat(approvedAmount);
    if (isNaN(amount) || amount <= 0) { setError('Enter a valid approved amount.'); return; }
    setBusy(true);
    try {
      setClaim(await approveClaim(id, { approvedAmount: amount, notes: approveNotes || undefined }));
      setShowApprove(false);
      setApprovedAmount('');
      setApproveNotes('');
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to approve claim.');
    } finally {
      setBusy(false);
    }
  }

  async function handleReject() {
    if (!id || !rejectionReason.trim()) { setError('Rejection reason is required.'); return; }
    setBusy(true);
    try {
      setClaim(await rejectClaim(id, { rejectionReason: rejectionReason.trim(), notes: rejectNotes || undefined }));
      setShowReject(false);
      setRejectionReason('');
      setRejectNotes('');
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to reject claim.');
    } finally {
      setBusy(false);
    }
  }

  async function handleCancel() {
    if (!id || !confirm('Cancel this claim?')) return;
    setBusy(true);
    try {
      setClaim(await cancelClaim(id));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to cancel claim.');
    } finally {
      setBusy(false);
    }
  }

  async function handleDownloadPdf() {
    if (!id) return;
    try {
      const blob = await downloadClaimPdf(id);
      const url  = URL.createObjectURL(blob);
      const a    = document.createElement('a');
      a.href     = url;
      a.download = `claim-${claim?.claimNumber ?? id}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setError('Failed to download PDF.');
    }
  }

  if (loading) return <div className="p-8 text-center text-gray-500">Loading…</div>;
  if (!claim)  return <div className="p-8 text-center text-red-500">{error || 'Claim not found.'}</div>;

  const inp = 'border border-gray-300 rounded px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-blue-500';
  const btn = 'px-4 py-2 rounded text-sm font-medium transition-colors disabled:opacity-50';

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <h1 className="text-2xl font-bold text-gray-900 font-mono">{claim.claimNumber}</h1>
            <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${STATUS_COLORS[claim.status] ?? 'bg-gray-100 text-gray-700'}`}>
              {CLAIM_STATUS_LABELS[claim.status] ?? claim.status}
            </span>
          </div>
          <p className="text-sm text-gray-500">
            Created {fmtDate(claim.createdAt)} by {claim.createdByName}
          </p>
        </div>

        <div className="flex gap-2 flex-wrap justify-end">
          <button onClick={handleDownloadPdf} className={`${btn} bg-white border border-gray-300 text-gray-700 hover:bg-gray-50`}>
            Download PDF
          </button>
          {canManage && claim.status === 'Draft' && (
            <button onClick={handleSubmit} disabled={busy}
              className={`${btn} bg-blue-600 text-white hover:bg-blue-700`}>
              Mark as Submitted
            </button>
          )}
          {isAdmin && claim.status === 'Submitted' && (
            <>
              <button onClick={() => { setShowApprove(true); setApprovedAmount(claim.claimAmount.toFixed(2)); }}
                className={`${btn} bg-green-600 text-white hover:bg-green-700`}>
                Record Approval
              </button>
              <button onClick={() => setShowReject(true)}
                className={`${btn} bg-red-600 text-white hover:bg-red-700`}>
                Record Rejection
              </button>
            </>
          )}
          {isAdmin && (claim.status === 'Draft' || claim.status === 'Submitted') && (
            <button onClick={handleCancel} disabled={busy}
              className={`${btn} bg-white border border-gray-300 text-gray-500 hover:bg-gray-50`}>
              Cancel Claim
            </button>
          )}
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded text-sm">{error}</div>
      )}

      {/* Info grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Patient */}
        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Patient</h2>
          <p className="font-semibold text-gray-900">{claim.patientName}</p>
          <p className="text-sm text-gray-600">MRN: {claim.patientMrn}</p>
          {claim.nhisNumber && (
            <p className="text-sm text-blue-700 font-medium mt-1">NHIS #: {claim.nhisNumber}</p>
          )}
        </div>

        {/* Payer */}
        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Payer</h2>
          <p className="font-semibold text-gray-900">{claim.payerName}</p>
          <p className="text-sm text-gray-600">{claim.payerType}</p>
        </div>

        {/* Invoice */}
        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Invoice</h2>
          <Link to={`/billing/${claim.billId}`} className="font-mono text-blue-600 hover:underline font-semibold">
            {claim.billNumber}
          </Link>
        </div>

        {/* Amounts */}
        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Amounts</h2>
          <div className="space-y-1 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-600">Claim Amount:</span>
              <span className="font-semibold">{fmt(claim.claimAmount)}</span>
            </div>
            {claim.approvedAmount != null && (
              <div className="flex justify-between">
                <span className="text-gray-600">Approved Amount:</span>
                <span className="font-semibold text-green-700">{fmt(claim.approvedAmount)}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Timeline */}
      <div className="bg-white border border-gray-200 rounded-lg p-5">
        <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Timeline</h2>
        <div className="space-y-2 text-sm">
          <div className="flex gap-4">
            <span className="text-gray-500 w-28 shrink-0">Created</span>
            <span>{fmtDate(claim.createdAt)}</span>
          </div>
          {claim.submittedAt && (
            <div className="flex gap-4">
              <span className="text-gray-500 w-28 shrink-0">Submitted</span>
              <span>{fmtDate(claim.submittedAt)}</span>
            </div>
          )}
          {claim.responseAt && (
            <div className="flex gap-4">
              <span className="text-gray-500 w-28 shrink-0">Response</span>
              <span>{fmtDate(claim.responseAt)}</span>
            </div>
          )}
        </div>
      </div>

      {/* Rejection reason */}
      {claim.rejectionReason && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-xs font-semibold text-red-600 uppercase tracking-wide mb-1">Rejection Reason</p>
          <p className="text-sm text-red-800">{claim.rejectionReason}</p>
        </div>
      )}

      {/* Notes */}
      {claim.notes && (
        <div className="bg-white border border-gray-200 rounded-lg p-5">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Notes</h2>
          <p className="text-sm text-gray-700">{claim.notes}</p>
        </div>
      )}

      {/* Payment link */}
      {claim.paymentId && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4 text-sm text-green-800">
          Payment recorded on invoice{' '}
          <Link to={`/billing/${claim.billId}`} className="font-semibold underline">{claim.billNumber}</Link>.
        </div>
      )}

      {/* Approve modal */}
      {showApprove && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-md">
            <h2 className="text-lg font-bold mb-4">Record Insurer Approval</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-xs text-gray-500 mb-1">Approved Amount (GHS) <span className="text-red-500">*</span></label>
                <input type="number" min="0.01" step="0.01" value={approvedAmount}
                  onChange={(e) => setApprovedAmount(e.target.value)} className={inp} />
                <p className="text-xs text-gray-500 mt-1">Claimed: {fmt(claim.claimAmount)}</p>
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Notes</label>
                <textarea value={approveNotes} onChange={(e) => setApproveNotes(e.target.value)}
                  rows={2} className={inp} />
              </div>
            </div>
            <div className="flex gap-3 mt-5 justify-end">
              <button onClick={() => setShowApprove(false)}
                className={`${btn} bg-white border border-gray-300 text-gray-700`}>Cancel</button>
              <button onClick={handleApprove} disabled={busy}
                className={`${btn} bg-green-600 text-white hover:bg-green-700`}>Confirm Approval</button>
            </div>
          </div>
        </div>
      )}

      {/* Reject modal */}
      {showReject && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-md">
            <h2 className="text-lg font-bold mb-4">Record Rejection</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-xs text-gray-500 mb-1">Rejection Reason <span className="text-red-500">*</span></label>
                <textarea value={rejectionReason} onChange={(e) => setRejectionReason(e.target.value)}
                  rows={3} className={inp} placeholder="e.g. Service not covered, Invalid NHIS number…" />
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Notes</label>
                <textarea value={rejectNotes} onChange={(e) => setRejectNotes(e.target.value)}
                  rows={2} className={inp} />
              </div>
            </div>
            <div className="flex gap-3 mt-5 justify-end">
              <button onClick={() => setShowReject(false)}
                className={`${btn} bg-white border border-gray-300 text-gray-700`}>Cancel</button>
              <button onClick={handleReject} disabled={busy}
                className={`${btn} bg-red-600 text-white hover:bg-red-700`}>Confirm Rejection</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
