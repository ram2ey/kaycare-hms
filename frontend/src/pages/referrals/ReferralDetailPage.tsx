import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  getReferral,
  sendReferral,
  respondToReferral,
  cancelReferral,
  updateReferral,
} from '../../api/referrals';
import { getUsers } from '../../api/users';
import type { ReferralDetailResponse, UpdateReferralRequest } from '../../types/referrals';
import type { UserResponse } from '../../types/users';
import { REFERRAL_STATUS_COLORS, REFERRAL_URGENCY_COLORS } from '../../types/referrals';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';
import { ConfirmDialog } from '../../components/ConfirmDialog';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const fmtDT = (d: string) => new Date(d).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

export default function ReferralDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();

  const [referral, setReferral]   = useState<ReferralDetailResponse | null>(null);
  const [loading, setLoading]     = useState(true);
  const [error, setError]         = useState('');
  const [doctors, setDoctors]     = useState<UserResponse[]>([]);

  const [showEdit, setShowEdit]           = useState(false);
  const [editForm, setEditForm]           = useState<UpdateReferralRequest | null>(null);
  const [editSaving, setEditSaving]       = useState(false);

  const [showRespond, setShowRespond]     = useState(false);
  const [respondAction, setRespondAction] = useState('');
  const [respondNotes, setRespondNotes]   = useState('');
  const [respondSaving, setRespondSaving] = useState(false);

  const [actionError, setActionError]     = useState('');
  const [confirmAction, setConfirmAction] = useState<null | { message: string; danger?: boolean; run: () => void }>(null);

  const canManage = user && [Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user.role as never);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getReferral(id)
      .then(r => { setReferral(r); })
      .catch(() => setError('Failed to load referral.'))
      .finally(() => setLoading(false));
    getUsers({ role: 'Doctor' }).then(setDoctors).catch(() => {});
  }, [id]);

  const openEdit = () => {
    if (!referral) return;
    setEditForm({
      referredToDoctorUserId: referral.referredToDoctorUserId ?? undefined,
      referredToDepartment:   referral.referredToDepartment ?? undefined,
      referralType:           referral.referralType,
      externalFacility:       referral.externalFacility ?? undefined,
      urgency:                referral.urgency,
      reason:                 referral.reason,
      clinicalNotes:          referral.clinicalNotes ?? undefined,
    });
    setShowEdit(true);
  };

  const handleEdit = async () => {
    if (!editForm || !id) return;
    setEditSaving(true); setActionError('');
    try {
      setReferral(await updateReferral(id, editForm));
      setShowEdit(false);
    } catch (e: any) {
      setActionError(e?.response?.data?.message ?? 'Failed to update referral.');
    } finally { setEditSaving(false); }
  };

  const handleSend = async () => {
    if (!id) return;
    setActionError('');
    try { setReferral(await sendReferral(id)); }
    catch (e: any) { setActionError(e?.response?.data?.message ?? 'Failed to send.'); }
  };

  const handleRespond = async () => {
    if (!id || !respondAction) return;
    setRespondSaving(true); setActionError('');
    try {
      setReferral(await respondToReferral(id, { action: respondAction, responseNotes: respondNotes || undefined }));
      setShowRespond(false);
      setRespondNotes('');
    } catch (e: any) {
      setActionError(e?.response?.data?.message ?? 'Failed to respond.');
    } finally { setRespondSaving(false); }
  };

  const handleCancel = async () => {
    if (!id) return;
    setActionError('');
    try { setReferral(await cancelReferral(id)); }
    catch (e: any) { setActionError(e?.response?.data?.message ?? 'Failed to cancel.'); }
  };

  if (loading) return <div className="p-6 text-center text-gray-400">Loading…</div>;
  if (error || !referral) return <div className="p-6 text-center text-red-500">{error || 'Not found.'}</div>;

  const isDraft    = referral.status === 'Draft';
  const isSent     = referral.status === 'Sent';
  const isAccepted = referral.status === 'Accepted';
  const isOpen     = isDraft || isSent || isAccepted;

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <div className="mb-3">
        <Link to="/referrals" className="text-sm text-gray-500 hover:text-gray-700">← Referrals</Link>
      </div>

      {actionError && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-600 rounded-lg px-4 py-3 text-sm">{actionError}</div>
      )}

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">{referral.referralNumber}</h1>
            <span className={`text-sm px-3 py-0.5 rounded-full font-medium ${REFERRAL_STATUS_COLORS[referral.status] ?? 'bg-gray-100 text-gray-600'}`}>
              {referral.status}
            </span>
            <span className={`text-sm px-3 py-0.5 rounded-full font-medium ${REFERRAL_URGENCY_COLORS[referral.urgency] ?? 'bg-gray-100 text-gray-600'}`}>
              {referral.urgency}
            </span>
          </div>
          <p className="text-gray-500 mt-1">{referral.patientName} · {referral.patientMrn}</p>
        </div>
        <div className="flex gap-2 flex-wrap justify-end">
          <Link to={`/patients/${referral.patientId}`} className="px-3 py-1.5 border border-teal-500 text-teal-700 rounded-lg hover:bg-teal-50 text-sm font-medium">
            Patient Profile
          </Link>
          {isDraft && canManage && (
            <>
              <button onClick={openEdit} className="px-3 py-1.5 border border-gray-300 text-gray-600 rounded-lg hover:bg-gray-50 text-sm font-medium">Edit</button>
              <button onClick={() => setConfirmAction({ message: 'Send this referral?', run: handleSend })} className="px-3 py-1.5 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium">Send Referral</button>
            </>
          )}
          {(isSent || isAccepted) && canManage && (
            <button onClick={() => { setRespondAction(''); setRespondNotes(''); setShowRespond(true); }}
              className="px-3 py-1.5 bg-teal-600 text-white rounded-lg hover:bg-teal-700 text-sm font-medium">
              Respond
            </button>
          )}
          {isOpen && canManage && (
            <button onClick={() => setConfirmAction({ message: 'Cancel this referral?', danger: true, run: handleCancel })} className="px-3 py-1.5 border border-red-500 text-red-600 rounded-lg hover:bg-red-50 text-sm font-medium">Cancel</button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 mb-4">
        {/* Referral info */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <h2 className="font-semibold text-gray-700 mb-3 text-sm uppercase tracking-wide">Referral Details</h2>
          <dl className="space-y-2 text-sm">
            {([
              ['Referring Doctor', referral.referringDoctorName],
              ['Type', referral.referralType],
              referral.referralType === 'Internal'
                ? ['Referred To', referral.referredToDoctorName ?? referral.referredToDepartment ?? '—']
                : ['External Facility', referral.externalFacility ?? '—'],
              ['Department', referral.referredToDepartment ?? '—'],
              ['Created', fmtDT(referral.createdAt)],
              ['Updated', fmtDT(referral.updatedAt)],
            ] as [string, string][]).map(([label, value]) => (
              <div key={label} className="flex gap-2">
                <dt className="text-gray-400 min-w-[130px]">{label}</dt>
                <dd className="text-gray-800 font-medium">{value}</dd>
              </div>
            ))}
          </dl>
        </div>

        {/* Response */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <h2 className="font-semibold text-gray-700 mb-3 text-sm uppercase tracking-wide">Response</h2>
          {referral.responseNotes ? (
            <p className="text-sm text-gray-700">{referral.responseNotes}</p>
          ) : (
            <p className="text-sm text-gray-400">No response recorded yet.</p>
          )}
        </div>
      </div>

      {/* Reason */}
      <div className="bg-white border border-gray-200 rounded-xl p-5 mb-4">
        <h2 className="font-semibold text-gray-700 mb-2 text-sm uppercase tracking-wide">Reason for Referral</h2>
        <p className="text-sm text-gray-700 whitespace-pre-wrap">{referral.reason}</p>
      </div>

      {/* Clinical notes */}
      {referral.clinicalNotes && (
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <h2 className="font-semibold text-gray-700 mb-2 text-sm uppercase tracking-wide">Clinical Notes</h2>
          <p className="text-sm text-gray-700 whitespace-pre-wrap">{referral.clinicalNotes}</p>
        </div>
      )}

      {/* Edit modal */}
      {showEdit && editForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Edit Referral</h2>
              <button onClick={() => setShowEdit(false)} className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Type</label>
                  <select className={inp} value={editForm.referralType} onChange={e => setEditForm(f => f && ({ ...f, referralType: e.target.value }))}>
                    <option value="Internal">Internal</option>
                    <option value="External">External</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Urgency</label>
                  <select className={inp} value={editForm.urgency} onChange={e => setEditForm(f => f && ({ ...f, urgency: e.target.value }))}>
                    <option value="Routine">Routine</option>
                    <option value="Urgent">Urgent</option>
                    <option value="Emergency">Emergency</option>
                  </select>
                </div>
              </div>
              {editForm.referralType === 'Internal' ? (
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">Referred-to Doctor</label>
                    <select className={inp} value={editForm.referredToDoctorUserId ?? ''} onChange={e => setEditForm(f => f && ({ ...f, referredToDoctorUserId: e.target.value || undefined }))}>
                      <option value="">Select…</option>
                      {doctors.map(d => <option key={d.userId} value={d.userId}>{d.fullName}</option>)}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">Department</label>
                    <input className={inp} value={editForm.referredToDepartment ?? ''} onChange={e => setEditForm(f => f && ({ ...f, referredToDepartment: e.target.value || undefined }))} />
                  </div>
                </div>
              ) : (
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">External Facility</label>
                  <input className={inp} value={editForm.externalFacility ?? ''} onChange={e => setEditForm(f => f && ({ ...f, externalFacility: e.target.value || undefined }))} />
                </div>
              )}
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Reason *</label>
                <textarea className={`${inp} resize-none`} rows={3} value={editForm.reason} onChange={e => setEditForm(f => f && ({ ...f, reason: e.target.value }))} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Clinical Notes</label>
                <textarea className={`${inp} resize-none`} rows={3} value={editForm.clinicalNotes ?? ''} onChange={e => setEditForm(f => f && ({ ...f, clinicalNotes: e.target.value || undefined }))} />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setShowEdit(false)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleEdit} disabled={editSaving} className="px-5 py-2 bg-blue-700 text-white rounded-lg text-sm font-medium hover:bg-blue-800 disabled:opacity-50">
                {editSaving ? 'Saving…' : 'Save Changes'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Respond modal */}
      {showRespond && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Respond to Referral</h2>
              <button onClick={() => setShowRespond(false)} className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-2">Action *</label>
                <div className="flex gap-2">
                  {(['Accept', 'Complete', 'Decline'] as const).map(a => (
                    <button key={a}
                      onClick={() => setRespondAction(a)}
                      className={`flex-1 py-2 rounded-lg text-sm font-medium border-2 transition-colors ${
                        respondAction === a
                          ? a === 'Decline' ? 'border-red-500 bg-red-50 text-red-700' : 'border-blue-500 bg-blue-50 text-blue-700'
                          : 'border-gray-200 text-gray-600 hover:border-gray-300'
                      }`}>
                      {a}
                    </button>
                  ))}
                </div>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Notes</label>
                <textarea className={`${inp} resize-none`} rows={3} value={respondNotes} onChange={e => setRespondNotes(e.target.value)} placeholder="Clinical response, outcome, follow-up plan…" />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setShowRespond(false)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleRespond} disabled={respondSaving || !respondAction}
                className="px-5 py-2 bg-teal-600 text-white rounded-lg text-sm font-medium hover:bg-teal-700 disabled:opacity-50">
                {respondSaving ? 'Saving…' : 'Submit Response'}
              </button>
            </div>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={!!confirmAction}
        title="Confirm"
        message={confirmAction?.message ?? ''}
        danger={!!confirmAction?.danger}
        onConfirm={() => { confirmAction?.run(); setConfirmAction(null); }}
        onCancel={() => setConfirmAction(null)}
      />
    </div>
  );
}
