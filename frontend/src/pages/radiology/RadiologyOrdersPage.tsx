import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getRadiologyWorklist } from '../../api/radiology'
import type { RadiologyOrderSummary } from '../../types/radiology'
import { ORDER_STATUS_COLORS } from '../../types/radiology'

export function RadiologyOrdersPage() {
  const navigate = useNavigate()
  const [orders, setOrders] = useState<RadiologyOrderSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [date, setDate] = useState(() => new Date().toISOString().split('T')[0])
  const [statusFilter, setStatusFilter] = useState('')

  function load() {
    setLoading(true)
    getRadiologyWorklist(date, statusFilter || undefined)
      .then(setOrders)
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [date, statusFilter])

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Radiology Orders</h1>
          <p className="text-sm text-gray-500 mt-0.5">{orders.length} order{orders.length !== 1 ? 's' : ''} today</p>
        </div>
        <button
          onClick={() => navigate('/radiology/new')}
          className="bg-sky-700 hover:bg-sky-800 text-white px-4 py-2 rounded-lg text-sm font-medium"
        >
          + New Order
        </button>
      </div>

      {/* Filters */}
      <div className="flex gap-3 mb-5">
        <input
          type="date"
          value={date}
          onChange={(e) => setDate(e.target.value)}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
        />
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
        >
          <option value="">All statuses</option>
          <option value="Pending">Pending</option>
          <option value="Scheduled">Scheduled</option>
          <option value="InProgress">In Progress</option>
          <option value="Completed">Completed</option>
          <option value="Signed">Signed</option>
        </select>
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-400 text-sm">Loading…</div>
        ) : orders.length === 0 ? (
          <div className="p-8 text-center text-gray-400 text-sm">No radiology orders found.</div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 font-medium">Patient</th>
                <th className="px-5 py-3 font-medium">Procedures</th>
                <th className="px-5 py-3 font-medium">Priority</th>
                <th className="px-5 py-3 font-medium">Status</th>
                <th className="px-5 py-3 font-medium">Referring Dr.</th>
                <th className="px-5 py-3 font-medium">Ordered</th>
                <th className="px-5 py-3 font-medium">Progress</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <tr
                  key={order.radiologyOrderId}
                  onClick={() => navigate(`/radiology/${order.radiologyOrderId}`)}
                  className="border-b border-gray-50 hover:bg-gray-50 cursor-pointer"
                >
                  <td className="px-5 py-3">
                    <p className="font-medium text-gray-800">{order.patientName}</p>
                    <p className="text-xs text-gray-400 font-mono">{order.patientMrn}</p>
                  </td>
                  <td className="px-5 py-3 text-gray-600 text-xs max-w-48">
                    {order.procedureNames.join(', ')}
                  </td>
                  <td className="px-5 py-3">
                    <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium ${
                      order.priority === 'STAT' ? 'bg-red-100 text-red-700 border border-red-200' :
                      order.priority === 'Urgent' ? 'bg-orange-100 text-orange-700' :
                      'bg-gray-150 text-gray-600'
                    }`}>
                      {order.priority === 'STAT' && (
                        <span className="relative flex h-1.5 w-1.5">
                          <span className="absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75 animate-ping"></span>
                          <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-red-500"></span>
                        </span>
                      )}
                      {order.priority}
                    </span>
                  </td>
                  <td className="px-5 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${ORDER_STATUS_COLORS[order.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {order.status}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-gray-500 text-xs">{order.orderingDoctorName}</td>
                  <td className="px-5 py-3 text-gray-400 text-xs">
                    {new Date(order.orderedAt).toLocaleString('en-GB', { dateStyle: 'short', timeStyle: 'short' })}
                  </td>
                  <td className="px-5 py-3 text-xs text-gray-500">
                    {order.signedCount}/{order.signedCount + order.reportedCount + order.incompleteCount} signed
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
