import { useState, useEffect } from 'react';
import { getLabCatalogAll, createLabTest, updateLabTest, toggleLabTest } from '../../api/labCatalog';
import type { LabTestCatalog, SaveLabTestRequest } from '../../types/labOrders';
import { DEPARTMENTS } from '../../types/labOrders';

export default function LabCatalogPage() {
  const [catalog, setCatalog] = useState<LabTestCatalog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedDept, setSelectedDept] = useState('All');
  const [selectedStatus, setSelectedStatus] = useState('All');

  // Modal states
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState<LabTestCatalog | null>(null);
  const [saving, setSaving] = useState(false);
  const [actionError, setActionError] = useState('');

  // Form states
  const [testCode, setTestCode] = useState('');
  const [testName, setTestName] = useState('');
  const [department, setDepartment] = useState('Haematology');
  const [instrumentType, setInstrumentType] = useState('');
  const [isManualEntry, setIsManualEntry] = useState(true);
  const [tatHours, setTatHours] = useState(4);
  const [defaultUnit, setDefaultUnit] = useState('');
  const [defaultReferenceRange, setDefaultReferenceRange] = useState('');
  const [criticalReferenceRange, setCriticalReferenceRange] = useState('');
  const [isActive, setIsActive] = useState(true);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const data = await getLabCatalogAll();
      setCatalog(data);
    } catch {
      setError('Failed to load lab test catalog.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  function openCreate() {
    setEditingItem(null);
    setTestCode('');
    setTestName('');
    setDepartment('Haematology');
    setInstrumentType('');
    setIsManualEntry(true);
    setTatHours(4);
    setDefaultUnit('');
    setDefaultReferenceRange('');
    setCriticalReferenceRange('');
    setIsActive(true);
    setShowModal(true);
  }

  function openEdit(item: LabTestCatalog) {
    setEditingItem(item);
    setTestCode(item.testCode);
    setTestName(item.testName);
    setDepartment(item.department);
    setInstrumentType(item.instrumentType ?? '');
    setIsManualEntry(item.isManualEntry);
    setTatHours(item.tatHours);
    setDefaultUnit(item.defaultUnit ?? '');
    setDefaultReferenceRange(item.defaultReferenceRange ?? '');
    setCriticalReferenceRange(item.criticalReferenceRange ?? '');
    setIsActive(item.isActive);
    setShowModal(true);
  }

  async function handleToggle(id: string) {
    setActionError('');
    try {
      await toggleLabTest(id);
      setCatalog(catalog.map(c => c.labTestCatalogId === id ? { ...c, isActive: !c.isActive } : c));
    } catch {
      setActionError('Failed to toggle active status.');
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!testCode.trim() || !testName.trim()) return;

    setSaving(true);
    setActionError('');
    const data: SaveLabTestRequest = {
      testCode: testCode.trim(),
      testName: testName.trim(),
      department,
      instrumentType: instrumentType.trim() || null,
      isManualEntry,
      tatHours: Number(tatHours),
      defaultUnit: defaultUnit.trim() || null,
      defaultReferenceRange: defaultReferenceRange.trim() || null,
      criticalReferenceRange: criticalReferenceRange.trim() || null,
      isActive,
    };

    try {
      if (editingItem) {
        const updated = await updateLabTest(editingItem.labTestCatalogId, data);
        setCatalog(catalog.map(c => c.labTestCatalogId === editingItem.labTestCatalogId ? updated : c));
      } else {
        const created = await createLabTest(data);
        setCatalog([created, ...catalog]);
      }
      setShowModal(false);
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Save failed.';
      setActionError(msg);
    } finally {
      setSaving(false);
    }
  }

  const filtered = catalog.filter(item => {
    const matchesSearch =
      item.testName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      item.testCode.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesDept = selectedDept === 'All' || item.department === selectedDept;
    const matchesStatus =
      selectedStatus === 'All' ||
      (selectedStatus === 'Active' && item.isActive) ||
      (selectedStatus === 'Inactive' && !item.isActive);

    return matchesSearch && matchesDept && matchesStatus;
  });

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-800">Lab Test Catalog</h2>
          <p className="text-sm text-gray-500 mt-1">
            Configure available laboratory tests, reference ranges, turn-around times, and analyzers.
          </p>
        </div>
        <button
          onClick={openCreate}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white rounded-lg text-sm font-semibold transition-colors flex items-center gap-2 shadow-sm self-start md:self-auto"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clipRule="evenodd" />
          </svg>
          Add Lab Test
        </button>
      </div>

      {actionError && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">{actionError}</div>
      )}

      {/* Filter panel */}
      <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm flex flex-wrap gap-4 items-center mb-6">
        <div className="flex-1 min-w-[240px]">
          <input
            type="text"
            placeholder="Search by test name or code..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div className="w-48">
          <select
            value={selectedDept}
            onChange={e => setSelectedDept(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
          >
            <option value="All">All Departments</option>
            {DEPARTMENTS.map(d => (
              <option key={d} value={d}>
                {d}
              </option>
            ))}
          </select>
        </div>
        <div className="w-40">
          <select
            value={selectedStatus}
            onChange={e => setSelectedStatus(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
          >
            <option value="All">All Statuses</option>
            <option value="Active">Active Only</option>
            <option value="Inactive">Inactive Only</option>
          </select>
        </div>
      </div>

      {loading ? (
        <div className="text-gray-400 py-8 text-center">Loading test catalog...</div>
      ) : error ? (
        <div className="text-red-600 py-4 text-center">{error}</div>
      ) : filtered.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-500">
          No tests match your filter criteria.
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100 bg-gray-50 text-gray-500 text-xs font-semibold uppercase tracking-wider">
                <tr>
                  <th className="text-left px-6 py-3.5">Code</th>
                  <th className="text-left px-6 py-3.5">Test Name</th>
                  <th className="text-left px-6 py-3.5">Department</th>
                  <th className="text-left px-6 py-3.5">Analyzer</th>
                  <th className="text-left px-6 py-3.5">Method</th>
                  <th className="text-left px-6 py-3.5">TAT (hrs)</th>
                  <th className="text-left px-6 py-3.5">Unit</th>
                  <th className="text-left px-6 py-3.5">Ref Range</th>
                  <th className="text-left px-6 py-3.5">Status</th>
                  <th className="px-6 py-3.5"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 text-gray-700">
                {filtered.map(item => (
                  <tr key={item.labTestCatalogId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="px-6 py-4 font-bold text-gray-950">{item.testCode}</td>
                    <td className="px-6 py-4 font-medium text-gray-950">{item.testName}</td>
                    <td className="px-6 py-4 text-gray-600">{item.department}</td>
                    <td className="px-6 py-4 text-gray-500">{item.instrumentType || 'Manual'}</td>
                    <td className="px-6 py-4 text-gray-500">
                      {item.isManualEntry ? (
                        <span className="text-xs bg-amber-50 text-amber-700 px-2 py-0.5 rounded font-medium">Manual Entry</span>
                      ) : (
                        <span className="text-xs bg-indigo-50 text-indigo-700 px-2 py-0.5 rounded font-medium">HL7/Analyzer</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-gray-500">{item.tatHours} hrs</td>
                    <td className="px-6 py-4 text-gray-500">{item.defaultUnit || '—'}</td>
                    <td className="px-6 py-4 text-gray-500 font-mono text-xs">{item.defaultReferenceRange || '—'}</td>
                    <td className="px-6 py-4">
                      <button
                        onClick={() => handleToggle(item.labTestCatalogId)}
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold cursor-pointer transition-colors ${
                          item.isActive
                            ? 'bg-green-150 text-green-700 hover:bg-green-200'
                            : 'bg-red-100 text-red-700 hover:bg-red-200'
                        }`}
                      >
                        {item.isActive ? 'Active' : 'Inactive'}
                      </button>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button
                        onClick={() => openEdit(item)}
                        className="text-xs text-blue-600 hover:text-blue-800 font-semibold hover:underline"
                      >
                        Edit
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Save Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/45 flex items-center justify-center z-50 p-4 overflow-y-auto">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-xl max-h-[90vh] flex flex-col">
            <div className="px-6 py-4 border-b border-gray-150 flex items-center justify-between">
              <h3 className="text-lg font-bold text-gray-900">
                {editingItem ? 'Edit Lab Test' : 'Add Lab Test'}
              </h3>
              <button onClick={() => setShowModal(false)} aria-label="Close" className="text-gray-400 hover:text-gray-600">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="lab-test-code" className="block text-xs font-semibold text-gray-600 mb-1">Test Code *</label>
                  <input
                    id="lab-test-code"
                    required
                    maxLength={20}
                    value={testCode}
                    onChange={e => setTestCode(e.target.value)}
                    placeholder="e.g. FBC"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 uppercase font-bold"
                  />
                </div>
                <div>
                  <label htmlFor="lab-test-name" className="block text-xs font-semibold text-gray-600 mb-1">Test Name *</label>
                  <input
                    id="lab-test-name"
                    required
                    maxLength={200}
                    value={testName}
                    onChange={e => setTestName(e.target.value)}
                    placeholder="e.g. Full Blood Count"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="lab-department" className="block text-xs font-semibold text-gray-600 mb-1">Department *</label>
                  <select
                    id="lab-department"
                    value={department}
                    onChange={e => setDepartment(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                  >
                    {DEPARTMENTS.map(d => (
                      <option key={d} value={d}>
                        {d}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label htmlFor="lab-tat-hours" className="block text-xs font-semibold text-gray-600 mb-1">Turn-around Time (hours)</label>
                  <input
                    id="lab-tat-hours"
                    type="number"
                    min={1}
                    max={168}
                    required
                    value={tatHours}
                    onChange={e => setTatHours(Number(e.target.value))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="lab-instrument-type" className="block text-xs font-semibold text-gray-600 mb-1">Analyzer/Instrument Name</label>
                  <input
                    id="lab-instrument-type"
                    value={instrumentType}
                    onChange={e => setInstrumentType(e.target.value)}
                    placeholder="e.g. DxH560, Sysmex (Optional)"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div className="flex items-center pt-6">
                  <label className="inline-flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={isManualEntry}
                      onChange={e => setIsManualEntry(e.target.checked)}
                      className="rounded text-blue-600 focus:ring-blue-500 h-4 w-4 border-gray-300"
                    />
                    <span className="text-sm text-gray-700">Enable manual results entry</span>
                  </label>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label htmlFor="lab-default-unit" className="block text-xs font-semibold text-gray-600 mb-1">Default Unit</label>
                  <input
                    id="lab-default-unit"
                    value={defaultUnit}
                    onChange={e => setDefaultUnit(e.target.value)}
                    placeholder="e.g. g/dL, mmol/L"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label htmlFor="lab-reference-range" className="block text-xs font-semibold text-gray-600 mb-1">Reference Range</label>
                  <input
                    id="lab-reference-range"
                    value={defaultReferenceRange}
                    onChange={e => setDefaultReferenceRange(e.target.value)}
                    placeholder="e.g. 11.5-16.0"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label htmlFor="lab-critical-range" className="block text-xs font-semibold text-gray-600 mb-1">Critical Panic Range</label>
                  <input
                    id="lab-critical-range"
                    value={criticalReferenceRange}
                    onChange={e => setCriticalReferenceRange(e.target.value)}
                    placeholder="e.g. 7.0-20.0"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div className="flex items-center gap-2 pt-2 border-t border-gray-100">
                <input
                  type="checkbox"
                  id="isActiveForm"
                  checked={isActive}
                  onChange={e => setIsActive(e.target.checked)}
                  className="rounded text-blue-600 focus:ring-blue-500 h-4 w-4 border-gray-300"
                />
                <label htmlFor="isActiveForm" className="text-sm text-gray-700 cursor-pointer">
                  Item is active (available for orders)
                </label>
              </div>

              <div className="flex gap-3 justify-end pt-4 border-t border-gray-150">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="px-4 py-2 text-sm font-semibold text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="px-5 py-2 text-sm font-semibold bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors flex items-center gap-2"
                >
                  {saving && (
                    <svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                  )}
                  {editingItem ? 'Save Changes' : 'Create Lab Test'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
