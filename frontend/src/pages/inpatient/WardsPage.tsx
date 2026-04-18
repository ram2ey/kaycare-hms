import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { getWards, createWard, updateWard, deactivateWard } from '../../api/inpatient';
import type { WardResponse, SaveWardRequest } from '../../types/inpatient';
import { WARD_TYPES } from '../../types/inpatient';
import { useAuth } from '../../contexts/AuthContext';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

const emptyForm = (): SaveWardRequest => ({
  name: '', wardType: 'General', description: '', dailyRate: 0, isActive: true,
});

const WARD_TYPE_COLORS: Record<string, string> = {
  General:    'bg-blue-100 text-blue-700',
  ICU:        'bg-red-100 text-red-700',
  HDU:        'bg-orange-100 text-orange-700',
  Maternity:  'bg-pink-100 text-pink-700',
  Pediatric:  'bg-purple-100 text-purple-700',
  Private:    'bg-teal-100 text-teal-700',
  Emergency:  'bg-red-100 text-red-800',
  Surgical:   'bg-indigo-100 text-indigo-700',
  Psychiatric:'bg-yellow-100 text-yellow-700',
};

export default function WardsPage() {
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin' || user?.role === 'SuperAdmin';

  const [wards, setWards]         = useState<WardResponse[]>([]);
  const [loading, setLoading]     = useState(true);
  const [error, setError]         = useState('');
  const [showInactive, setShowInactive] = useState(false);

  const [showForm, setShowForm]   = useState(false);
  const [editTarget, setEditTarget] = useState<WardResponse | null>(null);
  const [form, setForm]           = useState<SaveWardRequest>(emptyForm());
  const [saving, setSaving]       = useState(false);
  const [formError, setFormError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setWards(await getWards({ activeOnly: showInactive ? undefined : true }));
    } catch {
      setError('Failed to load wards.');
    } finally {
      setLoading(false);
    }
  }, [showInactive]);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => { setEditTarget(null); setForm(emptyForm()); setFormError(''); setShowForm(true); };
  const openEdit   = (w: WardResponse) => {
    setEditTarget(w);
    setForm({ name: w.name, wardType: w.wardType, description: w.description ?? '', dailyRate: w.dailyRate, isActive: w.isActive });
    setFormError('');
    setShowForm(true);
  };

  const handleSave = async () => {
    if (!form.name.trim()) { setFormError('Ward name is required.'); return; }
    setSaving(true);
    setFormError('');
    try {
      if (editTarget) {
        const updated = await updateWard(editTarget.wardId, form);
        setWards(prev => prev.map(w => w.wardId === updated.wardId ? updated : w));
      } else {
        const created = await createWard(form);
        setWards(prev => [...prev, created]);
      }
      setShowForm(false);
    } catch (e: any) {
      setFormError(e?.response?.data?.message ?? 'Failed to save ward.');
    } finally {
      setSaving(false);
    }
  };

  const handleDeactivate = async (w: WardResponse) => {
    if (!confirm(`Deactivate ward "${w.name}"?`)) return;
    try {
      const updated = await deactivateWard(w.wardId);
      setWards(prev => prev.map(x => x.wardId === updated.wardId ? updated : x));
    } catch {
      setError('Failed to deactivate ward.');
    }
  };

  const totalBeds     = wards.reduce((s, w) => s + w.totalBeds, 0);
  const totalAvail    = wards.reduce((s, w) => s + w.availableBeds, 0);
  const totalOccupied = wards.reduce((s, w) => s + w.occupiedBeds, 0);

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Wards & Beds</h1>
          <p className="text-sm text-gray-500 mt-1">Manage hospital wards and bed availability</p>
        </div>
        {isAdmin && (
          <button onClick={openCreate} className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium">
            + Add Ward
          </button>
        )}
      </div>

      {/* Summary strip */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        {[
          { label: 'Total Beds', value: totalBeds, color: 'text-gray-900' },
          { label: 'Available', value: totalAvail, color: 'text-green-600' },
          { label: 'Occupied', value: totalOccupied, color: 'text-red-600' },
        ].map(s => (
          <div key={s.label} className="bg-white border border-gray-200 rounded-xl p-4 text-center">
            <div className={`text-3xl font-bold ${s.color}`}>{s.value}</div>
            <div className="text-sm text-gray-500 mt-1">{s.label}</div>
          </div>
        ))}
      </div>

      {/* Filters */}
      {isAdmin && (
        <div className="flex items-center gap-3 mb-4">
          <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
            <input type="checkbox" checked={showInactive} onChange={e => setShowInactive(e.target.checked)} className="rounded" />
            Show inactive wards
          </label>
        </div>
      )}

      {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{error}</div>}

      {loading ? (
        <div className="text-center py-12 text-gray-400">Loading…</div>
      ) : wards.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          <p className="text-4xl mb-3">🏥</p>
          <p className="text-lg font-medium">No wards found</p>
          {isAdmin && <p className="text-sm mt-1">Click "Add Ward" to create one</p>}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {wards.map(ward => (
            <div key={ward.wardId} className={`bg-white border rounded-xl overflow-hidden ${!ward.isActive ? 'opacity-60' : ''}`}>
              <div className="px-5 py-4 border-b border-gray-100">
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${WARD_TYPE_COLORS[ward.wardType] ?? 'bg-gray-100 text-gray-600'}`}>
                        {ward.wardType}
                      </span>
                      {!ward.isActive && <span className="text-xs bg-gray-100 text-gray-500 px-2 py-0.5 rounded-full">Inactive</span>}
                    </div>
                    <h3 className="font-semibold text-gray-900">{ward.name}</h3>
                    {ward.description && <p className="text-xs text-gray-500 mt-0.5">{ward.description}</p>}
                  </div>
                  {isAdmin && (
                    <div className="flex gap-1 ml-2">
                      <button onClick={() => openEdit(ward)} className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded" title="Edit">✏️</button>
                      {ward.isActive && (
                        <button onClick={() => handleDeactivate(ward)} className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded" title="Deactivate">🗑</button>
                      )}
                    </div>
                  )}
                </div>
              </div>

              {/* Bed stats */}
              <div className="px-5 py-3 grid grid-cols-3 gap-2 text-center border-b border-gray-100">
                <div>
                  <div className="text-lg font-bold text-green-600">{ward.availableBeds}</div>
                  <div className="text-xs text-gray-400">Available</div>
                </div>
                <div>
                  <div className="text-lg font-bold text-red-500">{ward.occupiedBeds}</div>
                  <div className="text-xs text-gray-400">Occupied</div>
                </div>
                <div>
                  <div className="text-lg font-bold text-yellow-500">{ward.maintenanceBeds}</div>
                  <div className="text-xs text-gray-400">Maintenance</div>
                </div>
              </div>

              <div className="px-5 py-3 flex items-center justify-between">
                <span className="text-xs text-gray-500">{ward.totalBeds} bed{ward.totalBeds !== 1 ? 's' : ''} total</span>
                <Link
                  to={`/inpatient/wards/${ward.wardId}`}
                  className="text-sm text-blue-600 hover:underline font-medium"
                >
                  Manage Beds →
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create / Edit modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">{editTarget ? 'Edit Ward' : 'Add Ward'}</h2>
              <button onClick={() => setShowForm(false)} className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="px-6 py-4 space-y-4">
              {formError && <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-3 py-2 text-sm">{formError}</div>}
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Ward Name *</label>
                <input className={inp} value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Ward Type *</label>
                <select className={inp} value={form.wardType} onChange={e => setForm(f => ({ ...f, wardType: e.target.value }))}>
                  {WARD_TYPES.map(t => <option key={t}>{t}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Description</label>
                <input className={inp} value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Daily Rate (GHS)</label>
                <input type="number" min="0" step="0.01" className={inp} value={form.dailyRate}
                  onChange={e => setForm(f => ({ ...f, dailyRate: parseFloat(e.target.value) || 0 }))} />
              </div>
              {editTarget && (
                <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                  <input type="checkbox" checked={form.isActive} onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))} className="rounded" />
                  Active
                </label>
              )}
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setShowForm(false)} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800">Cancel</button>
              <button onClick={handleSave} disabled={saving} className="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm font-medium">
                {saving ? 'Saving…' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
