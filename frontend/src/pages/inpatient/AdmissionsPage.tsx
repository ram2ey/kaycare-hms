import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { getAdmissions, getWards, admitPatient, getBeds } from '../../api/inpatient';
import type { AdmissionSummaryResponse, WardResponse, BedResponse, AdmitPatientRequest } from '../../types/inpatient';
import { ADMISSION_STATUS_COLORS } from '../../types/inpatient';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';
import apiClient from '../../api/client';
import type { PatientResponse } from '../../types/patients';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const fmtDate = (d: string) => new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
const fmtDateTime = (d: string) => new Date(d).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

const ADMIT_ROLES = [Roles.Doctor, Roles.Admin, Roles.SuperAdmin, Roles.Receptionist];

export default function AdmissionsPage() {
  const { user } = useAuth();
  const canAdmit = ADMIT_ROLES.includes(user?.role as any);

  const [admissions, setAdmissions] = useState<AdmissionSummaryResponse[]>([]);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState('');
  const [filterStatus, setFilterStatus] = useState('Active');
  const [filterWard, setFilterWard]     = useState('');
  const [wards, setWards]               = useState<WardResponse[]>([]);

  // Admit modal state
  const [showAdmit, setShowAdmit]   = useState(false);
  const [admitForm, setAdmitForm]   = useState<Partial<AdmitPatientRequest>>({ admissionReason: '', diagnosisOnAdmission: '' });
  const [admitSaving, setAdmitSaving] = useState(false);
  const [admitError, setAdmitError]   = useState('');
  const [patients, setPatients]       = useState<PatientResponse[]>([]);
  const [doctors, setDoctors]         = useState<{ userId: string; fullName: string }[]>([]);
  const [availableBeds, setAvailableBeds] = useState<BedResponse[]>([]);

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const params: any = {};
      if (filterStatus) params.status = filterStatus;
      if (filterWard)   params.wardId = filterWard;
      setAdmissions(await getAdmissions(params));
    } catch { setError('Failed to load admissions.'); }
    finally { setLoading(false); }
  }, [filterStatus, filterWard]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    getWards({ activeOnly: true }).then(setWards).catch(() => setError('Failed to load wards. Ward filter may be incomplete.'));
  }, []);

  const openAdmit = async () => {
    setAdmitForm({ admissionReason: '', diagnosisOnAdmission: '' });
    setAdmitError('');
    setShowAdmit(true);
    try {
      const [pts, usrs, beds] = await Promise.all([
        apiClient.get<PatientResponse[]>('/patients').then(r => r.data),
        apiClient.get<any[]>('/users').then(r => r.data),
        apiClient.get<BedResponse[]>('/wards/beds/available').then(r => r.data).catch(() => [] as BedResponse[]),
      ]);
      setPatients(pts);
      setDoctors(usrs
        .filter((u: any) => u.role === 'Doctor' || u.role === 'Admin' || u.role === 'SuperAdmin')
        .map((u: any) => ({ userId: u.userId, fullName: u.fullName })));
      setAvailableBeds(beds);
    } catch { setAdmitError('Failed to load patients, doctors, and beds. Please try again.'); }
  };

  const handleWardChange = async (wardId: string) => {
    setAdmitForm(f => ({ ...f, bedId: undefined }));
    if (!wardId) { setAvailableBeds([]); return; }
    try {
      const beds = await getBeds(wardId);
      setAvailableBeds(beds.filter(b => b.status === 'Available'));
    } catch { setAvailableBeds([]); }
  };

  const handleAdmit = async () => {
    if (!admitForm.patientId)             { setAdmitError('Select a patient.'); return; }
    if (!admitForm.bedId)                 { setAdmitError('Select a bed.'); return; }
    if (!admitForm.admittingDoctorUserId) { setAdmitError('Select admitting doctor.'); return; }
    if (!admitForm.admissionReason?.trim()) { setAdmitError('Admission reason is required.'); return; }

    setAdmitSaving(true); setAdmitError('');
    try {
      const result = await admitPatient(admitForm as AdmitPatientRequest);
      setAdmissions(prev => [result, ...prev]);
      setShowAdmit(false);
    } catch (e: any) {
      setAdmitError(e?.response?.data?.message ?? 'Failed to admit patient.');
    } finally { setAdmitSaving(false); }
  };

  const active    = admissions.filter(a => a.status === 'Active').length;
  const discharged = admissions.filter(a => a.status === 'Discharged').length;

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Admissions</h1>
          <p className="text-sm text-gray-500 mt-1">Inpatient admission management</p>
        </div>
        {canAdmit && (
          <button onClick={openAdmit} className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium">
            + Admit Patient
          </button>
        )}
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-4 mb-6">
        <div className="bg-white border border-gray-200 rounded-xl p-4 text-center">
          <div className="text-3xl font-bold text-green-600">{active}</div>
          <div className="text-sm text-gray-500 mt-1">Active</div>
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-4 text-center">
          <div className="text-3xl font-bold text-gray-400">{discharged}</div>
          <div className="text-sm text-gray-500 mt-1">Discharged (filtered)</div>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-4">
        <select value={filterStatus} onChange={e => setFilterStatus(e.target.value)}
          className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">All statuses</option>
          <option value="Active">Active</option>
          <option value="Discharged">Discharged</option>
        </select>
        <select value={filterWard} onChange={e => setFilterWard(e.target.value)}
          className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">All wards</option>
          {wards.map(w => <option key={w.wardId} value={w.wardId}>{w.name}</option>)}
        </select>
      </div>

      {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{error}</div>}

      {loading ? (
        <div className="text-center py-12 text-gray-400">Loading…</div>
      ) : admissions.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          <p className="text-4xl mb-3">🛏</p>
          <p className="text-lg font-medium">No admissions found</p>
        </div>
      ) : (
        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200">
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Admission #</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Patient</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Ward / Bed</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Doctor</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Admitted</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Expected D/C</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Status</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              {admissions.map(a => (
                <tr key={a.admissionId} className="border-b border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono text-xs text-gray-600">{a.admissionNumber}</td>
                  <td className="px-4 py-3">
                    <div className="font-medium text-gray-900">{a.patientName}</div>
                    <div className="text-xs text-gray-400">{a.patientMrn}</div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="text-gray-700">{a.wardName}</div>
                    <div className="text-xs text-gray-400">Bed {a.bedNumber}</div>
                  </td>
                  <td className="px-4 py-3 text-gray-600">{a.admittingDoctorName}</td>
                  <td className="px-4 py-3 text-gray-600 whitespace-nowrap">{fmtDateTime(a.admissionDate)}</td>
                  <td className="px-4 py-3 text-gray-500">
                    {a.expectedDischargeDate ? fmtDate(a.expectedDischargeDate) : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${ADMISSION_STATUS_COLORS[a.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {a.status}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <Link to={`/inpatient/admissions/${a.admissionId}`} className="text-blue-600 hover:underline text-xs font-medium">
                      View →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Admit modal */}
      {showAdmit && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between sticky top-0 bg-white">
              <h2 className="font-semibold text-gray-900">Admit Patient</h2>
              <button onClick={() => setShowAdmit(false)} className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="px-6 py-4 space-y-4">
              {admitError && <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-3 py-2 text-sm">{admitError}</div>}

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Patient *</label>
                <select className={inp} value={admitForm.patientId ?? ''}
                  onChange={e => setAdmitForm(f => ({ ...f, patientId: e.target.value }))}>
                  <option value="">Select patient…</option>
                  {patients.map(p => (
                    <option key={p.patientId} value={p.patientId}>{p.fullName} — {p.medicalRecordNumber}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Ward</label>
                <select className={inp} onChange={e => handleWardChange(e.target.value)}>
                  <option value="">Select ward to filter beds…</option>
                  {wards.map(w => <option key={w.wardId} value={w.wardId}>{w.name} ({w.availableBeds} available)</option>)}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Bed *</label>
                <select className={inp} value={admitForm.bedId ?? ''}
                  onChange={e => setAdmitForm(f => ({ ...f, bedId: e.target.value }))}>
                  <option value="">Select available bed…</option>
                  {availableBeds.map(b => (
                    <option key={b.bedId} value={b.bedId}>{b.wardName} — Bed {b.bedNumber}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Admitting Doctor *</label>
                <select className={inp} value={admitForm.admittingDoctorUserId ?? ''}
                  onChange={e => setAdmitForm(f => ({ ...f, admittingDoctorUserId: e.target.value }))}>
                  <option value="">Select doctor…</option>
                  {doctors.map(d => <option key={d.userId} value={d.userId}>{d.fullName}</option>)}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Admission Reason *</label>
                <textarea className={inp} rows={2} value={admitForm.admissionReason}
                  onChange={e => setAdmitForm(f => ({ ...f, admissionReason: e.target.value }))} />
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Diagnosis on Admission</label>
                <input className={inp} value={admitForm.diagnosisOnAdmission ?? ''}
                  onChange={e => setAdmitForm(f => ({ ...f, diagnosisOnAdmission: e.target.value }))} />
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Expected Discharge Date</label>
                <input type="date" className={inp} value={admitForm.expectedDischargeDate ?? ''}
                  onChange={e => setAdmitForm(f => ({ ...f, expectedDischargeDate: e.target.value || undefined }))} />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3 sticky bottom-0 bg-white">
              <button onClick={() => setShowAdmit(false)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleAdmit} disabled={admitSaving} className="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm font-medium">
                {admitSaving ? 'Admitting…' : 'Admit Patient'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
