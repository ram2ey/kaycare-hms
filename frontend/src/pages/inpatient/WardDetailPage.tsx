import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getWard, getBeds, addBed, updateBed, updateBedStatus, deleteBed } from '../../api/inpatient';
import type { WardResponse, BedResponse, SaveBedRequest, UpdateBedStatusRequest } from '../../types/inpatient';
import { BED_STATUS_COLORS, BED_STATUS_OPTIONS } from '../../types/inpatient';
import { useAuth } from '../../contexts/AuthContext';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

export default function WardDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin' || user?.role === 'SuperAdmin';
  const canUpdateStatus = isAdmin || user?.role === 'Nurse';

  const [ward, setWard]       = useState<WardResponse | null>(null);
  const [beds, setBeds]       = useState<BedResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  // Add bed modal
  const [showAdd, setShowAdd]       = useState(false);
  const [addForm, setAddForm]       = useState<SaveBedRequest>({ bedNumber: '', notes: '' });
  const [addSaving, setAddSaving]   = useState(false);
  const [addError, setAddError]     = useState('');

  // Edit bed modal
  const [editBed, setEditBed]       = useState<BedResponse | null>(null);
  const [editForm, setEditForm]     = useState<SaveBedRequest>({ bedNumber: '', notes: '' });
  const [editSaving, setEditSaving] = useState(false);
  const [editError, setEditError]   = useState('');

  // Status modal
  const [statusBed, setStatusBed]       = useState<BedResponse | null>(null);
  const [statusForm, setStatusForm]     = useState<UpdateBedStatusRequest>({ status: '', notes: '' });
  const [statusSaving, setStatusSaving] = useState(false);
  const [statusError, setStatusError]   = useState('');

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError('');
    try {
      const [w, b] = await Promise.all([getWard(id), getBeds(id)]);
      setWard(w);
      setBeds(b);
    } catch {
      setError('Failed to load ward.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  const handleAddBed = async () => {
    if (!addForm.bedNumber.trim()) { setAddError('Bed number is required.'); return; }
    setAddSaving(true); setAddError('');
    try {
      const bed = await addBed(id!, addForm);
      setBeds(prev => [...prev, bed]);
      setWard(prev => prev ? { ...prev, totalBeds: prev.totalBeds + 1, availableBeds: prev.availableBeds + 1 } : prev);
      setShowAdd(false);
      setAddForm({ bedNumber: '', notes: '' });
    } catch (e: any) {
      setAddError(e?.response?.data?.message ?? 'Failed to add bed.');
    } finally {
      setAddSaving(false);
    }
  };

  const openEditBed = (bed: BedResponse) => {
    setEditBed(bed);
    setEditForm({ bedNumber: bed.bedNumber, notes: bed.notes ?? '' });
    setEditError('');
  };

  const handleEditBed = async () => {
    if (!editForm.bedNumber.trim()) { setEditError('Bed number is required.'); return; }
    setEditSaving(true); setEditError('');
    try {
      const updated = await updateBed(editBed!.bedId, editForm);
      setBeds(prev => prev.map(b => b.bedId === updated.bedId ? updated : b));
      setEditBed(null);
    } catch (e: any) {
      setEditError(e?.response?.data?.message ?? 'Failed to update bed.');
    } finally {
      setEditSaving(false);
    }
  };

  const openStatusModal = (bed: BedResponse) => {
    setStatusBed(bed);
    setStatusForm({ status: bed.status, notes: bed.notes ?? '' });
    setStatusError('');
  };

  const handleStatusUpdate = async () => {
    setStatusSaving(true); setStatusError('');
    try {
      const updated = await updateBedStatus(statusBed!.bedId, statusForm);
      setBeds(prev => prev.map(b => b.bedId === updated.bedId ? updated : b));
      // Recompute ward stats
      setWard(prev => {
        if (!prev) return prev;
        const newBeds = beds.map(b => b.bedId === updated.bedId ? updated : b);
        return {
          ...prev,
          availableBeds:   newBeds.filter(b => b.status === 'Available').length,
          occupiedBeds:    newBeds.filter(b => b.status === 'Occupied').length,
          maintenanceBeds: newBeds.filter(b => b.status === 'Maintenance').length,
        };
      });
      setStatusBed(null);
    } catch (e: any) {
      setStatusError(e?.response?.data?.message ?? 'Failed to update status.');
    } finally {
      setStatusSaving(false);
    }
  };

  const handleDeleteBed = async (bed: BedResponse) => {
    if (!confirm(`Delete bed "${bed.bedNumber}"? This cannot be undone.`)) return;
    try {
      await deleteBed(bed.bedId);
      setBeds(prev => prev.filter(b => b.bedId !== bed.bedId));
      setWard(prev => prev ? {
        ...prev,
        totalBeds:     prev.totalBeds - 1,
        availableBeds: bed.status === 'Available' ? prev.availableBeds - 1 : prev.availableBeds,
        occupiedBeds:  bed.status === 'Occupied'  ? prev.occupiedBeds  - 1 : prev.occupiedBeds,
        maintenanceBeds: bed.status === 'Maintenance' ? prev.maintenanceBeds - 1 : prev.maintenanceBeds,
      } : prev);
    } catch (e: any) {
      setError(e?.response?.data?.message ?? 'Failed to delete bed.');
    }
  };

  if (loading) return <div className="p-6 text-center text-gray-400">Loading…</div>;
  if (error)   return <div className="p-6 text-center text-red-500">{error}</div>;
  if (!ward)   return <div className="p-6 text-center text-gray-400">Ward not found.</div>;

  return (
    <div className="p-6 max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex items-center gap-3 mb-2">
        <Link to="/inpatient/wards" className="text-sm text-gray-500 hover:text-gray-700">← Wards</Link>
      </div>
      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{ward.name}</h1>
          <div className="flex items-center gap-3 mt-1">
            <span className="text-sm text-gray-500">{ward.wardType}</span>
            {ward.description && <span className="text-sm text-gray-400">· {ward.description}</span>}
            {ward.dailyRate > 0 && <span className="text-sm text-gray-500">· GHS {ward.dailyRate.toFixed(2)}/day</span>}
          </div>
        </div>
        {isAdmin && (
          <button onClick={() => { setShowAdd(true); setAddForm({ bedNumber: '', notes: '' }); setAddError(''); }}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium">
            + Add Bed
          </button>
        )}
      </div>

      {/* Bed stats */}
      <div className="grid grid-cols-4 gap-3 mb-6">
        {[
          { label: 'Total', value: ward.totalBeds, color: 'text-gray-900' },
          { label: 'Available', value: ward.availableBeds, color: 'text-green-600' },
          { label: 'Occupied', value: ward.occupiedBeds, color: 'text-red-500' },
          { label: 'Maintenance', value: ward.maintenanceBeds, color: 'text-yellow-500' },
        ].map(s => (
          <div key={s.label} className="bg-white border border-gray-200 rounded-xl p-3 text-center">
            <div className={`text-2xl font-bold ${s.color}`}>{s.value}</div>
            <div className="text-xs text-gray-400 mt-0.5">{s.label}</div>
          </div>
        ))}
      </div>

      {/* Beds grid */}
      {beds.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          <p className="text-4xl mb-3">🛏</p>
          <p className="text-lg font-medium">No beds in this ward</p>
          {isAdmin && <p className="text-sm mt-1">Click "+ Add Bed" to add the first bed</p>}
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-3">
          {beds.map(bed => (
            <div key={bed.bedId} className="bg-white border border-gray-200 rounded-xl p-4 flex flex-col gap-2">
              <div className="flex items-start justify-between">
                <span className="font-bold text-gray-900 text-lg">{bed.bedNumber}</span>
                <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${BED_STATUS_COLORS[bed.status] ?? 'bg-gray-100 text-gray-600'}`}>
                  {bed.status}
                </span>
              </div>
              {bed.notes && <p className="text-xs text-gray-400 truncate">{bed.notes}</p>}
              <div className="flex gap-1 mt-auto pt-1 border-t border-gray-100">
                {canUpdateStatus && (
                  <button onClick={() => openStatusModal(bed)}
                    className="flex-1 text-xs py-1 text-blue-600 hover:bg-blue-50 rounded font-medium">
                    Status
                  </button>
                )}
                {isAdmin && (
                  <>
                    <button onClick={() => openEditBed(bed)}
                      className="flex-1 text-xs py-1 text-gray-500 hover:bg-gray-50 rounded">
                      Edit
                    </button>
                    <button onClick={() => handleDeleteBed(bed)}
                      className="text-xs py-1 px-2 text-red-400 hover:bg-red-50 rounded"
                      disabled={bed.status === 'Occupied'} title={bed.status === 'Occupied' ? 'Cannot delete occupied bed' : ''}>
                      ×
                    </button>
                  </>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Add Bed modal */}
      {showAdd && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Add Bed</h2>
              <button onClick={() => setShowAdd(false)} className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="px-6 py-4 space-y-4">
              {addError && <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-3 py-2 text-sm">{addError}</div>}
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Bed Number *</label>
                <input className={inp} placeholder="e.g. A1, B3, ICU-02" value={addForm.bedNumber}
                  onChange={e => setAddForm(f => ({ ...f, bedNumber: e.target.value }))} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Notes</label>
                <input className={inp} placeholder="Optional notes" value={addForm.notes}
                  onChange={e => setAddForm(f => ({ ...f, notes: e.target.value }))} />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setShowAdd(false)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleAddBed} disabled={addSaving} className="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm font-medium">
                {addSaving ? 'Adding…' : 'Add Bed'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Edit Bed modal */}
      {editBed && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Edit Bed {editBed.bedNumber}</h2>
              <button onClick={() => setEditBed(null)} className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="px-6 py-4 space-y-4">
              {editError && <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-3 py-2 text-sm">{editError}</div>}
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Bed Number *</label>
                <input className={inp} value={editForm.bedNumber} onChange={e => setEditForm(f => ({ ...f, bedNumber: e.target.value }))} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Notes</label>
                <input className={inp} value={editForm.notes} onChange={e => setEditForm(f => ({ ...f, notes: e.target.value }))} />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setEditBed(null)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleEditBed} disabled={editSaving} className="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm font-medium">
                {editSaving ? 'Saving…' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Status modal */}
      {statusBed && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Update Bed {statusBed.bedNumber}</h2>
              <button onClick={() => setStatusBed(null)} className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="px-6 py-4 space-y-4">
              {statusError && <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-3 py-2 text-sm">{statusError}</div>}
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-2">Status</label>
                <div className="grid grid-cols-3 gap-2">
                  {BED_STATUS_OPTIONS.map(s => (
                    <button key={s}
                      onClick={() => setStatusForm(f => ({ ...f, status: s }))}
                      className={`py-2 rounded-lg text-sm font-medium border-2 transition-colors ${
                        statusForm.status === s
                          ? 'border-blue-500 bg-blue-50 text-blue-700'
                          : 'border-gray-200 text-gray-600 hover:border-gray-300'
                      }`}>
                      {s}
                    </button>
                  ))}
                </div>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Notes</label>
                <input className={inp} placeholder="Reason for status change" value={statusForm.notes}
                  onChange={e => setStatusForm(f => ({ ...f, notes: e.target.value }))} />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setStatusBed(null)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleStatusUpdate} disabled={statusSaving} className="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm font-medium">
                {statusSaving ? 'Updating…' : 'Update'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
