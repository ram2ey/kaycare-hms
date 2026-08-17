import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getAdmission, dischargePatient, transferPatient, getBeds } from '../../api/inpatient';
import type { AdmissionDetailResponse, DischargePatientRequest, TransferPatientRequest, BedResponse } from '../../types/inpatient';
import { ADMISSION_STATUS_COLORS, DISCHARGE_TYPES, DISCHARGE_TYPE_LABELS } from '../../types/inpatient';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const fmtDT = (d: string) => new Date(d).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
const fmtD  = (d: string) => new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });

export default function AdmissionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const canDischarge = [Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user?.role as any);
  const canTransfer  = [Roles.Doctor, Roles.Nurse, Roles.Admin, Roles.SuperAdmin].includes(user?.role as any);

  const [admission, setAdmission] = useState<AdmissionDetailResponse | null>(null);
  const [loading, setLoading]     = useState(true);
  const [error, setError]         = useState('');

  // Discharge modal
  const [showDischarge, setShowDischarge]   = useState(false);
  const [dischargeForm, setDischargeForm]   = useState<DischargePatientRequest>({ dischargeType: 'Regular', dischargeNotes: '' });
  const [dischargeSaving, setDischargeSaving] = useState(false);
  const [dischargeError, setDischargeError]   = useState('');

  // Transfer modal
  const [showTransfer, setShowTransfer]     = useState(false);
  const [transferForm, setTransferForm]     = useState<TransferPatientRequest>({ newBedId: '', reason: '' });
  const [transferSaving, setTransferSaving] = useState(false);
  const [transferError, setTransferError]   = useState('');
  const [availableBeds, setAvailableBeds]   = useState<BedResponse[]>([]);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true); setError('');
    try { setAdmission(await getAdmission(id)); }
    catch { setError('Failed to load admission.'); }
    finally { setLoading(false); }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  const handleDischarge = async () => {
    setDischargeSaving(true); setDischargeError('');
    try {
      setAdmission(await dischargePatient(id!, dischargeForm));
      setShowDischarge(false);
    } catch (e: any) {
      setDischargeError(e?.response?.data?.message ?? 'Failed to discharge.');
    } finally { setDischargeSaving(false); }
  };

  const openTransfer = async () => {
    setTransferForm({ newBedId: '', reason: '' });
    setTransferError('');
    setShowTransfer(true);
    if (!admission) return;
    try {
      const beds = await getBeds(admission.wardId);
      setAvailableBeds(beds.filter(b => b.status === 'Available'));
    } catch { setAvailableBeds([]); }
  };

  const handleTransfer = async () => {
    if (!transferForm.newBedId) { setTransferError('Select a destination bed.'); return; }
    setTransferSaving(true); setTransferError('');
    try {
      setAdmission(await transferPatient(id!, transferForm));
      setShowTransfer(false);
    } catch (e: any) {
      setTransferError(e?.response?.data?.message ?? 'Failed to transfer.');
    } finally { setTransferSaving(false); }
  };

  if (loading) return <div className="p-6 text-center text-gray-400">Loading…</div>;
  if (error)   return <div className="p-6 text-center text-red-500">{error}</div>;
  if (!admission) return <div className="p-6 text-center text-gray-400">Not found.</div>;

  const isActive = admission.status === 'Active';

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <div className="flex items-center gap-3 mb-2">
        <Link to="/inpatient/admissions" className="text-sm text-gray-500 hover:text-gray-700">← Admissions</Link>
      </div>

      <div className="flex items-start justify-between mb-6">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">{admission.admissionNumber}</h1>
            <span className={`text-sm px-3 py-0.5 rounded-full font-medium ${ADMISSION_STATUS_COLORS[admission.status] ?? 'bg-gray-100 text-gray-600'}`}>
              {admission.status}
            </span>
          </div>
          <p className="text-gray-500 mt-1">{admission.patientName} · {admission.patientMrn}</p>
        </div>
        <div className="flex gap-2 flex-wrap justify-end">
          {admission?.patientId && (
            <>
              <Link to={`/patients/${admission.patientId}/vitals`}
                className="px-4 py-2 border border-teal-500 text-teal-700 rounded-lg hover:bg-teal-50 text-sm font-medium">
                Vital Signs
              </Link>
              <Link to={`/patients/${admission.patientId}/nursing-notes`}
                className="px-4 py-2 border border-teal-500 text-teal-700 rounded-lg hover:bg-teal-50 text-sm font-medium">
                Nursing Notes
              </Link>
              <Link to={`/inpatient/admissions/${id}/mar`}
                className="px-4 py-2 border border-teal-500 text-teal-700 rounded-lg hover:bg-teal-50 text-sm font-medium">
                MAR
              </Link>
            </>
          )}
          <Link to={`/inpatient/admissions/${id}/discharge-summary`}
            className="px-4 py-2 border border-purple-500 text-purple-700 rounded-lg hover:bg-purple-50 text-sm font-medium">
            Discharge Summary
          </Link>
          <Link to={`/inpatient/admissions/${id}/billing`}
            className="px-4 py-2 border border-green-500 text-green-700 rounded-lg hover:bg-green-50 text-sm font-medium">
            Billing
          </Link>
          {isActive && canTransfer && (
            <button onClick={openTransfer} className="px-4 py-2 border border-blue-500 text-blue-600 rounded-lg hover:bg-blue-50 text-sm font-medium">
              Transfer
            </button>
          )}
          {isActive && canDischarge && (
            <button onClick={() => { setDischargeForm({ dischargeType: 'Regular', dischargeNotes: '' }); setDischargeError(''); setShowDischarge(true); }}
              className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 text-sm font-medium">
              Discharge
            </button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
        {/* Admission info */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <h2 className="font-semibold text-gray-700 mb-3 text-sm uppercase tracking-wide">Admission Details</h2>
          <dl className="space-y-2 text-sm">
            {[
              ['Ward', admission.wardName],
              ['Bed', admission.bedNumber],
              ['Admitting Doctor', admission.admittingDoctorName],
              ['Admitted', fmtDT(admission.admissionDate)],
              ['Expected D/C', admission.expectedDischargeDate ? fmtD(admission.expectedDischargeDate) : '—'],
              ['Reason', admission.admissionReason],
              ['Diagnosis', admission.diagnosisOnAdmission ?? '—'],
            ].map(([label, value]) => (
              <div key={label} className="flex gap-2">
                <dt className="text-gray-400 min-w-[110px]">{label}</dt>
                <dd className="text-gray-800 font-medium">{value}</dd>
              </div>
            ))}
          </dl>
        </div>

        {/* Discharge info */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <h2 className="font-semibold text-gray-700 mb-3 text-sm uppercase tracking-wide">Discharge Details</h2>
          {admission.status === 'Discharged' ? (
            <dl className="space-y-2 text-sm">
              {[
                ['Discharged', admission.actualDischargeDate ? fmtDT(admission.actualDischargeDate) : '—'],
                ['Type', admission.dischargeType ? DISCHARGE_TYPE_LABELS[admission.dischargeType] ?? admission.dischargeType : '—'],
                ['Notes', admission.dischargeNotes ?? '—'],
              ].map(([label, value]) => (
                <div key={label} className="flex gap-2">
                  <dt className="text-gray-400 min-w-[80px]">{label}</dt>
                  <dd className="text-gray-800 font-medium">{value}</dd>
                </div>
              ))}
            </dl>
          ) : (
            <p className="text-gray-400 text-sm">Patient is currently admitted.</p>
          )}
        </div>
      </div>

      {/* Transfer history */}
      <div className="bg-white border border-gray-200 rounded-xl p-5">
        <h2 className="font-semibold text-gray-700 mb-3 text-sm uppercase tracking-wide">
          Transfer History {admission.transfers.length > 0 && `(${admission.transfers.length})`}
        </h2>
        {admission.transfers.length === 0 ? (
          <p className="text-gray-400 text-sm">No transfers recorded.</p>
        ) : (
          <div className="space-y-3">
            {admission.transfers.map(t => (
              <div key={t.admissionTransferId} className="flex items-start gap-4 text-sm border-b border-gray-100 pb-3 last:border-0">
                <div className="text-gray-400 whitespace-nowrap min-w-[130px]">{fmtDT(t.transferredAt)}</div>
                <div>
                  <span className="font-medium text-gray-700">{t.fromWardName} · {t.fromBedNumber}</span>
                  <span className="mx-2 text-gray-400">→</span>
                  <span className="font-medium text-gray-700">{t.toWardName} · {t.toBedNumber}</span>
                  {t.reason && <div className="text-gray-500 mt-0.5">{t.reason}</div>}
                  <div className="text-gray-400 text-xs mt-0.5">By {t.transferredByName}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Discharge modal */}
      {showDischarge && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Discharge Patient</h2>
              <button onClick={() => setShowDischarge(false)} aria-label="Close" className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="px-6 py-4 space-y-4">
              {dischargeError && <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-3 py-2 text-sm">{dischargeError}</div>}
              <div>
                <span id="discharge-type-label" className="block text-xs font-medium text-gray-600 mb-2">Discharge Type</span>
                <div role="group" aria-labelledby="discharge-type-label" className="grid grid-cols-2 gap-2">
                  {DISCHARGE_TYPES.map(t => (
                    <button key={t}
                      onClick={() => setDischargeForm(f => ({ ...f, dischargeType: t }))}
                      className={`py-2 rounded-lg text-sm border-2 transition-colors ${
                        dischargeForm.dischargeType === t
                          ? 'border-blue-500 bg-blue-50 text-blue-700 font-medium'
                          : 'border-gray-200 text-gray-600 hover:border-gray-300'
                      }`}>
                      {DISCHARGE_TYPE_LABELS[t]}
                    </button>
                  ))}
                </div>
              </div>
              <div>
                <label htmlFor="discharge-notes" className="block text-xs font-medium text-gray-600 mb-1">Discharge Notes</label>
                <textarea id="discharge-notes" className={inp} rows={3} value={dischargeForm.dischargeNotes}
                  onChange={e => setDischargeForm(f => ({ ...f, dischargeNotes: e.target.value }))} />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setShowDischarge(false)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleDischarge} disabled={dischargeSaving} className="px-5 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 text-sm font-medium">
                {dischargeSaving ? 'Discharging…' : 'Confirm Discharge'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Transfer modal */}
      {showTransfer && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Transfer Patient</h2>
              <button onClick={() => setShowTransfer(false)} aria-label="Close" className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="px-6 py-4 space-y-4">
              {transferError && <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-3 py-2 text-sm">{transferError}</div>}
              <p className="text-sm text-gray-500">Current: <span className="font-medium text-gray-800">{admission.wardName} · Bed {admission.bedNumber}</span></p>
              <div>
                <label htmlFor="transfer-bed" className="block text-xs font-medium text-gray-600 mb-1">New Bed *</label>
                <select id="transfer-bed" className={inp} value={transferForm.newBedId}
                  onChange={e => setTransferForm(f => ({ ...f, newBedId: e.target.value }))}>
                  <option value="">Select available bed…</option>
                  {availableBeds.map(b => (
                    <option key={b.bedId} value={b.bedId}>{b.wardName} — Bed {b.bedNumber}</option>
                  ))}
                </select>
                {availableBeds.length === 0 && <p className="text-xs text-yellow-600 mt-1">No available beds in current ward. Select a different ward from the Wards page.</p>}
              </div>
              <div>
                <label htmlFor="transfer-reason" className="block text-xs font-medium text-gray-600 mb-1">Reason</label>
                <input id="transfer-reason" className={inp} value={transferForm.reason ?? ''}
                  onChange={e => setTransferForm(f => ({ ...f, reason: e.target.value }))} />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setShowTransfer(false)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleTransfer} disabled={transferSaving} className="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm font-medium">
                {transferSaving ? 'Transferring…' : 'Confirm Transfer'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
