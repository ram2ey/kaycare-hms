import { useState, useEffect } from 'react';
import { getImagingCatalogAll, createImagingProcedure, updateImagingProcedure, toggleImagingProcedure } from '../../api/imagingCatalog';
import type { ImagingProcedureItem, SaveImagingProcedureRequest } from '../../types/radiology';

const MODALITIES = ['X-Ray', 'CT', 'MRI', 'Ultrasound', 'Nuclear Medicine', 'Fluoroscopy', 'Mammography', 'Other'];

export default function ImagingCatalogPage() {
  const [catalog, setCatalog] = useState<ImagingProcedureItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedModality, setSelectedModality] = useState('All');
  const [selectedStatus, setSelectedStatus] = useState('All');

  // Modal states
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState<ImagingProcedureItem | null>(null);
  const [saving, setSaving] = useState(false);
  const [actionError, setActionError] = useState('');

  // Form states
  const [procedureCode, setProcedureCode] = useState('');
  const [procedureName, setProcedureName] = useState('');
  const [modality, setModality] = useState('X-Ray');
  const [bodyPart, setBodyPart] = useState('');
  const [department, setDepartment] = useState('Radiology');
  const [tatHours, setTatHours] = useState(4);
  const [isActive, setIsActive] = useState(true);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const data = await getImagingCatalogAll();
      setCatalog(data);
    } catch {
      setError('Failed to load imaging procedure catalog.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  function openCreate() {
    setEditingItem(null);
    setProcedureCode('');
    setProcedureName('');
    setModality('X-Ray');
    setBodyPart('');
    setDepartment('Radiology');
    setTatHours(4);
    setIsActive(true);
    setShowModal(true);
  }

  function openEdit(item: ImagingProcedureItem) {
    setEditingItem(item);
    setProcedureCode(item.procedureCode);
    setProcedureName(item.procedureName);
    setModality(item.modality);
    setBodyPart(item.bodyPart);
    setDepartment(item.department);
    setTatHours(item.tatHours);
    setIsActive(item.isActive);
    setShowModal(true);
  }

  async function handleToggle(id: string) {
    setActionError('');
    try {
      await toggleImagingProcedure(id);
      setCatalog(catalog.map(c => c.imagingProcedureId === id ? { ...c, isActive: !c.isActive } : c));
    } catch {
      setActionError('Failed to toggle active status.');
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!procedureCode.trim() || !procedureName.trim() || !bodyPart.trim()) return;

    setSaving(true);
    setActionError('');
    const data: SaveImagingProcedureRequest = {
      procedureCode: procedureCode.trim(),
      procedureName: procedureName.trim(),
      modality,
      bodyPart: bodyPart.trim(),
      department: department.trim(),
      tatHours: Number(tatHours),
      isActive,
    };

    try {
      if (editingItem) {
        const updated = await updateImagingProcedure(editingItem.imagingProcedureId, data);
        setCatalog(catalog.map(c => c.imagingProcedureId === editingItem.imagingProcedureId ? updated : c));
      } else {
        const created = await createImagingProcedure(data);
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
      item.procedureName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      item.procedureCode.toLowerCase().includes(searchQuery.toLowerCase()) ||
      item.bodyPart.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesModality = selectedModality === 'All' || item.modality === selectedModality;
    const matchesStatus =
      selectedStatus === 'All' ||
      (selectedStatus === 'Active' && item.isActive) ||
      (selectedStatus === 'Inactive' && !item.isActive);

    return matchesSearch && matchesModality && matchesStatus;
  });

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-800">Imaging Procedure Catalog</h2>
          <p className="text-sm text-gray-500 mt-1">
            Configure radiology services, modalities (X-Ray, CT, MRI, Ultrasound), and turn-around times.
          </p>
        </div>
        <button
          onClick={openCreate}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white rounded-lg text-sm font-semibold transition-colors flex items-center gap-2 shadow-sm self-start md:self-auto"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clipRule="evenodd" />
          </svg>
          Add Imaging Procedure
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
            placeholder="Search by procedure name, code, body part..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div className="w-48">
          <select
            value={selectedModality}
            onChange={e => setSelectedModality(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
          >
            <option value="All">All Modalities</option>
            {MODALITIES.map(m => (
              <option key={m} value={m}>
                {m}
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
        <div className="text-gray-400 py-8 text-center">Loading imaging catalog...</div>
      ) : error ? (
        <div className="text-red-600 py-4 text-center">{error}</div>
      ) : filtered.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-500">
          No procedures match your filter criteria.
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100 bg-gray-50 text-gray-500 text-xs font-semibold uppercase tracking-wider">
                <tr>
                  <th className="text-left px-6 py-3.5">Code</th>
                  <th className="text-left px-6 py-3.5">Procedure Name</th>
                  <th className="text-left px-6 py-3.5">Modality</th>
                  <th className="text-left px-6 py-3.5">Body Part</th>
                  <th className="text-left px-6 py-3.5">Department</th>
                  <th className="text-left px-6 py-3.5">TAT (hrs)</th>
                  <th className="text-left px-6 py-3.5">Status</th>
                  <th className="px-6 py-3.5"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 text-gray-700">
                {filtered.map(item => (
                  <tr key={item.imagingProcedureId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="px-6 py-4 font-bold text-gray-950">{item.procedureCode}</td>
                    <td className="px-6 py-4 font-medium text-gray-950">{item.procedureName}</td>
                    <td className="px-6 py-4 text-gray-600">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-semibold bg-blue-50 text-blue-700">
                        {item.modality}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-gray-500">{item.bodyPart}</td>
                    <td className="px-6 py-4 text-gray-500">{item.department}</td>
                    <td className="px-6 py-4 text-gray-500">{item.tatHours} hrs</td>
                    <td className="px-6 py-4">
                      <button
                        onClick={() => handleToggle(item.imagingProcedureId)}
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
                {editingItem ? 'Edit Imaging Procedure' : 'Add Imaging Procedure'}
              </h3>
              <button onClick={() => setShowModal(false)} className="text-gray-400 hover:text-gray-600">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Procedure Code *</label>
                  <input
                    required
                    maxLength={20}
                    value={procedureCode}
                    onChange={e => setProcedureCode(e.target.value)}
                    placeholder="e.g. XR-CHEST"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 uppercase font-bold"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Procedure Name *</label>
                  <input
                    required
                    maxLength={200}
                    value={procedureName}
                    onChange={e => setProcedureName(e.target.value)}
                    placeholder="e.g. Chest X-Ray PA View"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Modality *</label>
                  <select
                    value={modality}
                    onChange={e => setModality(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                  >
                    {MODALITIES.map(m => (
                      <option key={m} value={m}>
                        {m}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Body Part *</label>
                  <input
                    required
                    maxLength={100}
                    value={bodyPart}
                    onChange={e => setBodyPart(e.target.value)}
                    placeholder="e.g. Chest, Abdomen, Brain"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Department *</label>
                  <input
                    required
                    maxLength={100}
                    value={department}
                    onChange={e => setDepartment(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Turn-around Time (hours)</label>
                  <input
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

              <div className="flex items-center gap-2 pt-2 border-t border-gray-100">
                <input
                  type="checkbox"
                  id="isActiveForm"
                  checked={isActive}
                  onChange={e => setIsActive(e.target.checked)}
                  className="rounded text-blue-600 focus:ring-blue-500 h-4 w-4 border-gray-300"
                />
                <label htmlFor="isActiveForm" className="text-sm text-gray-700 cursor-pointer">
                  Procedure is active (available for radiology orders)
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
                  {editingItem ? 'Save Changes' : 'Create Procedure'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
