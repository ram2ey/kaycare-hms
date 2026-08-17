import { useState, useEffect } from 'react';
import {
  getWards,
  createWard,
  updateWard,
  deactivateWard,
  getBeds,
  addBed,
  updateBed,
  updateBedStatus,
  deleteBed
} from '../../api/inpatient';
import type { WardResponse, BedResponse, SaveWardRequest, SaveBedRequest } from '../../types/inpatient';
import { WARD_TYPES, BED_STATUS_COLORS, BED_STATUS_OPTIONS } from '../../types/inpatient';
import { ConfirmDialog } from '../../components/ConfirmDialog';

function fmt(n: number) {
  return `GHS ${n.toFixed(2)}`;
}

export default function WardSetupPage() {
  const [wards, setWards] = useState<WardResponse[]>([]);
  const [selectedWard, setSelectedWard] = useState<WardResponse | null>(null);
  const [beds, setBeds] = useState<BedResponse[]>([]);
  const [loadingWards, setLoadingWards] = useState(true);
  const [loadingBeds, setLoadingBeds] = useState(false);
  const [errorWards, setErrorWards] = useState('');
  const [errorBeds, setErrorBeds] = useState('');
  const [actionError, setActionError] = useState('');
  const [confirmAction, setConfirmAction] = useState<null | { message: string; run: () => void }>(null);
  const [confirmBusy, setConfirmBusy] = useState(false);

  // Filtering wards
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState('All');
  const [selectedStatus, setSelectedStatus] = useState('All');

  // Ward Modal states
  const [showWardModal, setShowWardModal] = useState(false);
  const [editingWard, setEditingWard] = useState<WardResponse | null>(null);
  const [savingWard, setSavingWard] = useState(false);

  // Ward Form states
  const [wardName, setWardName] = useState('');
  const [wardType, setWardType] = useState('General');
  const [wardDailyRate, setWardDailyRate] = useState(0);
  const [wardDescription, setWardDescription] = useState('');
  const [wardIsActive, setWardIsActive] = useState(true);

  // Bed Modal states
  const [showBedModal, setShowBedModal] = useState(false);
  const [editingBed, setEditingBed] = useState<BedResponse | null>(null);
  const [savingBed, setSavingBed] = useState(false);

  // Bed Form states
  const [bedNumber, setBedNumber] = useState('');
  const [bedNotes, setBedNotes] = useState('');

  // Bed Status Modal states
  const [showStatusModal, setShowStatusModal] = useState(false);
  const [statusBed, setStatusBed] = useState<BedResponse | null>(null);
  const [bedStatus, setBedStatus] = useState('Available');
  const [savingStatus, setSavingStatus] = useState(false);

  async function loadWards() {
    setLoadingWards(true);
    setErrorWards('');
    try {
      // Admins should see all wards (active and inactive)
      const data = await getWards({ activeOnly: false });
      setWards(data);
      // Auto-select first ward or preserve selection
      if (data.length > 0) {
        if (selectedWard) {
          const updatedSelected = data.find(w => w.wardId === selectedWard.wardId);
          setSelectedWard(updatedSelected || data[0]);
        } else {
          setSelectedWard(data[0]);
        }
      } else {
        setSelectedWard(null);
      }
    } catch {
      setErrorWards('Failed to load wards.');
    } finally {
      setLoadingWards(false);
    }
  }

  async function loadBeds(wardId: string) {
    setLoadingBeds(true);
    setErrorBeds('');
    try {
      const data = await getBeds(wardId);
      setBeds(data);
    } catch {
      setErrorBeds('Failed to load beds for the selected ward.');
    } finally {
      setLoadingBeds(false);
    }
  }

  useEffect(() => {
    loadWards();
  }, []);

  useEffect(() => {
    if (selectedWard) {
      loadBeds(selectedWard.wardId);
    } else {
      setBeds([]);
    }
  }, [selectedWard?.wardId]);

  // Ward CRUD handlers
  function openCreateWard() {
    setEditingWard(null);
    setWardName('');
    setWardType('General');
    setWardDailyRate(0);
    setWardDescription('');
    setWardIsActive(true);
    setShowWardModal(true);
  }

  function openEditWard(w: WardResponse) {
    setEditingWard(w);
    setWardName(w.name);
    setWardType(w.wardType);
    setWardDailyRate(w.dailyRate);
    setWardDescription(w.description || '');
    setWardIsActive(w.isActive);
    setShowWardModal(true);
  }

  function handleDeactivateWard(w: WardResponse) {
    setActionError('');
    if (w.occupiedBeds > 0) {
      setActionError('Cannot deactivate a ward with occupied beds.');
      return;
    }
    setConfirmAction({
      message: `Are you sure you want to deactivate ward "${w.name}"?`,
      run: () => doDeactivateWard(w),
    });
  }

  async function doDeactivateWard(w: WardResponse) {
    setConfirmBusy(true);
    setActionError('');
    try {
      await deactivateWard(w.wardId);
      await loadWards();
    } catch {
      setActionError('Failed to deactivate ward.');
    } finally {
      setConfirmBusy(false);
    }
  }

  async function handleWardSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!wardName.trim()) return;

    setSavingWard(true);
    setActionError('');
    const data: SaveWardRequest = {
      name: wardName.trim(),
      wardType,
      dailyRate: Number(wardDailyRate),
      description: wardDescription.trim() || undefined,
      isActive: wardIsActive,
    };

    try {
      if (editingWard) {
        const updated = await updateWard(editingWard.wardId, data);
        setWards(wards.map(w => w.wardId === editingWard.wardId ? { ...w, ...updated } : w));
        if (selectedWard?.wardId === editingWard.wardId) {
          setSelectedWard(updated);
        }
      } else {
        const created = await createWard(data);
        setWards([created, ...wards]);
        setSelectedWard(created);
      }
      setShowWardModal(false);
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Save failed.';
      setActionError(msg);
    } finally {
      setSavingWard(false);
    }
  }

  // Bed CRUD handlers
  function openCreateBed() {
    if (!selectedWard) return;
    setEditingBed(null);
    setBedNumber('');
    setBedNotes('');
    setShowBedModal(true);
  }

  function openEditBed(b: BedResponse) {
    setEditingBed(b);
    setBedNumber(b.bedNumber);
    setBedNotes(b.notes || '');
    setShowBedModal(true);
  }

  async function handleBedSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedWard || !bedNumber.trim()) return;

    setSavingBed(true);
    setActionError('');
    const data: SaveBedRequest = {
      bedNumber: bedNumber.trim(),
      notes: bedNotes.trim() || undefined,
    };

    try {
      if (editingBed) {
        const updated = await updateBed(editingBed.bedId, data);
        setBeds(beds.map(b => b.bedId === editingBed.bedId ? updated : b));
      } else {
        const created = await addBed(selectedWard.wardId, data);
        setBeds([...beds, created]);
      }
      // Reload wards list to update bed counts
      const wardsList = await getWards({ activeOnly: false });
      setWards(wardsList);
      const updatedSelected = wardsList.find(w => w.wardId === selectedWard.wardId);
      if (updatedSelected) setSelectedWard(updatedSelected);

      setShowBedModal(false);
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Save failed.';
      setActionError(msg);
    } finally {
      setSavingBed(false);
    }
  }

  function handleDeleteBed(b: BedResponse) {
    setActionError('');
    if (b.status === 'Occupied') {
      setActionError('Cannot delete an occupied bed.');
      return;
    }
    setConfirmAction({
      message: `Are you sure you want to delete Bed "${b.bedNumber}"?`,
      run: () => doDeleteBed(b),
    });
  }

  async function doDeleteBed(b: BedResponse) {
    setConfirmBusy(true);
    setActionError('');
    try {
      await deleteBed(b.bedId);
      setBeds(beds.filter(x => x.bedId !== b.bedId));

      // Reload wards list to update bed counts
      const wardsList = await getWards({ activeOnly: false });
      setWards(wardsList);
      if (selectedWard) {
        const updatedSelected = wardsList.find(w => w.wardId === selectedWard.wardId);
        if (updatedSelected) setSelectedWard(updatedSelected);
      }
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Delete failed.';
      setActionError(msg);
    } finally {
      setConfirmBusy(false);
    }
  }

  // Bed Status handlers
  function openChangeStatus(b: BedResponse) {
    setStatusBed(b);
    setBedStatus(b.status);
    setBedNotes(b.notes || '');
    setShowStatusModal(true);
  }

  async function handleStatusSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!statusBed) return;

    setSavingStatus(true);
    setActionError('');
    try {
      const updated = await updateBedStatus(statusBed.bedId, {
        status: bedStatus,
        notes: bedNotes.trim() || undefined,
      });
      setBeds(beds.map(b => b.bedId === statusBed.bedId ? updated : b));

      // Reload wards list to update bed counts
      const wardsList = await getWards({ activeOnly: false });
      setWards(wardsList);
      if (selectedWard) {
        const updatedSelected = wardsList.find(w => w.wardId === selectedWard.wardId);
        if (updatedSelected) setSelectedWard(updatedSelected);
      }

      setShowStatusModal(false);
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Status update failed.';
      setActionError(msg);
    } finally {
      setSavingStatus(false);
    }
  }

  const filteredWards = wards.filter(w => {
    const matchesSearch = w.name.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesType = selectedType === 'All' || w.wardType === selectedType;
    const matchesStatus =
      selectedStatus === 'All' ||
      (selectedStatus === 'Active' && w.isActive) ||
      (selectedStatus === 'Inactive' && !w.isActive);
    return matchesSearch && matchesType && matchesStatus;
  });

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-800">Ward & Bed Configuration</h2>
          <p className="text-sm text-gray-500 mt-1">
            Create and manage inpatient wards, configure accommodation rates, and set up bed capacities.
          </p>
        </div>
        <button
          onClick={openCreateWard}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white rounded-lg text-sm font-semibold transition-colors flex items-center gap-2 shadow-sm self-start md:self-auto"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clipRule="evenodd" />
          </svg>
          Add Ward
        </button>
      </div>

      {actionError && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">{actionError}</div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Wards Section - Left Panel */}
        <div className="lg:col-span-2 space-y-4">
          <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm flex flex-wrap gap-3 items-center">
            <div className="flex-1 min-w-[200px]">
              <input
                type="text"
                placeholder="Search wards by name..."
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div className="w-40">
              <select
                value={selectedType}
                onChange={e => setSelectedType(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
              >
                <option value="All">All Types</option>
                {WARD_TYPES.map(t => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </div>
            <div className="w-36">
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

          {loadingWards ? (
            <div className="text-gray-400 py-8 text-center bg-white rounded-xl border border-gray-200">Loading wards...</div>
          ) : errorWards ? (
            <div className="text-red-600 py-8 text-center bg-white rounded-xl border border-gray-200">{errorWards}</div>
          ) : filteredWards.length === 0 ? (
            <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-500">
              No wards match the filters.
            </div>
          ) : (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="border-b border-gray-100 bg-gray-50 text-gray-500 text-xs font-semibold uppercase tracking-wider">
                    <tr>
                      <th className="text-left px-5 py-3">Ward Name</th>
                      <th className="text-left px-5 py-3">Type</th>
                      <th className="text-right px-5 py-3">Daily Rate</th>
                      <th className="text-center px-5 py-3">Beds (A/O/M/Total)</th>
                      <th className="text-left px-5 py-3">Status</th>
                      <th className="px-5 py-3"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 text-gray-700">
                    {filteredWards.map(w => {
                      const isSelected = selectedWard?.wardId === w.wardId;
                      return (
                        <tr
                          key={w.wardId}
                          onClick={() => setSelectedWard(w)}
                          className={`cursor-pointer transition-colors hover:bg-gray-50/50 ${isSelected ? 'bg-blue-50/50 hover:bg-blue-50' : ''}`}
                        >
                          <td className="px-5 py-3.5">
                            <div className="font-bold text-gray-950">{w.name}</div>
                            {w.description && <div className="text-xs text-gray-400 mt-0.5 line-clamp-1">{w.description}</div>}
                          </td>
                          <td className="px-5 py-3.5">
                            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-gray-100 text-gray-800">
                              {w.wardType}
                            </span>
                          </td>
                          <td className="px-5 py-3.5 text-right font-semibold text-gray-900">{fmt(w.dailyRate)}</td>
                          <td className="px-5 py-3.5 text-center text-xs font-mono">
                            <span className="text-green-650 font-bold">{w.availableBeds}</span>
                            <span className="text-gray-400 mx-1">/</span>
                            <span className="text-red-650 font-bold">{w.occupiedBeds}</span>
                            <span className="text-gray-400 mx-1">/</span>
                            <span className="text-yellow-650 font-bold">{w.maintenanceBeds}</span>
                            <span className="text-gray-400 mx-1">/</span>
                            <span className="text-gray-700 font-bold">{w.totalBeds}</span>
                          </td>
                          <td className="px-5 py-3.5">
                            <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold ${w.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-150 text-gray-550'}`}>
                              {w.isActive ? 'Active' : 'Inactive'}
                            </span>
                          </td>
                          <td className="px-5 py-3.5 text-right space-x-3" onClick={e => e.stopPropagation()}>
                            <button
                              onClick={() => openEditWard(w)}
                              className="text-xs text-blue-650 hover:text-blue-800 font-semibold hover:underline"
                            >
                              Edit
                            </button>
                            {w.isActive && (
                              <button
                                onClick={() => handleDeactivateWard(w)}
                                className="text-xs text-red-600 hover:text-red-800 font-semibold hover:underline"
                              >
                                Deactivate
                              </button>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>

        {/* Beds Section - Right Panel */}
        <div className="space-y-4">
          {selectedWard ? (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-4 space-y-4">
              <div className="flex items-center justify-between pb-3 border-b border-gray-100">
                <div>
                  <h3 className="font-bold text-gray-800">{selectedWard.name} — Bed List</h3>
                  <p className="text-xs text-gray-500 mt-0.5">{selectedWard.wardType} Ward · Daily Rate: {fmt(selectedWard.dailyRate)}</p>
                </div>
                <button
                  disabled={!selectedWard.isActive}
                  onClick={openCreateBed}
                  className="px-2.5 py-1.5 bg-blue-700 hover:bg-blue-800 disabled:opacity-40 text-white rounded-md text-xs font-bold transition-colors flex items-center gap-1 shadow-sm"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clipRule="evenodd" />
                  </svg>
                  Add Bed
                </button>
              </div>

              {loadingBeds ? (
                <div className="text-gray-400 py-6 text-center text-sm">Loading beds...</div>
              ) : errorBeds ? (
                <div className="text-red-500 py-6 text-center text-sm">{errorBeds}</div>
              ) : beds.length === 0 ? (
                <div className="text-gray-400 py-10 text-center text-sm">
                  No beds configured in this ward. Click "Add Bed" to create one.
                </div>
              ) : (
                <div className="divide-y divide-gray-100 max-h-[60vh] overflow-y-auto space-y-0.5">
                  {beds.map(b => (
                    <div key={b.bedId} className="py-3 flex items-start justify-between gap-3 hover:bg-gray-50/50 px-2 rounded-lg transition-colors">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="font-bold text-gray-950 text-sm">Bed {b.bedNumber}</span>
                          <span
                            onClick={() => openChangeStatus(b)}
                            className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold cursor-pointer select-none transition-colors ${BED_STATUS_COLORS[b.status] || 'bg-gray-100 text-gray-650 hover:bg-gray-200'}`}
                          >
                            {b.status}
                          </span>
                        </div>
                        {b.notes && <p className="text-xs text-gray-500">{b.notes}</p>}
                      </div>

                      <div className="flex items-center gap-2 pt-0.5">
                        <button
                          onClick={() => openEditBed(b)}
                          className="text-xs text-blue-650 hover:text-blue-800 hover:underline font-semibold"
                        >
                          Edit
                        </button>
                        <span className="text-gray-300 text-xs">|</span>
                        <button
                          disabled={b.status === 'Occupied'}
                          onClick={() => handleDeleteBed(b)}
                          className="text-xs text-red-600 hover:text-red-800 hover:underline font-semibold disabled:opacity-40"
                        >
                          Delete
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ) : (
            <div className="bg-gray-50 rounded-xl border border-dashed border-gray-300 p-8 text-center text-gray-400 text-sm">
              Select a ward on the left to manage its bed inventory.
            </div>
          )}
        </div>
      </div>

      {/* Ward Save Modal */}
      {showWardModal && (
        <div className="fixed inset-0 bg-black/45 flex items-center justify-center z-50 p-4 overflow-y-auto">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg flex flex-col">
            <div className="px-6 py-4 border-b border-gray-150 flex items-center justify-between">
              <h3 className="text-lg font-bold text-gray-900">
                {editingWard ? 'Edit Ward' : 'Add Ward'}
              </h3>
              <button onClick={() => setShowWardModal(false)} className="text-gray-400 hover:text-gray-600">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <form onSubmit={handleWardSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Ward Name *</label>
                <input
                  required
                  maxLength={100}
                  value={wardName}
                  onChange={e => setWardName(e.target.value)}
                  placeholder="e.g. Male Medical Ward, Pediatric ICU"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Ward Type *</label>
                  <select
                    value={wardType}
                    onChange={e => setWardType(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                  >
                    {WARD_TYPES.map(t => (
                      <option key={t} value={t}>{t}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Daily Accommodation Rate (GHS) *</label>
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    required
                    value={wardDailyRate}
                    onChange={e => setWardDailyRate(Number(e.target.value))}
                    placeholder="0.00"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Description / Location</label>
                <textarea
                  maxLength={500}
                  rows={3}
                  value={wardDescription}
                  onChange={e => setWardDescription(e.target.value)}
                  placeholder="e.g. Block B, Second Floor"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="flex items-center gap-2 pt-2">
                <input
                  type="checkbox"
                  id="wardIsActiveForm"
                  checked={wardIsActive}
                  onChange={e => setWardIsActive(e.target.checked)}
                  className="rounded text-blue-600 focus:ring-blue-500 h-4 w-4 border-gray-300"
                />
                <label htmlFor="wardIsActiveForm" className="text-sm text-gray-700 cursor-pointer">
                  Ward is active (available for admissions/transfers)
                </label>
              </div>

              {actionError && <p className="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{actionError}</p>}

              <div className="flex gap-3 justify-end pt-4 border-t border-gray-150">
                <button
                  type="button"
                  onClick={() => setShowWardModal(false)}
                  className="px-4 py-2 text-sm font-semibold text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={savingWard}
                  className="px-5 py-2 text-sm font-semibold bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors flex items-center gap-2"
                >
                  {savingWard && (
                    <svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                  )}
                  {editingWard ? 'Save Changes' : 'Create Ward'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Bed Add/Edit Modal */}
      {showBedModal && (
        <div className="fixed inset-0 bg-black/45 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md flex flex-col">
            <div className="px-6 py-4 border-b border-gray-150 flex items-center justify-between">
              <h3 className="text-lg font-bold text-gray-900">
                {editingBed ? `Edit Bed ${editingBed.bedNumber}` : 'Add New Bed'}
              </h3>
              <button onClick={() => setShowBedModal(false)} className="text-gray-400 hover:text-gray-600">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <form onSubmit={handleBedSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Bed Number *</label>
                <input
                  required
                  maxLength={20}
                  value={bedNumber}
                  onChange={e => setBedNumber(e.target.value)}
                  placeholder="e.g. M-101, ICU-04"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 font-bold uppercase"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Notes / Equipment</label>
                <textarea
                  maxLength={200}
                  rows={2}
                  value={bedNotes}
                  onChange={e => setBedNotes(e.target.value)}
                  placeholder="e.g. Oxygen Concentrator attached, Near Window"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              {actionError && <p className="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{actionError}</p>}

              <div className="flex gap-3 justify-end pt-4 border-t border-gray-150">
                <button
                  type="button"
                  onClick={() => setShowBedModal(false)}
                  className="px-4 py-2 text-sm font-semibold text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={savingBed}
                  className="px-5 py-2 text-sm font-semibold bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors flex items-center gap-2"
                >
                  {savingBed && (
                    <svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                  )}
                  {editingBed ? 'Save Changes' : 'Create Bed'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Bed Status Change Modal */}
      {showStatusModal && statusBed && (
        <div className="fixed inset-0 bg-black/45 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md flex flex-col">
            <div className="px-6 py-4 border-b border-gray-150 flex items-center justify-between">
              <h3 className="text-lg font-bold text-gray-900">
                Update Bed Status — {statusBed.bedNumber}
              </h3>
              <button onClick={() => setShowStatusModal(false)} className="text-gray-400 hover:text-gray-600">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <form onSubmit={handleStatusSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Status *</label>
                <select
                  value={bedStatus}
                  onChange={e => setBedStatus(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                >
                  {BED_STATUS_OPTIONS.map(opt => (
                    <option
                      key={opt}
                      value={opt}
                      disabled={opt === 'Occupied' && statusBed.status !== 'Occupied'}
                    >
                      {opt} {opt === 'Occupied' && statusBed.status !== 'Occupied' ? '(Set only by admitting patient)' : ''}
                    </option>
                  ))}
                </select>
                {statusBed.status === 'Occupied' && bedStatus !== 'Occupied' && (
                  <p className="text-xs text-amber-600 mt-1">
                    Warning: Manually setting an occupied bed to Available/Maintenance does not discharge the patient.
                  </p>
                )}
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Notes / Reason for Status Change</label>
                <textarea
                  maxLength={200}
                  rows={2}
                  value={bedNotes}
                  onChange={e => setBedNotes(e.target.value)}
                  placeholder="e.g. Undergoing cleaning, electrical repair needed"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              {actionError && <p className="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{actionError}</p>}

              <div className="flex gap-3 justify-end pt-4 border-t border-gray-150">
                <button
                  type="button"
                  onClick={() => setShowStatusModal(false)}
                  className="px-4 py-2 text-sm font-semibold text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={savingStatus}
                  className="px-5 py-2 text-sm font-semibold bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors flex items-center gap-2"
                >
                  {savingStatus && (
                    <svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                  )}
                  Update Status
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={!!confirmAction}
        title="Confirm"
        message={confirmAction?.message ?? ''}
        danger
        busy={confirmBusy}
        onConfirm={() => { confirmAction?.run(); setConfirmAction(null); }}
        onCancel={() => setConfirmAction(null)}
      />
    </div>
  );
}
