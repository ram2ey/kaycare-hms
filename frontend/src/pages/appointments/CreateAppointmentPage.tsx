import { useState, useEffect } from 'react';
import { useNavigate, Link, useSearchParams } from 'react-router-dom';
import { createAppointment, listDoctors } from '../../api/appointments';
import { searchPatients } from '../../api/patients';
import type { CreateAppointmentRequest, UserSummary } from '../../types/appointments';
import type { PatientResponse } from '../../types/patients';
import { AppointmentTypes } from '../../types/appointments';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

export default function CreateAppointmentPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [searchParams] = useSearchParams();

  const [doctors, setDoctors] = useState<UserSummary[]>([]);
  const [patientQuery, setPatientQuery] = useState('');
  const [patientResults, setPatientResults] = useState<PatientResponse[]>([]);
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [searching, setSearching] = useState(false);

  const [form, setForm] = useState<CreateAppointmentRequest>({
    patientId: searchParams.get('patientId') ?? '',
    doctorUserId: user?.role === Roles.Doctor ? (user as { userId?: string })?.userId ?? '' : '',
    scheduledAt: '',
    durationMinutes: 30,
    appointmentType: 'Consultation',
    chiefComplaint: '',
    room: '',
    notes: '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    listDoctors().then(setDoctors).catch(() => {});
  }, []);

  // Pre-fill patient if passed via query param
  useEffect(() => {
    const pid = searchParams.get('patientId');
    if (pid) setForm((f) => ({ ...f, patientId: pid }));
  }, [searchParams]);

  async function searchForPatient() {
    if (!patientQuery.trim()) return;
    setSearching(true);
    try {
      const res = await searchPatients({ query: patientQuery, pageSize: 5 });
      setPatientResults(res.items);
    } catch {
      setPatientResults([]);
    } finally {
      setSearching(false);
    }
  }

  function selectPatient(p: PatientResponse) {
    setSelectedPatient(p);
    setForm((f) => ({ ...f, patientId: p.patientId }));
    setPatientResults([]);
    setPatientQuery('');
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.patientId) { setError('Please select a patient.'); return; }
    if (!form.doctorUserId) { setError('Please select a doctor.'); return; }
    setSaving(true);
    setError('');
    try {
      const appt = await createAppointment(form);
      navigate(`/appointments/${appt.appointmentId}`);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number; data?: { message?: string } } })?.response?.status;
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      if (status === 409) {
        setError('The doctor is not available at this time. Please choose a different slot.');
      } else {
        setError(msg || 'Failed to schedule appointment.');
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="p-6 max-w-2xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/appointments" className="hover:text-blue-600">Appointments</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">Schedule Appointment</span>
      </div>

      <h2 className="text-2xl font-semibold text-gray-800 mb-6">Schedule Appointment</h2>

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
              <button
                type="button"
                onClick={() => { setSelectedPatient(null); setForm((f) => ({ ...f, patientId: '' })); }}
                className="text-xs text-gray-500 hover:text-red-500"
              >
                Change
              </button>
            </div>
          ) : (
            <div className="relative">
              <div className="flex gap-2">
                <input
                  type="text"
                  value={patientQuery}
                  onChange={(e) => setPatientQuery(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), searchForPatient())}
                  placeholder="Search patient by name, MRN, or phone…"
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button
                  type="button"
                  onClick={searchForPatient}
                  disabled={searching}
                  className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-sm rounded-lg text-gray-700 transition-colors"
                >
                  {searching ? '…' : 'Search'}
                </button>
              </div>
              {patientResults.length > 0 && (
                <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                  {patientResults.map((p) => (
                    <button
                      key={p.patientId}
                      type="button"
                      onClick={() => selectPatient(p)}
                      className="w-full text-left px-4 py-2.5 hover:bg-gray-50 text-sm border-b border-gray-100 last:border-0"
                    >
                      <span className="font-medium">{p.fullName}</span>
                      <span className="text-gray-400 font-mono text-xs ml-2">{p.medicalRecordNumber}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}
        </section>

        {/* Appointment details */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Appointment Details</h3>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs text-gray-600 mb-1">Doctor *</label>
              <select
                required
                value={form.doctorUserId}
                onChange={(e) => setForm((f) => ({ ...f, doctorUserId: e.target.value }))}
                className={input}
              >
                <option value="">Select doctor…</option>
                {doctors.map((d) => (
                  <option key={d.userId} value={d.userId}>{d.fullName}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs text-gray-600 mb-1">Type *</label>
              <select
                required
                value={form.appointmentType}
                onChange={(e) => setForm((f) => ({ ...f, appointmentType: e.target.value }))}
                className={input}
              >
                {AppointmentTypes.map((t) => <option key={t}>{t}</option>)}
              </select>
            </div>

            <div>
              <label className="block text-xs text-gray-600 mb-1">Date & Time *</label>
              <input
                required
                type="datetime-local"
                value={form.scheduledAt}
                onChange={(e) => setForm((f) => ({ ...f, scheduledAt: e.target.value }))}
                className={input}
              />
            </div>

            <div>
              <label className="block text-xs text-gray-600 mb-1">Duration (minutes)</label>
              <input
                type="number"
                min={5}
                max={480}
                value={form.durationMinutes}
                onChange={(e) => setForm((f) => ({ ...f, durationMinutes: Number(e.target.value) }))}
                className={input}
              />
            </div>

            <div>
              <label className="block text-xs text-gray-600 mb-1">Room</label>
              <input
                value={form.room}
                onChange={(e) => setForm((f) => ({ ...f, room: e.target.value }))}
                placeholder="e.g. Room 3A"
                className={input}
              />
            </div>

            <div>
              <label className="block text-xs text-gray-600 mb-1">Chief Complaint</label>
              <input
                value={form.chiefComplaint}
                onChange={(e) => setForm((f) => ({ ...f, chiefComplaint: e.target.value }))}
                placeholder="e.g. Chest pain"
                className={input}
              />
            </div>

            <div className="col-span-2">
              <label className="block text-xs text-gray-600 mb-1">Notes</label>
              <textarea
                rows={3}
                value={form.notes}
                onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
                className={`${input} resize-none`}
                placeholder="Additional notes…"
              />
            </div>
          </div>
        </section>

        {error && <p className="text-sm text-red-600 bg-red-50 px-4 py-3 rounded-lg">{error}</p>}

        <div className="flex gap-3 justify-end">
          <Link to="/appointments" className="px-5 py-2 border border-gray-300 rounded-lg text-sm text-gray-600 hover:bg-gray-50 transition-colors">
            Cancel
          </Link>
          <button
            type="submit"
            disabled={saving}
            className="px-5 py-2 bg-blue-700 hover:bg-blue-800 disabled:bg-blue-400 text-white text-sm font-medium rounded-lg transition-colors"
          >
            {saving ? 'Scheduling…' : 'Schedule Appointment'}
          </button>
        </div>
      </form>
    </div>
  );
}

const input = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
