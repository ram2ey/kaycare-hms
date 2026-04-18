import { useState, useEffect } from 'react';
import { getPharmacyReport } from '../../../api/reports';
import type { PharmacyReportResponse } from '../../../types/reports';
import { StatCard, BarTable, LoadingState, ErrorState, EmptyState } from '../components';

export default function PharmacyTab({ range }: { range: { from: string; to: string } }) {
  const [data, setData]       = useState<PharmacyReportResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  useEffect(() => {
    setLoading(true); setError('');
    getPharmacyReport(range)
      .then(setData)
      .catch(() => setError('Failed to load pharmacy report.'))
      .finally(() => setLoading(false));
  }, [range.from, range.to]);

  if (loading) return <LoadingState />;
  if (error || !data) return <ErrorState msg={error || 'No data.'} />;
  if (data.totalDispenseEvents === 0 && data.totalDrugs === 0) return <EmptyState />;

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-4 gap-4">
        <StatCard label="Dispense Events" value={data.totalDispenseEvents} />
        <StatCard label="Total Drugs"     value={data.totalDrugs} />
        <StatCard label="Low Stock"       value={data.lowStockItems}
          color={data.lowStockItems > 0 ? 'text-orange-600' : 'text-gray-400'} />
        <StatCard label="Out of Stock"    value={data.outOfStockItems}
          color={data.outOfStockItems > 0 ? 'text-red-600' : 'text-gray-400'} />
      </div>
      <div className="grid grid-cols-2 gap-5">
        <BarTable title="Top Dispensed Drugs (by units)"  rows={data.topDispensedDrugs} />
        <BarTable title="Stock by Category"               rows={data.stockByCategory} />
      </div>
    </div>
  );
}
