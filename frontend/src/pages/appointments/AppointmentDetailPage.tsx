import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  getAppointment,
  confirmAppointment,
  checkInAppointment,
  startAppointment,
  completeAppointment,
  cancelAppointment,
  noShowAppointment,
} from '../../api/appointments';
import type { AppointmentDetailResponse } from '../../types/appointments';
import { STATUS_COLORS, StatusTransitions } from '../../types/appointments';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const TRANSITION_ACTIONS: Record<string, { label: string; fn: (id: string) => Promise<AppointmentDetailResponse>; style: string }> = {
  Confirmed:  { label: 'Confirm',   fn: confirmAppointment,  style: 'bg-indigo-600 hover:bg-indigo-700 text-white' },
  CheckedIn:  { label: 'Check In',  fn: checkInAppointment,  style: 'bg-yellow-500 hover:bg-yellow-600 text-white' },
  InProgress: { label: 'Start',     fn: startAppointment,    style: 'bg-orange-500 hover:bg-orange-600 text-white' },
  Completed:  { label: 'Complete',  fn: completeAppointment, style: 'bg-green-600 hover:bg-green-700 text-white' },
  NoShow:     { label: 'No Show',   fn: noShowAppointment,   style: 'bg-red-500 hover:bg-red-600 text-white' },
};

export default function AppointmentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();

  const [appt, setAppt] = useState<AppointmentDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [transitioning, setTransitioning] = useState('');
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [cancelReason, setCancelReason] = useState('');

  useEffect(() => {
    if (!id) return;
    getAppointment(id)
      .then(setAppt)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [id]);

  async function handleTransition(toStatus: string) {
    if (!id || !appt) return;
    setTransitioning(toStatus);
    try {
      const updated = await TRANSITION_ACTIONS[toStatus].fn(id);
      setAppt(updated);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      alert(msg || 'Transition failed.');
    } finally {
      setTransitioning('');
    }
  }

  async function handleCancel() {
    if (!id) return;
    setTransitioning('Cancelled');
    try {
      const updated = await cancelAppointment(id, cancelReason || undefined);
      setAppt(updated);
      setShowCancelModal(false);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      alert(msg || 'Cancel failed.');
    } finally {
      setTransitioning('');
    }
  }

  const isDoctorOrAdmin = user && [Roles.Doctor, Roles.SuperAdmin, Roles.Admin].includes(user.role as never);
  const nextStatuses = appt ? StatusTransitions[appt.status] ?? [] : [];

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (!appt) return <div className="p-8 text-red-600">Appointment not found.</div>;

  return (
    <div className="p-6 max-w-3xl">
      {/* Breadcrumb */}
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/appointments" className="hover:text-blue-600">Appointments</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">{appt.patientName}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">{appt.patientName}</h2>
          <p className="text-sm text-gray-500 mt-0.5">
            {new Date(appt.scheduledAt).toLocaleString('en-GB', {
              weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
              hour: '2-digit', minute: '2-digit',
            })}
            {' '}({appt.durationMinutes} min)
          </p>
        </div>
        <span className={`text-sm font-medium px-3 py-1.5 rounded-full ${STATUS_COLORS[appt.status] ?? 'bg-gray-100 text-gray-600'}`}>
          {appt.status}
        </span>
      </div>

      {/* Status actions */}
      {nextStatuses.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-6">
          {nextStatuses
            .filter((s) => s !== 'Cancelled')
            .filter((s) => s === 'InProgress' || s === 'Completed' ? isDoctorOrAdmin : true)
            .map((s) => {
              const action = TRANSITION_ACTIONS[s];
              if (!action) return null;
              return (
                <button
                  key={s}
                  onClick={() => handleTransition(s)}
                  disabled={!!transitioning}
                  className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors disabled:opacity-50 ${action.style}`}
                >
                  {transitioning === s ? 'Updating…' : action.label}
                </button>
              );
            })}
          {nextStatuses.includes('Cancelled') && (
            <button
              onClick={() => setShowCancelModal(true)}
              disabled={!!transitioning}
              className="px-4 py-2 rounded-lg text-sm font-medium border border-red-300 text-red-600 hover:bg-red-50 transition-colors disabled:opacity-50"
            >
              Cancel Appointment
            </button>
          )}
        </div>
      )}

      {/* Details */}
      <div className="grid grid-cols-2 gap-5">
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Appointment</h3>
          <dl className="space-y-2.5">
            <Row label="Type" value={appt.appointmentType} />
            <Row label="Doctor" value={appt.doctorName} />
            <Row label="Room" value={appt.room} />
            <Row label="Chief Complaint" value={appt.chiefComplaint} />
          </dl>
        </section>

        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Patient</h3>
          <dl className="space-y-2.5">
            <Row label="Name" value={appt.patientName} />
            <Row label="MRN" value={appt.medicalRecordNumber} />
          </dl>
          <div className="mt-3">
            <Link to={`/patients/${appt.patientId}`} className="text-sm text-blue-600 hover:underline">
              View Patient Record →
            </Link>
          </div>
        </section>

        {appt.notes && (
          <section className="col-span-2 bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-3">Notes</h3>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{appt.notes}</p>
          </section>
        )}

        {appt.status === 'Cancelled' && (
          <section className="col-span-2 bg-red-50 rounded-xl border border-red-200 p-5">
            <h3 className="text-sm font-semibold text-red-700 uppercase tracking-wide mb-3">Cancellation</h3>
            <p className="text-sm text-red-600">
              Cancelled at {appt.cancelledAt ? new Date(appt.cancelledAt).toLocaleString('en-GB') : '—'}
            </p>
            {appt.cancellationReason && (
              <p className="text-sm text-red-700 mt-1">Reason: {appt.cancellationReason}</p>
            )}
          </section>
        )}

        <section className="col-span-2 bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex justify-between text-xs text-gray-400">
            <span>Created: {new Date(appt.createdAt).toLocaleString('en-GB')}</span>
            <span>Updated: {new Date(appt.updatedAt).toLocaleString('en-GB')}</span>
          </div>
        </section>
      </div>

      {/* Cancel modal */}
      {showCancelModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">Cancel Appointment</h3>
            <label className="block text-sm text-gray-600 mb-2">Reason (optional)</label>
            <textarea
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-400 resize-none"
              placeholder="Enter cancellation reason…"
            />
            <div className="flex gap-3 mt-4 justify-end">
              <button
                onClick={() => setShowCancelModal(false)}
                className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                Back
              </button>
              <button
                onClick={handleCancel}
                disabled={!!transitioning}
                className="px-4 py-2 text-sm font-medium bg-red-600 hover:bg-red-700 text-white rounded-lg disabled:opacity-50 transition-colors"
              >
                {transitioning ? 'Cancelling…' : 'Confirm Cancel'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Row({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div className="flex text-sm">
      <dt className="w-36 text-gray-500 shrink-0">{label}</dt>
      <dd className="text-gray-800">{value || '—'}</dd>
    </div>
  );
}
