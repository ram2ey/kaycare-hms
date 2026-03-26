import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getOutstanding, getPatientBills } from '../../api/billing';
import { searchPatients } from '../../api/patients';
import type { BillResponse } from '../../types/billing';
import type { PatientResponse } from '../../types/patients';
import { STATUS_COLORS } from '../../types/billing';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const BILLING_ROLES = [Roles.Admin, Roles.SuperAdmin, Roles.Receptionist];

function fmt(amount: number) {
  return `GHS ${amount.toFixed(2)}`;
}

export default function BillingPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const canBill = user && [...BILLING_ROLES, Roles.Doctor].includes(user.role as never);
  const canViewOutstanding = user && BILLING_ROLES.includes(user.role as never);

  const [tab, setTab] = useState<'outstanding' | 'patient'>(canViewOutstanding ? 'outstanding' : 'patient');
  const [outstanding, setOutstanding] = useState<BillResponse[]>([]);
  const [patientBills, setPatientBills] = useState<BillResponse[]>([]);
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [patientQuery, setPatientQuery] = useState('');
  const [patientResults, setPatientResults] = useState<PatientResponse[]>([]);
  const [searching, setSearching] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (tab === 'outstanding' && canViewOutstanding) {
      setLoading(true);
      getOutstanding().then(setOutstanding).catch(() => {}).finally(() => setLoading(false));
    }
  }, [tab, canViewOutstanding]);

  async function searchForPatient() {
    if (!patientQuery.trim()) return;
    setSearching(true);
    try {
      const res = await searchPatients({ query: patientQuery, pageSize: 5 });
      setPatientResults(res.items);
    } finally {
      setSearching(false);
    }
  }

  async function selectPatient(p: PatientResponse) {
    setSelectedPatient(p);
    setPatientResults([]);
    setPatientQuery('');
    setLoading(true);
    try {
      setPatientBills(await getPatientBills(p.patientId));
    } finally {
      setLoading(false);
    }
  }

  const displayList = tab === 'outstanding' ? outstanding : patientBills;

  // Summary totals for outstanding tab
  const totalOutstanding = outstanding.reduce((s, b) => s + b.balanceDue, 0);

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Billing</h2>
          {tab === 'outstanding' && outstanding.length > 0 && (
            <p className="text-sm text-gray-500 mt-0.5">
              {outstanding.length} bill{outstanding.length !== 1 ? 's' : ''} · Total outstanding: <span className="font-semibold text-red-600">{fmt(totalOutstanding)}</span>
            </p>
          )}
        </div>
        {canBill && (
          <button
            onClick={() => navigate('/billing/new')}
            className="bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
          >
            + New Bill
          </button>
        )}
      </div>

      {/* Tabs */}
      <div className="flex rounded-lg border border-gray-200 overflow-hidden text-sm mb-5 w-fit">
        {canViewOutstanding && (
          <button
            onClick={() => setTab('outstanding')}
            className={`px-5 py-2 ${tab === 'outstanding' ? 'bg-blue-700 text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
          >
            Outstanding
          </button>
        )}
        <button
          onClick={() => setTab('patient')}
          className={`px-5 py-2 ${tab === 'patient' ? 'bg-blue-700 text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
        >
          By Patient
        </button>
      </div>

      {/* Patient search */}
      {tab === 'patient' && (
        <div className="bg-white rounded-xl border border-gray-200 p-5 mb-5">
          {selectedPatient ? (
            <div className="flex items-center justify-between bg-blue-50 rounded-lg px-4 py-3">
              <div>
                <p className="font-medium text-gray-800">{selectedPatient.fullName}</p>
                <p className="text-xs text-blue-600 font-mono">{selectedPatient.medicalRecordNumber}</p>
              </div>
              <button onClick={() => { setSelectedPatient(null); setPatientBills([]); }} className="text-xs text-gray-500 hover:text-red-500">Change</button>
            </div>
          ) : (
            <div className="relative">
              <div className="flex gap-2">
                <input
                  type="text" value={patientQuery}
                  onChange={(e) => setPatientQuery(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), searchForPatient())}
                  placeholder="Search patient by name, MRN, or phone…"
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button onClick={searchForPatient} disabled={searching}
                  className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-sm rounded-lg text-gray-700 transition-colors">
                  {searching ? '…' : 'Search'}
                </button>
              </div>
              {patientResults.length > 0 && (
                <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                  {patientResults.map((p) => (
                    <button key={p.patientId} onClick={() => selectPatient(p)}
                      className="w-full text-left px-4 py-2.5 hover:bg-gray-50 text-sm border-b border-gray-100 last:border-0">
                      <span className="font-medium">{p.fullName}</span>
                      <span className="text-gray-400 font-mono text-xs ml-2">{p.medicalRecordNumber}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* Bills table */}
      {(tab === 'outstanding' || selectedPatient) && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Invoice #</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Patient</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Date</th>
                <th className="text-right px-5 py-3 font-medium text-gray-600">Total</th>
                <th className="text-right px-5 py-3 font-medium text-gray-600">Paid</th>
                <th className="text-right px-5 py-3 font-medium text-gray-600">Balance</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {loading && (
                <tr><td colSpan={7} className="px-5 py-8 text-center text-gray-400">Loading…</td></tr>
              )}
              {!loading && displayList.length === 0 && (
                <tr><td colSpan={7} className="px-5 py-8 text-center text-gray-400">
                  {tab === 'outstanding' ? 'No outstanding bills.' : 'No bills found.'}
                </td></tr>
              )}
              {!loading && displayList.map((bill) => (
                <tr key={bill.billId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3">
                    <Link to={`/billing/${bill.billId}`} className="font-mono text-xs text-blue-700 hover:underline">
                      {bill.billNumber}
                    </Link>
                  </td>
                  <td className="px-5 py-3">
                    <Link to={`/patients/${bill.patientId}`} className="font-medium text-gray-800 hover:text-blue-700">
                      {bill.patientName}
                    </Link>
                    <div className="text-xs text-gray-400 font-mono">{bill.medicalRecordNumber}</div>
                  </td>
                  <td className="px-5 py-3 text-gray-600">
                    {new Date(bill.createdAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}
                  </td>
                  <td className="px-5 py-3 text-right text-gray-700">{fmt(bill.totalAmount)}</td>
                  <td className="px-5 py-3 text-right text-green-700">{fmt(bill.paidAmount)}</td>
                  <td className={`px-5 py-3 text-right font-semibold ${bill.balanceDue > 0 ? 'text-red-600' : 'text-gray-400'}`}>
                    {fmt(bill.balanceDue)}
                  </td>
                  <td className="px-5 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[bill.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {bill.status}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'patient' && !selectedPatient && (
        <div className="text-center py-16 text-gray-400 text-sm">
          Search for a patient to view their billing history.
        </div>
      )}
    </div>
  );
}
