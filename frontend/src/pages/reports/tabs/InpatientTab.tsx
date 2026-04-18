import { useState, useEffect } from 'react';
import { getInpatientReport } from '../../../api/reports';
import type { InpatientReportResponse } from '../../../types/reports';
import { StatCard, BarTable, MiniLineChart, LoadingState, ErrorState, EmptyState } from '../components';

export default function InpatientTab({ range }: { range: { from: string; to: string } }) {
  const [data, setData]       = useState<InpatientReportResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  useEffect(() => {
    setLoading(true); setError('');
    getInpatientReport(range)
      .then(setData)
      .catch(() => setError('Failed to load inpatient report.'))
      .finally(() => setLoading(false));
  }, [range.from, range.to]);

  if (loading) return <LoadingState />;
  if (error || !data) return <ErrorState msg={error || 'No data.'} />;
  if (data.totalAdmissions === 0 && data.discharges === 0) return <EmptyState />;

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-3 gap-4">
        <StatCard label="Total Admissions"  value={data.totalAdmissions}  sub={`${data.activeAdmissions} currently active`} />
        <StatCard label="Discharges"        value={data.discharges} />
        <StatCard label="Avg Length of Stay" value={`${data.averageLengthOfStayDays} days`} />
        <StatCard label="Total Beds"        value={data.totalBeds} />
        <StatCard label="Occupied Beds"     value={data.occupiedBeds}    sub={`${data.occupancyRate}% occupancy`}
          color={data.occupancyRate >= 90 ? 'text-red-600' : data.occupancyRate >= 70 ? 'text-orange-600' : 'text-teal-700'} />
      </div>
      <MiniLineChart title="Admissions per Day" data={data.admissionsByDay} />
      <div className="grid grid-cols-2 gap-5">
        <BarTable title="Admissions by Ward"   rows={data.admissionsByWard} />
        <BarTable title="Discharges by Type"   rows={data.dischargesByType} />
      </div>
    </div>
  );
}
