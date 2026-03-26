import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { createConsultation } from '../../api/consultations';
import { getPatientAppointments } from '../../api/appointments';
import type { AppointmentResponse } from '../../types/appointments';

export default function CreateConsultationPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const patientId = searchParams.get('patientId') ?? '';

  const [appointments, setAppointments] = useState<AppointmentResponse[]>([]);
  const [selectedAppointmentId, setSelectedAppointmentId] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!patientId) return;
    getPatientAppointments(patientId)
      .then((data) => {
        // Only InProgress or CheckedIn appointments can start a consultation
        const eligible = data.filter((a) =>
          a.status === 'InProgress' || a.status === 'CheckedIn' || a.status === 'Confirmed' || a.status === 'Scheduled'
        );
        setAppointments(eligible);
        if (eligible.length === 1) setSelectedAppointmentId(eligible[0].appointmentId);
      })
      .catch(() => {});
  }, [patientId]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedAppointmentId) { setError('Please select an appointment.'); return; }
    setSaving(true);
    setError('');
    try {
      const consultation = await createConsultation({ appointmentId: selectedAppointmentId });
      navigate(`/consultations/${consultation.consultationId}`);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number; data?: { message?: string } } })?.response?.status;
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      if (status === 409) {
        setError('A consultation already exists for this appointment.');
      } else {
        setError(msg || 'Failed to create consultation.');
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="p-6 max-w-xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/consultations" className="hover:text-blue-600">Consultations</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">New Consultation</span>
      </div>

      <h2 className="text-2xl font-semibold text-gray-800 mb-6">Start Consultation</h2>

      <form onSubmit={handleSubmit} className="bg-white rounded-xl border border-gray-200 p-5 space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Select Appointment *</label>
          {appointments.length === 0 ? (
            <p className="text-sm text-gray-400 py-3">
              No eligible appointments found for this patient.{' '}
              <Link to="/appointments/new" className="text-blue-600 hover:underline">Schedule one first.</Link>
            </p>
          ) : (
            <div className="space-y-2">
              {appointments.map((a) => (
                <label
                  key={a.appointmentId}
                  className={`flex items-start gap-3 p-3 border rounded-lg cursor-pointer transition-colors ${
                    selectedAppointmentId === a.appointmentId
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                  }`}
                >
                  <input
                    type="radio"
                    name="appointment"
                    value={a.appointmentId}
                    checked={selectedAppointmentId === a.appointmentId}
                    onChange={() => setSelectedAppointmentId(a.appointmentId)}
                    className="mt-0.5"
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-800">
                      {new Date(a.scheduledAt).toLocaleString('en-GB', {
                        weekday: 'short', day: 'numeric', month: 'short',
                        hour: '2-digit', minute: '2-digit',
                      })}
                    </p>
                    <p className="text-xs text-gray-500 mt-0.5">
                      {a.appointmentType} · {a.doctorName} · {a.status}
                    </p>
                    {a.chiefComplaint && (
                      <p className="text-xs text-gray-400 mt-0.5">{a.chiefComplaint}</p>
                    )}
                  </div>
                </label>
              ))}
            </div>
          )}
        </div>

        {error && <p className="text-sm text-red-600 bg-red-50 px-4 py-3 rounded-lg">{error}</p>}

        <div className="flex gap-3 justify-end pt-2">
          <Link to="/consultations" className="px-5 py-2 border border-gray-300 rounded-lg text-sm text-gray-600 hover:bg-gray-50 transition-colors">
            Cancel
          </Link>
          <button
            type="submit"
            disabled={saving || appointments.length === 0}
            className="px-5 py-2 bg-blue-700 hover:bg-blue-800 disabled:bg-blue-400 text-white text-sm font-medium rounded-lg transition-colors"
          >
            {saving ? 'Creating…' : 'Start Consultation'}
          </button>
        </div>
      </form>
    </div>
  );
}
