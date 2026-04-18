import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getLabOrdersByPatient } from '../../../api/labOrders';
import type { LabOrder } from '../../../types/labOrders';
import { useAuth } from '../../../contexts/AuthContext';
import { Roles } from '../../../types';

const STATUS_COLORS: Record<string, string> = {
  Ordered:    'bg-blue-100 text-blue-700',
  Received:   'bg-yellow-100 text-yellow-700',
  Resulted:   'bg-green-100 text-green-700',
  Signed:     'bg-gray-100 text-gray-600',
  Cancelled:  'bg-red-100 text-red-600',
};

export default function LabTab({ patientId }: { patientId: string }) {
  const { user } = useAuth();
  const [orders, setOrders] = useState<LabOrder[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getLabOrdersByPatient(patientId).then(setOrders).finally(() => setLoading(false));
  }, [patientId]);

  const canCreate = [Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user?.role as never);

  if (loading) return <div className="text-gray-400 text-sm">Loading…</div>;

  return (
    <div className="max-w-4xl">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold text-gray-700">Lab Orders ({orders.length})</h3>
        {canCreate && (
          <Link to={`/lab-orders/new?patientId=${patientId}`}
            className="px-3 py-1.5 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium">
            + New Lab Order
          </Link>
        )}
      </div>

      {orders.length === 0 ? (
        <p className="text-sm text-gray-400">No lab orders on record.</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Date</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Accession</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Tests</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Organisation</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Status</th>
                <th />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {orders.map(o => (
                <tr key={o.labOrderId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-600">{new Date(o.orderedAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3 font-mono text-xs text-gray-700">—</td>
                  <td className="px-4 py-3 text-gray-800">{o.testNames.join(', ')}</td>
                  <td className="px-4 py-3 text-gray-500">{o.organisation}</td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[o.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {o.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link to={`/lab-orders/${o.labOrderId}`} className="text-blue-600 hover:underline text-xs">View</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
