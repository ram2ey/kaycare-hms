import { useState, useEffect, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getCalendar, listDoctors } from '../../api/appointments';
import type { AppointmentResponse, UserSummary } from '../../types/appointments';
import { STATUS_COLORS, AppointmentStatuses } from '../../types/appointments';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

function weekStart(d: Date) {
  const dt = new Date(d);
  const day = dt.getDay(); // 0=Sun
  dt.setDate(dt.getDate() - ((day + 6) % 7)); // Monday
  dt.setHours(0, 0, 0, 0);
  return dt;
}

function addDays(d: Date, n: number) {
  const dt = new Date(d);
  dt.setDate(dt.getDate() + n);
  return dt;
}

function fmtDate(d: Date) {
  return d.toISOString().slice(0, 10);
}

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

const SCHEDULER_ROLES = [Roles.SuperAdmin, Roles.Admin, Roles.Doctor, Roles.Nurse, Roles.Receptionist];

export default function AppointmentsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [view, setView] = useState<'calendar' | 'list'>('calendar');
  const [weekOf, setWeekOf] = useState(() => weekStart(new Date()));
  const [selectedDoctor, setSelectedDoctor] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [doctors, setDoctors] = useState<UserSummary[]>([]);
  const [appointments, setAppointments] = useState<AppointmentResponse[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    listDoctors().then(setDoctors).catch(() => {});
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const from = fmtDate(weekOf);
      const to = fmtDate(addDays(weekOf, 7));
      const data = await getCalendar({
        doctorUserId: selectedDoctor || undefined,
        from,
        to,
        status: statusFilter || undefined,
      });
      setAppointments(data);
    } catch {
      setAppointments([]);
    } finally {
      setLoading(false);
    }
  }, [weekOf, selectedDoctor, statusFilter]);

  useEffect(() => { load(); }, [load]);

  // Group by day for calendar view
  const byDay: Record<string, AppointmentResponse[]> = {};
  for (let i = 0; i < 7; i++) {
    const key = fmtDate(addDays(weekOf, i));
    byDay[key] = [];
  }
  for (const appt of appointments) {
    const key = appt.scheduledAt.slice(0, 10);
    if (byDay[key]) byDay[key].push(appt);
  }

  const canSchedule = user && SCHEDULER_ROLES.includes(user.role as never);

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Appointments</h2>
        <div className="flex gap-2">
          {canSchedule && (
            <button
              onClick={() => navigate('/appointments/new')}
              className="bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
            >
              + Schedule
            </button>
          )}
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-5">
        <div className="flex rounded-lg border border-gray-200 overflow-hidden text-sm">
          <button
            onClick={() => setView('calendar')}
            className={`px-4 py-2 ${view === 'calendar' ? 'bg-blue-700 text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
          >
            Calendar
          </button>
          <button
            onClick={() => setView('list')}
            className={`px-4 py-2 ${view === 'list' ? 'bg-blue-700 text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
          >
            List
          </button>
        </div>

        <select
          value={selectedDoctor}
          onChange={(e) => setSelectedDoctor(e.target.value)}
          className="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
        >
          <option value="">All Doctors</option>
          {doctors.map((d) => (
            <option key={d.userId} value={d.userId}>{d.fullName}</option>
          ))}
        </select>

        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
        >
          <option value="">All Statuses</option>
          {AppointmentStatuses.map((s) => <option key={s}>{s}</option>)}
        </select>
      </div>

      {/* Week navigation */}
      <div className="flex items-center gap-4 mb-4">
        <button
          onClick={() => setWeekOf((w) => addDays(w, -7))}
          className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm hover:bg-gray-50"
        >
          ← Prev
        </button>
        <span className="text-sm font-medium text-gray-700">
          {weekOf.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}
          {' — '}
          {addDays(weekOf, 6).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}
        </span>
        <button
          onClick={() => setWeekOf((w) => addDays(w, 7))}
          className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm hover:bg-gray-50"
        >
          Next →
        </button>
        <button
          onClick={() => setWeekOf(weekStart(new Date()))}
          className="px-3 py-1.5 text-sm text-blue-600 hover:underline"
        >
          Today
        </button>
      </div>

      {loading && <p className="text-sm text-gray-400 mb-4">Loading…</p>}

      {/* Calendar view */}
      {view === 'calendar' && (
        <div className="grid grid-cols-7 gap-3">
          {DAYS.map((dayName, i) => {
            const date = addDays(weekOf, i);
            const key = fmtDate(date);
            const isToday = key === fmtDate(new Date());
            const dayAppts = byDay[key] ?? [];

            return (
              <div key={key} className="min-h-40">
                <div className={`text-center text-xs font-medium mb-2 py-1 rounded-lg ${isToday ? 'bg-blue-700 text-white' : 'text-gray-500'}`}>
                  <div>{dayName}</div>
                  <div className="text-base font-semibold">{date.getDate()}</div>
                </div>
                <div className="space-y-1.5">
                  {dayAppts.map((a) => (
                    <Link
                      key={a.appointmentId}
                      to={`/appointments/${a.appointmentId}`}
                      className={`block rounded-lg px-2 py-1.5 text-xs hover:opacity-80 transition-opacity ${STATUS_COLORS[a.status] ?? 'bg-gray-100 text-gray-600'}`}
                    >
                      <div className="font-medium">
                        {new Date(a.scheduledAt).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}
                      </div>
                      <div className="truncate">{a.patientName}</div>
                      <div className="text-gray-500 truncate">{a.doctorName}</div>
                    </Link>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* List view */}
      {view === 'list' && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Date & Time</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Patient</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Doctor</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Type</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Status</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Room</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {appointments.length === 0 && !loading && (
                <tr>
                  <td colSpan={6} className="px-5 py-8 text-center text-gray-400">No appointments found.</td>
                </tr>
              )}
              {appointments.map((a) => (
                <tr key={a.appointmentId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3 text-gray-700">
                    {new Date(a.scheduledAt).toLocaleString('en-GB', {
                      day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit',
                    })}
                    <span className="text-gray-400 text-xs ml-1">({a.durationMinutes}m)</span>
                  </td>
                  <td className="px-5 py-3">
                    <Link to={`/patients/${a.patientId}`} className="font-medium text-gray-800 hover:text-blue-700">
                      {a.patientName}
                    </Link>
                    <div className="text-xs text-gray-400 font-mono">{a.medicalRecordNumber}</div>
                  </td>
                  <td className="px-5 py-3 text-gray-600">{a.doctorName}</td>
                  <td className="px-5 py-3 text-gray-600">{a.appointmentType}</td>
                  <td className="px-5 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[a.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {a.status}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-gray-500">{a.room ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
