import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { getTestCatalog, placeLabOrder } from '../../api/labOrders';
import { searchPatients } from '../../api/patients';
import type { LabTestCatalog } from '../../types/labOrders';
import type { PatientResponse } from '../../types/patients';
import { DEPARTMENTS } from '../../types/labOrders';

export default function PlaceLabOrderPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const prePatientId = searchParams.get('patientId') ?? '';

  const [catalog, setCatalog] = useState<LabTestCatalog[]>([]);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [deptFilter, setDeptFilter] = useState('');

  // Patient search
  const [patientQuery, setPatientQuery] = useState('');
  const [patientResults, setPatientResults] = useState<PatientResponse[]>([]);
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [searchingPatient, setSearchingPatient] = useState(false);

  const [organisation, setOrganisation] = useState('DIRECT');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    getTestCatalog().then(setCatalog);
  }, []);

  // Pre-select patient from query param
  useEffect(() => {
    if (prePatientId && catalog.length > 0) {
      searchPatients({ query: prePatientId }).then(res => {
        const p = res.items.find(x => x.patientId === prePatientId);
        if (p) setSelectedPatient(p);
      });
    }
  }, [prePatientId, catalog]);

  const searchPatient = async (q: string) => {
    setPatientQuery(q);
    if (q.trim().length < 2) { setPatientResults([]); return; }
    setSearchingPatient(true);
    const res = await searchPatients({ query: q });
    setPatientResults(res.items);
    setSearchingPatient(false);
  };

  const toggleTest = (id: string) =>
    setSelectedIds(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });

  const filteredCatalog = deptFilter
    ? catalog.filter(t => t.department === deptFilter)
    : catalog;

  const grouped = filteredCatalog.reduce<Record<string, LabTestCatalog[]>>((acc, t) => {
    (acc[t.department] ??= []).push(t);
    return acc;
  }, {});

  const handleSubmit = async () => {
    if (!selectedPatient) { setError('Select a patient.'); return; }
    if (selectedIds.size === 0) { setError('Select at least one test.'); return; }
    setSaving(true);
    setError('');
    try {
      const order = await placeLabOrder({
        patientId: selectedPatient.patientId,
        organisation,
        notes: notes.trim() || undefined,
        testIds: [...selectedIds],
      });
      navigate(`/lab-orders/${order.labOrderId}`);
    } catch {
      setError('Failed to place order. Please try again.');
      setSaving(false);
    }
  };

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-800">Place Lab Order</h1>
        <p className="text-gray-500 text-sm mt-1">Select a patient and choose the tests to order.</p>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-red-700 text-sm">{error}</div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* ── Left: Patient + Details ─────────────────────────────────────── */}
        <div className="space-y-4">
          <div className="bg-white border rounded-lg p-4">
            <h2 className="text-sm font-semibold text-gray-700 mb-3">Patient</h2>
            {selectedPatient ? (
              <div>
                <div className="font-medium text-gray-800">{selectedPatient.fullName}</div>
                <div className="text-xs text-gray-500 mt-1">MRN: {selectedPatient.medicalRecordNumber}</div>
                <button
                  onClick={() => { setSelectedPatient(null); setPatientQuery(''); }}
                  className="text-xs text-blue-600 mt-2 hover:underline"
                >
                  Change patient
                </button>
              </div>
            ) : (
              <div className="relative">
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm"
                  placeholder="Search by name or MRN…"
                  value={patientQuery}
                  onChange={e => searchPatient(e.target.value)}
                />
                {searchingPatient && (
                  <div className="absolute top-full left-0 right-0 bg-white border rounded shadow-lg z-10 p-2 text-xs text-gray-400">
                    Searching…
                  </div>
                )}
                {patientResults.length > 0 && (
                  <div className="absolute top-full left-0 right-0 bg-white border rounded shadow-lg z-10 max-h-48 overflow-y-auto">
                    {patientResults.map(p => (
                      <button
                        key={p.patientId}
                        className="w-full text-left px-3 py-2 text-sm hover:bg-blue-50"
                        onClick={() => { setSelectedPatient(p); setPatientResults([]); setPatientQuery(''); }}
                      >
                        <div className="font-medium">{p.fullName}</div>
                        <div className="text-xs text-gray-400">{p.medicalRecordNumber}</div>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>

          <div className="bg-white border rounded-lg p-4">
            <h2 className="text-sm font-semibold text-gray-700 mb-3">Organisation</h2>
            <select
              value={organisation}
              onChange={e => setOrganisation(e.target.value)}
              className="w-full border rounded px-3 py-2 text-sm"
            >
              <option value="DIRECT">DIRECT (Walk-in)</option>
              <option value="REFERRED">Referred</option>
            </select>
            {organisation === 'REFERRED' && (
              <input
                type="text"
                className="w-full border rounded px-3 py-2 text-sm mt-2"
                placeholder="Referring facility name…"
                onChange={e => setOrganisation(e.target.value)}
              />
            )}
          </div>

          <div className="bg-white border rounded-lg p-4">
            <h2 className="text-sm font-semibold text-gray-700 mb-3">Notes</h2>
            <textarea
              className="w-full border rounded px-3 py-2 text-sm resize-none h-20"
              placeholder="Optional clinical notes…"
              value={notes}
              onChange={e => setNotes(e.target.value)}
            />
          </div>

          <div className="bg-white border rounded-lg p-4">
            <h2 className="text-sm font-semibold text-gray-700 mb-1">Selected Tests</h2>
            <p className="text-xs text-gray-400 mb-2">{selectedIds.size} test(s) selected</p>
            {selectedIds.size === 0 ? (
              <p className="text-xs text-gray-400 italic">None selected</p>
            ) : (
              <ul className="space-y-1">
                {catalog
                  .filter(t => selectedIds.has(t.labTestCatalogId))
                  .map(t => (
                    <li key={t.labTestCatalogId} className="text-xs flex justify-between">
                      <span>{t.testName}</span>
                      <button
                        onClick={() => toggleTest(t.labTestCatalogId)}
                        className="text-red-400 hover:text-red-600 ml-2"
                      >
                        ×
                      </button>
                    </li>
                  ))}
              </ul>
            )}
          </div>

          <button
            onClick={handleSubmit}
            disabled={saving || selectedIds.size === 0 || !selectedPatient}
            className="w-full py-2.5 bg-blue-600 text-white rounded-lg font-medium text-sm hover:bg-blue-700 disabled:opacity-50"
          >
            {saving ? 'Placing Order…' : 'Place Order'}
          </button>
        </div>

        {/* ── Right: Test catalog ─────────────────────────────────────────── */}
        <div className="md:col-span-2">
          <div className="bg-white border rounded-lg">
            <div className="p-4 border-b flex gap-3 items-center">
              <h2 className="text-sm font-semibold text-gray-700">Test Catalog</h2>
              <select
                value={deptFilter}
                onChange={e => setDeptFilter(e.target.value)}
                className="ml-auto border rounded px-2 py-1 text-xs"
              >
                <option value="">All Departments</option>
                {DEPARTMENTS.map(d => <option key={d} value={d}>{d}</option>)}
              </select>
            </div>

            <div className="divide-y max-h-[600px] overflow-y-auto">
              {Object.entries(grouped).map(([dept, tests]) => (
                <div key={dept}>
                  <div className="px-4 py-2 bg-gray-50 text-xs font-semibold text-gray-500 uppercase tracking-wide">
                    {dept}
                  </div>
                  {tests.map(t => (
                    <label
                      key={t.labTestCatalogId}
                      className="flex items-center px-4 py-3 hover:bg-blue-50 cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={selectedIds.has(t.labTestCatalogId)}
                        onChange={() => toggleTest(t.labTestCatalogId)}
                        className="mr-3 accent-blue-600"
                      />
                      <div className="flex-1">
                        <div className="text-sm font-medium text-gray-800">{t.testName}</div>
                        <div className="text-xs text-gray-400">
                          {t.isManualEntry ? 'Manual entry' : t.instrumentType} · TAT {t.tatHours}h
                        </div>
                      </div>
                      {t.isManualEntry && (
                        <span className="text-xs bg-orange-100 text-orange-700 px-2 py-0.5 rounded">
                          Manual
                        </span>
                      )}
                    </label>
                  ))}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
