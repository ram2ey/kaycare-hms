import { useState, useEffect } from 'react';
import { getClinicalReport } from '../../../api/reports';
import type { ClinicalReportResponse } from '../../../types/reports';
import { StatCard, BarTable, MiniLineChart, LoadingState, ErrorState, EmptyState } from '../components';

export default function ClinicalTab({ range }: { range: { from: string; to: string } }) {
  const [data, setData]     = useState<ClinicalReportResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]   = useState('');

  useEffect(() => {
    setLoading(true); setError('');
    getClinicalReport(range)
      .then(setData)
      .catch(() => setError('Failed to load clinical report.'))
      .finally(() => setLoading(false));
  }, [range.from, range.to]);

  if (loading) return <LoadingState />;
  if (error || !data) return <ErrorState msg={error || 'No data.'} />;
  if (data.totalConsultations === 0 && data.totalAppointments === 0 && data.newPatients === 0)
    return <EmptyState />;

  const consultPct = data.totalConsultations === 0 ? '—'
    : `${((data.signedConsultations / data.totalConsultations) * 100).toFixed(0)}% signed`;
  const apptPct = data.totalAppointments === 0 ? '—'
    : `${((data.attendedAppointments / data.totalAppointments) * 100).toFixed(0)}% attended`;

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-3 gap-4">
        <StatCard label="Consultations"  value={data.totalConsultations}  sub={consultPct} />
        <StatCard label="Appointments"   value={data.totalAppointments}   sub={apptPct} />
        <StatCard label="New Patients"   value={data.newPatients}         color="text-teal-700" />
      </div>
      <MiniLineChart title="Consultations per Day" data={Array.isArray(data.consultationsByDay) ? data.consultationsByDay : []} />
      <div className="grid grid-cols-2 gap-5">
        <BarTable title="Top Diagnoses"           rows={Array.isArray(data.topDiagnoses) ? data.topDiagnoses : []} />
        <BarTable title="Appointments by Status"  rows={Array.isArray(data.appointmentsByStatus) ? data.appointmentsByStatus : []} />
      </div>
    </div>
  );
}
