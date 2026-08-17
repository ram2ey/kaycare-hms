import { useState, useEffect } from 'react';
import { getDrugs, createDrug, updateDrug, deactivateDrug } from '../../api/pharmacy';
import type { DrugInventoryResponse, SaveDrugRequest } from '../../types/pharmacy';
import { DRUG_UNITS, DRUG_DOSAGE_FORMS, DRUG_CATEGORIES } from '../../types/pharmacy';

function fmt(n: number) {
  return `GHS ${n.toFixed(2)}`;
}

export default function DrugFormularyPage() {
  const [drugs, setDrugs] = useState<DrugInventoryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Filter states
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('All');
  const [selectedStatus, setSelectedStatus] = useState('All');
  const [selectedCS, setSelectedCS] = useState('All'); // Controlled Substance

  // Modal states
  const [showModal, setShowModal] = useState(false);
  const [editingDrug, setEditingDrug] = useState<DrugInventoryResponse | null>(null);
  const [saving, setSaving] = useState(false);
  const [actionError, setActionError] = useState('');

  // Form states
  const [name, setName] = useState('');
  const [genericName, setGenericName] = useState('');
  const [dosageForm, setDosageForm] = useState('Tablet');
  const [strength, setStrength] = useState('');
  const [unit, setUnit] = useState('Tablet');
  const [category, setCategory] = useState('Analgesics');
  const [reorderThreshold, setReorderThreshold] = useState(10);
  const [unitCost, setUnitCost] = useState(0);
  const [sellingPrice, setSellingPrice] = useState(0);
  const [isControlledSubstance, setIsControlledSubstance] = useState(false);
  const [isActive, setIsActive] = useState(true);

  async function load() {
    setLoading(true);
    setError('');
    try {
      // Admins should see all drugs (active and inactive)
      const data = await getDrugs({ activeOnly: false });
      setDrugs(data);
    } catch {
      setError('Failed to load drug formulary.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  function openCreate() {
    setEditingDrug(null);
    setName('');
    setGenericName('');
    setDosageForm('Tablet');
    setStrength('');
    setUnit('Tablet');
    setCategory('Analgesics');
    setReorderThreshold(10);
    setUnitCost(0);
    setSellingPrice(0);
    setIsControlledSubstance(false);
    setIsActive(true);
    setShowModal(true);
  }

  function openEdit(d: DrugInventoryResponse) {
    setEditingDrug(d);
    setName(d.name);
    setGenericName(d.genericName || '');
    setDosageForm(d.dosageForm || 'Tablet');
    setStrength(d.strength || '');
    setUnit(d.unit);
    setCategory(d.category || 'Analgesics');
    setReorderThreshold(d.reorderThreshold);
    setUnitCost(d.unitCost);
    setSellingPrice(d.sellingPrice);
    setIsControlledSubstance(d.isControlledSubstance);
    setIsActive(d.isActive);
    setShowModal(true);
  }

  async function handleToggleActive(d: DrugInventoryResponse) {
    setActionError('');
    try {
      if (d.isActive) {
        const updated = await deactivateDrug(d.drugInventoryId);
        setDrugs(drugs.map(x => x.drugInventoryId === d.drugInventoryId ? updated : x));
      } else {
        const data: SaveDrugRequest = {
          name: d.name,
          genericName: d.genericName || undefined,
          dosageForm: d.dosageForm || undefined,
          strength: d.strength || undefined,
          unit: d.unit,
          category: d.category || undefined,
          reorderThreshold: d.reorderThreshold,
          unitCost: d.unitCost,
          sellingPrice: d.sellingPrice,
          isControlledSubstance: d.isControlledSubstance,
          isActive: true
        };
        const updated = await updateDrug(d.drugInventoryId, data);
        setDrugs(drugs.map(x => x.drugInventoryId === d.drugInventoryId ? updated : x));
      }
    } catch {
      setActionError('Failed to update active status.');
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim() || !unit.trim()) return;

    setSaving(true);
    setActionError('');
    const data: SaveDrugRequest = {
      name: name.trim(),
      genericName: genericName.trim() || undefined,
      dosageForm: dosageForm.trim() || undefined,
      strength: strength.trim() || undefined,
      unit: unit.trim(),
      category: category.trim() || undefined,
      reorderThreshold: Number(reorderThreshold),
      unitCost: Number(unitCost),
      sellingPrice: Number(sellingPrice),
      isControlledSubstance,
      isActive,
    };

    try {
      if (editingDrug) {
        const updated = await updateDrug(editingDrug.drugInventoryId, data);
        setDrugs(drugs.map(d => d.drugInventoryId === editingDrug.drugInventoryId ? updated : d));
      } else {
        const created = await createDrug(data);
        setDrugs([created, ...drugs]);
      }
      setShowModal(false);
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Save failed.';
      setActionError(msg);
    } finally {
      setSaving(false);
    }
  }

  const filtered = drugs.filter(d => {
    const matchesSearch =
      d.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (d.genericName && d.genericName.toLowerCase().includes(searchQuery.toLowerCase()));
    const matchesCategory = selectedCategory === 'All' || d.category === selectedCategory;
    const matchesStatus =
      selectedStatus === 'All' ||
      (selectedStatus === 'Active' && d.isActive) ||
      (selectedStatus === 'Inactive' && !d.isActive);
    const matchesCS =
      selectedCS === 'All' ||
      (selectedCS === 'Controlled' && d.isControlledSubstance) ||
      (selectedCS === 'Standard' && !d.isControlledSubstance);

    return matchesSearch && matchesCategory && matchesStatus && matchesCS;
  });

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-800">Drug Formulary</h2>
          <p className="text-sm text-gray-500 mt-1">
            Configure drug catalog catalog details, pricing templates, categories, and controlled status overrides.
          </p>
        </div>
        <button
          onClick={openCreate}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white rounded-lg text-sm font-semibold transition-colors flex items-center gap-2 shadow-sm self-start md:self-auto"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clipRule="evenodd" />
          </svg>
          Add Drug to Formulary
        </button>
      </div>

      {actionError && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">{actionError}</div>
      )}

      {/* Filter panel */}
      <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm flex flex-wrap gap-3 items-center mb-6">
        <div className="flex-1 min-w-[240px]">
          <input
            type="text"
            placeholder="Search by drug name or generic name..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div className="w-48">
          <select
            value={selectedCategory}
            onChange={e => setSelectedCategory(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
          >
            <option value="All">All Categories</option>
            {DRUG_CATEGORIES.map(c => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>
        <div className="w-40">
          <select
            value={selectedCS}
            onChange={e => setSelectedCS(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
          >
            <option value="All">All Substance Types</option>
            <option value="Standard">Standard Meds</option>
            <option value="Controlled">Controlled Substances</option>
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
        <div className="text-gray-400 py-8 text-center bg-white rounded-xl border border-gray-200">Loading drug formulary...</div>
      ) : error ? (
        <div className="text-red-600 py-8 text-center bg-white rounded-xl border border-gray-200">{error}</div>
      ) : filtered.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-500">
          No drugs match your criteria.
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100 bg-gray-50 text-gray-500 text-xs font-semibold uppercase tracking-wider">
                <tr>
                  <th className="text-left px-6 py-3.5">Name / Generic</th>
                  <th className="text-left px-6 py-3.5">Category</th>
                  <th className="text-left px-6 py-3.5">Form / Strength</th>
                  <th className="text-left px-6 py-3.5">Unit</th>
                  <th className="text-right px-6 py-3.5">Unit Cost</th>
                  <th className="text-right px-6 py-3.5">Selling Price</th>
                  <th className="text-center px-6 py-3.5">Controlled</th>
                  <th className="text-left px-6 py-3.5">Status</th>
                  <th className="px-6 py-3.5"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 text-gray-700">
                {filtered.map(d => (
                  <tr key={d.drugInventoryId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="px-6 py-4">
                      <div className="font-bold text-gray-950">{d.name}</div>
                      {d.genericName && <div className="text-xs text-gray-500 font-medium">{d.genericName}</div>}
                    </td>
                    <td className="px-6 py-4 text-gray-600">{d.category || '—'}</td>
                    <td className="px-6 py-4 text-gray-500">
                      {d.dosageForm || '—'} {d.strength && `· ${d.strength}`}
                    </td>
                    <td className="px-6 py-4 text-gray-500">{d.unit}</td>
                    <td className="px-6 py-4 text-right text-gray-900 font-medium">{fmt(d.unitCost)}</td>
                    <td className="px-6 py-4 text-right text-blue-750 font-bold">{fmt(d.sellingPrice)}</td>
                    <td className="px-6 py-4 text-center">
                      {d.isControlledSubstance ? (
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-red-50 text-red-700 border border-red-200">
                          Controlled
                        </span>
                      ) : (
                        <span className="text-gray-400 text-xs">—</span>
                      )}
                    </td>
                    <td className="px-6 py-4">
                      <button
                        onClick={() => handleToggleActive(d)}
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold cursor-pointer transition-colors ${
                          d.isActive
                            ? 'bg-green-150 text-green-700 hover:bg-green-200'
                            : 'bg-red-100 text-red-700 hover:bg-red-200'
                        }`}
                      >
                        {d.isActive ? 'Active' : 'Inactive'}
                      </button>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button
                        onClick={() => openEdit(d)}
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
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-xl max-h-[95vh] flex flex-col">
            <div className="px-6 py-4 border-b border-gray-150 flex items-center justify-between">
              <h3 className="text-lg font-bold text-gray-900">
                {editingDrug ? 'Edit Drug details' : 'Add New Drug to Formulary'}
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
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Brand/Trade Name *</label>
                  <input
                    required
                    maxLength={100}
                    value={name}
                    onChange={e => setName(e.target.value)}
                    placeholder="e.g. Amoxil, Panadol"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 font-medium"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Generic Name (Active Ingredient)</label>
                  <input
                    maxLength={100}
                    value={genericName}
                    onChange={e => setGenericName(e.target.value)}
                    placeholder="e.g. Amoxicillin, Paracetamol"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Dosage Form *</label>
                  <select
                    value={dosageForm}
                    onChange={e => setDosageForm(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                  >
                    {DRUG_DOSAGE_FORMS.map(f => (
                      <option key={f} value={f}>{f}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Strength (e.g. 500mg, 10ml)</label>
                  <input
                    maxLength={50}
                    value={strength}
                    onChange={e => setStrength(e.target.value)}
                    placeholder="e.g. 500mg, 125mg/5ml"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Dispensing Unit *</label>
                  <select
                    value={unit}
                    onChange={e => setUnit(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                  >
                    {DRUG_UNITS.map(u => (
                      <option key={u} value={u}>{u}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Therapeutic Category *</label>
                  <select
                    value={category}
                    onChange={e => setCategory(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                  >
                    {DRUG_CATEGORIES.map(c => (
                      <option key={c} value={c}>{c}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Reorder Stock Threshold *</label>
                  <input
                    type="number"
                    min={0}
                    required
                    value={reorderThreshold}
                    onChange={e => setReorderThreshold(Number(e.target.value))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Unit Cost (GHS) *</label>
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    required
                    value={unitCost}
                    onChange={e => setUnitCost(Number(e.target.value))}
                    placeholder="0.00"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Standard Selling Price (GHS) *</label>
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    required
                    value={sellingPrice}
                    onChange={e => setSellingPrice(Number(e.target.value))}
                    placeholder="0.00"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 font-semibold"
                  />
                </div>
              </div>

              <div className="space-y-2.5 pt-2 border-t border-gray-100">
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isControlledForm"
                    checked={isControlledSubstance}
                    onChange={e => setIsControlledSubstance(e.target.checked)}
                    className="rounded text-red-600 focus:ring-red-500 h-4 w-4 border-gray-300"
                  />
                  <label htmlFor="isControlledForm" className="text-sm font-semibold text-red-750 cursor-pointer select-none">
                    This is a Controlled Substance (requires special reporting and logging)
                  </label>
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isActiveForm"
                    checked={isActive}
                    onChange={e => setIsActive(e.target.checked)}
                    className="rounded text-blue-600 focus:ring-blue-500 h-4 w-4 border-gray-300"
                  />
                  <label htmlFor="isActiveForm" className="text-sm text-gray-700 cursor-pointer select-none">
                    Drug is active (available for prescribing and dispensing)
                  </label>
                </div>
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
                  {editingDrug ? 'Save Changes' : 'Add to Formulary'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
