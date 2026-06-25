import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getPatientBills } from '../../../api/billing';
import type { BillResponse } from '../../../types/billing';
import { useAuth } from '../../../contexts/AuthContext';
import { Roles } from '../../../types';

const STATUS_COLORS: Record<string, string> = {
  Draft:          'bg-gray-100 text-gray-600',
  Issued:         'bg-blue-100 text-blue-700',
  PartiallyPaid:  'bg-yellow-100 text-yellow-700',
  Paid:           'bg-green-100 text-green-700',
  Cancelled:      'bg-gray-100 text-gray-500',
  WrittenOff:     'bg-red-100 text-red-600',
  Void:           'bg-gray-100 text-gray-400',
};

export default function BillingTab({ patientId }: { patientId: string }) {
  const { user } = useAuth();
  const [bills, setBills] = useState<BillResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getPatientBills(patientId)
      .then((data) => setBills(Array.isArray(data) ? data : []))
      .catch(() => setBills([]))
      .finally(() => setLoading(false));
  }, [patientId]);

  const canCreate = [Roles.Doctor, Roles.Admin, Roles.SuperAdmin, Roles.Receptionist, Roles.BillingOfficer].includes(user?.role as never);

  const totalOutstanding = bills
    .filter(b => b.status === 'Issued' || b.status === 'PartiallyPaid')
    .reduce((sum, b) => sum + b.balanceDue, 0);

  if (loading) return <div className="text-gray-400 text-sm">Loading…</div>;

  return (
    <div className="max-w-4xl">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-4">
          <h3 className="text-sm font-semibold text-gray-700">Bills ({bills.length})</h3>
          {totalOutstanding > 0 && (
            <span className="text-xs font-medium bg-red-100 text-red-700 px-2 py-0.5 rounded-full">
              GHS {totalOutstanding.toFixed(2)} outstanding
            </span>
          )}
        </div>
        {canCreate && (
          <Link to={`/billing/new?patientId=${patientId}`}
            className="px-3 py-1.5 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium">
            + New Bill
          </Link>
        )}
      </div>

      {bills.length === 0 ? (
        <p className="text-sm text-gray-400">No bills on record.</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Date</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Bill #</th>
                <th className="text-right px-4 py-3 text-xs font-medium text-gray-500">Total</th>
                <th className="text-right px-4 py-3 text-xs font-medium text-gray-500">Paid</th>
                <th className="text-right px-4 py-3 text-xs font-medium text-gray-500">Balance</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Status</th>
                <th />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {bills.map(b => (
                <tr key={b.billId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-600">{new Date(b.createdAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3 font-mono text-xs text-gray-700">{b.billNumber}</td>
                  <td className="px-4 py-3 text-right text-gray-800">GHS {b.totalAmount.toFixed(2)}</td>
                  <td className="px-4 py-3 text-right text-gray-600">GHS {b.paidAmount.toFixed(2)}</td>
                  <td className={`px-4 py-3 text-right font-medium ${b.balanceDue > 0 ? 'text-red-600' : 'text-gray-600'}`}>
                    GHS {b.balanceDue.toFixed(2)}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[b.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {b.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link to={`/billing/${b.billId}`} className="text-blue-600 hover:underline text-xs">View</Link>
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
