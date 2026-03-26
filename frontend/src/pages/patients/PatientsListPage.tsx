import { useState, useEffect, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { searchPatients } from '../../api/patients';
import type { PatientResponse, PatientSearchRequest } from '../../types/patients';
import type { PagedResult } from '../../types';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const REGISTRAR_ROLES = [Roles.SuperAdmin, Roles.Admin, Roles.Doctor, Roles.Nurse, Roles.Receptionist];

export default function PatientsListPage() {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [query, setQuery] = useState('');
  const [dob, setDob] = useState('');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<PagedResult<PatientResponse> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const fetch = useCallback(async (params: PatientSearchRequest) => {
    setLoading(true);
    setError('');
    try {
      const data = await searchPatients(params);
      setResult(data);
    } catch {
      setError('Failed to load patients.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetch({ query, dateOfBirth: dob || undefined, page, pageSize: 20 });
  }, [fetch, query, dob, page]);

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    setPage(1);
    fetch({ query, dateOfBirth: dob || undefined, page: 1, pageSize: 20 });
  }

  const totalPages = result ? Math.ceil(result.totalCount / 20) : 1;
  const canRegister = user && REGISTRAR_ROLES.includes(user.role as never);

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Patients</h2>
          {result && (
            <p className="text-sm text-gray-500 mt-0.5">{result.totalCount} total</p>
          )}
        </div>
        {canRegister && (
          <button
            onClick={() => navigate('/patients/new')}
            className="bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
          >
            + Register Patient
          </button>
        )}
      </div>

      {/* Search bar */}
      <form onSubmit={handleSearch} className="flex gap-3 mb-6">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search by name, MRN, or phone…"
          className="flex-1 px-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <input
          type="date"
          value={dob}
          onChange={(e) => setDob(e.target.value)}
          className="px-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <button
          type="submit"
          className="bg-gray-800 hover:bg-gray-900 text-white text-sm px-5 py-2 rounded-lg transition-colors"
        >
          Search
        </button>
      </form>

      {/* Error */}
      {error && <p className="text-sm text-red-600 mb-4">{error}</p>}

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-5 py-3 font-medium text-gray-600">MRN</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Full Name</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Date of Birth</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Age</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Gender</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Phone</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Flags</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {loading && (
              <tr>
                <td colSpan={7} className="px-5 py-8 text-center text-gray-400">
                  Loading…
                </td>
              </tr>
            )}
            {!loading && result?.items.length === 0 && (
              <tr>
                <td colSpan={7} className="px-5 py-8 text-center text-gray-400">
                  No patients found.
                </td>
              </tr>
            )}
            {!loading &&
              result?.items.map((p) => (
                <tr key={p.patientId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3 font-mono text-xs text-blue-700">
                    <Link to={`/patients/${p.patientId}`}>{p.medicalRecordNumber}</Link>
                  </td>
                  <td className="px-5 py-3">
                    <Link
                      to={`/patients/${p.patientId}`}
                      className="font-medium text-gray-800 hover:text-blue-700"
                    >
                      {p.fullName}
                    </Link>
                  </td>
                  <td className="px-5 py-3 text-gray-600">{p.dateOfBirth}</td>
                  <td className="px-5 py-3 text-gray-600">{p.age}</td>
                  <td className="px-5 py-3 text-gray-600">{p.gender}</td>
                  <td className="px-5 py-3 text-gray-600">{p.phoneNumber ?? '—'}</td>
                  <td className="px-5 py-3 flex gap-1.5">
                    {p.hasAllergies && (
                      <span className="inline-block bg-red-100 text-red-700 text-xs px-2 py-0.5 rounded-full font-medium">
                        Allergy
                      </span>
                    )}
                    {!p.isActive && (
                      <span className="inline-block bg-gray-100 text-gray-500 text-xs px-2 py-0.5 rounded-full">
                        Inactive
                      </span>
                    )}
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
          <span>
            Page {page} of {totalPages}
          </span>
          <div className="flex gap-2">
            <button
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="px-3 py-1.5 border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50"
            >
              Previous
            </button>
            <button
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1.5 border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
