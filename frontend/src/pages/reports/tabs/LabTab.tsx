import { useState, useEffect } from 'react';
import { getLabReport } from '../../../api/reports';
import type { LabReportResponse } from '../../../types/reports';
import { StatCard, BarTable, MiniLineChart, LoadingState, ErrorState, EmptyState } from '../components';

export default function LabTab({ range }: { range: { from: string; to: string } }) {
  const [data, setData]       = useState<LabReportResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  useEffect(() => {
    setLoading(true); setError('');
    getLabReport(range)
      .then(setData)
      .catch(() => setError('Failed to load lab report.'))
      .finally(() => setLoading(false));
  }, [range.from, range.to]);

  if (loading) return <LoadingState />;
  if (error || !data) return <ErrorState msg={error || 'No data.'} />;
  if (data.totalOrders === 0) return <EmptyState />;

  const completionRate = data.totalOrders === 0 ? '—'
    : `${((data.completedOrders / data.totalOrders) * 100).toFixed(0)}% completion rate`;

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-3 gap-4">
        <StatCard label="Total Orders"     value={data.totalOrders}     sub={completionRate} />
        <StatCard label="Completed"        value={data.completedOrders} color="text-green-700" />
        <StatCard label="Pending / Active" value={data.pendingOrders}   color={data.pendingOrders > 20 ? 'text-orange-600' : 'text-gray-800'} />
      </div>
      <MiniLineChart title="Lab Orders per Day" data={Array.isArray(data.ordersByDay) ? data.ordersByDay : []} />
      <div className="grid grid-cols-2 gap-5">
        <BarTable title="Top Tests Ordered"  rows={Array.isArray(data.topTests) ? data.topTests : []} />
        <BarTable title="Orders by Status"   rows={Array.isArray(data.ordersByStatus) ? data.ordersByStatus : []} />
      </div>
    </div>
  );
}
