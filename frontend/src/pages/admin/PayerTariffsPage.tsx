import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { getPayers } from '../../api/payers';
import { getCatalog } from '../../api/serviceCatalog';
import { getPayerTariffs, upsertPayerTariff, deletePayerTariff } from '../../api/payerTariffs';
import type { PayerResponse, PayerTariffResponse, SavePayerTariffRequest } from '../../types/payers';
import type { ServiceCatalogItem } from '../../types/serviceCatalog';
import { ConfirmDialog } from '../../components/ConfirmDialog';

function fmt(n: number) {
  return `GHS ${n.toFixed(2)}`;
}

export default function PayerTariffsPage() {
  const [searchParams] = useSearchParams();
  const initialServiceId = searchParams.get('serviceId');

  const [payers, setPayers] = useState<PayerResponse[]>([]);
  const [selectedPayerId, setSelectedPayerId] = useState<string>('');
  const [services, setServices] = useState<ServiceCatalogItem[]>([]);
  const [tariffs, setTariffs] = useState<PayerTariffResponse[]>([]);
  
  const [loading, setLoading] = useState(true);
  const [loadingTariffs, setLoadingTariffs] = useState(false);
  const [error, setError] = useState('');
  const [actionError, setActionError] = useState('');
  const [confirmAction, setConfirmAction] = useState<null | { message: string; run: () => void }>(null);
  const [deleting, setDeleting] = useState(false);

  // Filtering / Search
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('All');
  const [selectedTariffStatus, setSelectedTariffStatus] = useState('All'); // All, Configured, Missing

  // Modal states
  const [showModal, setShowModal] = useState(false);
  const [selectedService, setSelectedService] = useState<ServiceCatalogItem | null>(null);
  const [editingTariff, setEditingTariff] = useState<PayerTariffResponse | null>(null);
  const [saving, setSaving] = useState(false);

  // Form states
  const [tariffPrice, setTariffPrice] = useState(0);
  const [tariffCode, setTariffCode] = useState('');
  const [effectiveDate, setEffectiveDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [isActive, setIsActive] = useState(true);

  // Load payers & catalog once
  useEffect(() => {
    async function init() {
      setLoading(true);
      setError('');
      try {
        const [payersList, catalogList] = await Promise.all([
          getPayers(true), // active payers
          getCatalog(true), // active services
        ]);
        setPayers(payersList);
        setServices(catalogList);

        if (payersList.length > 0) {
          setSelectedPayerId(payersList[0].payerId);
        }
      } catch {
        setError('Failed to initialize payers and service catalog.');
      } finally {
        setLoading(false);
      }
    }
    init();
  }, []);

  // Fetch tariffs when payer changes
  useEffect(() => {
    if (!selectedPayerId) return;

    async function loadTariffs() {
      setLoadingTariffs(true);
      setActionError('');
      try {
        const data = await getPayerTariffs({ payerId: selectedPayerId });
        setTariffs(data);
      } catch {
        setActionError('Failed to load tariffs for selected payer.');
      } finally {
        setLoadingTariffs(false);
      }
    }
    loadTariffs();
  }, [selectedPayerId]);

  // Open modal from serviceId searchParam if present
  useEffect(() => {
    if (initialServiceId && services.length > 0 && tariffs.length >= 0) {
      const svc = services.find(s => s.serviceCatalogItemId === initialServiceId);
      if (svc) {
        const tariff = tariffs.find(t => t.serviceCatalogItemId === svc.serviceCatalogItemId);
        openUpsertModal(svc, tariff || null);
      }
    }
  }, [initialServiceId, services, tariffs]);

  function openUpsertModal(svc: ServiceCatalogItem, existing: PayerTariffResponse | null) {
    setSelectedService(svc);
    setEditingTariff(existing);
    if (existing) {
      setTariffPrice(existing.tariffPrice);
      setTariffCode(existing.tariffCode || '');
      setEffectiveDate(existing.effectiveDate);
      setIsActive(existing.isActive);
    } else {
      setTariffPrice(svc.unitPrice);
      setTariffCode('');
      setEffectiveDate(new Date().toISOString().slice(0, 10));
      setIsActive(true);
    }
    setShowModal(true);
  }

  async function handleDeleteTariff(t: PayerTariffResponse) {
    setActionError('');
    setDeleting(true);
    try {
      await deletePayerTariff(t.payerTariffId);
      setTariffs(tariffs.filter(x => x.payerTariffId !== t.payerTariffId));
    } catch {
      setActionError('Failed to delete custom tariff.');
    } finally {
      setDeleting(false);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedPayerId || !selectedService) return;

    setSaving(true);
    setActionError('');
    const data: SavePayerTariffRequest = {
      payerId: selectedPayerId,
      serviceCatalogItemId: selectedService.serviceCatalogItemId,
      tariffPrice: Number(tariffPrice),
      tariffCode: tariffCode.trim() || null,
      effectiveDate,
      isActive,
    };

    try {
      const result = await upsertPayerTariff(data);
      // Update tariffs list
      if (editingTariff) {
        setTariffs(tariffs.map(t => t.payerTariffId === result.payerTariffId ? result : t));
      } else {
        setTariffs([result, ...tariffs]);
      }
      setShowModal(false);
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Save failed.';
      setActionError(msg);
    } finally {
      setSaving(false);
    }
  }

  // Categories present in catalog
  const categories = Array.from(new Set(services.map(s => s.category))).sort();

  // Combine services and tariffs for visual representation
  const items = services.map(svc => {
    const tariff = tariffs.find(t => t.serviceCatalogItemId === svc.serviceCatalogItemId);
    return {
      service: svc,
      tariff: tariff || null,
    };
  });

  const filteredItems = items.filter(item => {
    const matchesSearch = item.service.name.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesCategory = selectedCategory === 'All' || item.service.category === selectedCategory;
    const matchesTariffStatus =
      selectedTariffStatus === 'All' ||
      (selectedTariffStatus === 'Configured' && item.tariff !== null) ||
      (selectedTariffStatus === 'Missing' && item.tariff === null);

    return matchesSearch && matchesCategory && matchesTariffStatus;
  });

  if (loading) {
    return (
      <div className="p-8 text-center text-gray-500 max-w-7xl mx-auto">
        Loading payers and service catalog...
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center text-red-650 max-w-7xl mx-auto">
        {error}
      </div>
    );
  }

  const selectedPayerName = payers.find(p => p.payerId === selectedPayerId)?.name || 'Payer';

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-800">Payer Tariff Schedule</h2>
          <p className="text-sm text-gray-500 mt-1">
            Configure custom insurance payer contracts, copay details, tariff rates, and pricing overrides.
          </p>
        </div>

        {/* Payer Selector */}
        <div className="flex items-center gap-3">
          <label htmlFor="payer-tariff-payer-select" className="text-sm font-bold text-gray-600">Select Payer:</label>
          <select
            id="payer-tariff-payer-select"
            value={selectedPayerId}
            onChange={e => setSelectedPayerId(e.target.value)}
            className="px-3.5 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white font-semibold text-gray-800 shadow-sm"
          >
            {payers.map(p => (
              <option key={p.payerId} value={p.payerId}>
                {p.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Filter panel */}
      <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm flex flex-wrap gap-3 items-center mb-6">
        <div className="flex-1 min-w-[240px]">
          <input
            type="text"
            placeholder="Search service catalog by name..."
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
            {categories.map(cat => (
              <option key={cat} value={cat}>{cat}</option>
            ))}
          </select>
        </div>
        <div className="w-44">
          <select
            value={selectedTariffStatus}
            onChange={e => setSelectedTariffStatus(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
          >
            <option value="All">All Tariff Configurations</option>
            <option value="Configured">Configured Overrides</option>
            <option value="Missing">Not Overridden (Uses Standard)</option>
          </select>
        </div>
      </div>

      {actionError && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">{actionError}</div>
      )}

      {loadingTariffs ? (
        <div className="text-gray-400 py-12 text-center bg-white rounded-xl border border-gray-200">
          Loading tariffs for {selectedPayerName}...
        </div>
      ) : filteredItems.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-500">
          No catalog items match your search.
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100 bg-gray-50 text-gray-500 text-xs font-semibold uppercase tracking-wider">
                <tr>
                  <th className="text-left px-6 py-3.5">Service Item</th>
                  <th className="text-left px-6 py-3.5">Category</th>
                  <th className="text-right px-6 py-3.5">Standard Price</th>
                  <th className="text-right px-6 py-3.5">{selectedPayerName} Price</th>
                  <th className="text-left px-6 py-3.5">Tariff Code</th>
                  <th className="text-left px-6 py-3.5">Effective Date</th>
                  <th className="text-left px-6 py-3.5">Status</th>
                  <th className="px-6 py-3.5"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 text-gray-700">
                {filteredItems.map(({ service, tariff }) => (
                  <tr key={service.serviceCatalogItemId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="px-6 py-4 font-bold text-gray-950">{service.name}</td>
                    <td className="px-6 py-4">
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-gray-100 text-gray-800">
                        {service.category}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right text-gray-500 font-mono">{fmt(service.unitPrice)}</td>
                    <td className="px-6 py-4 text-right">
                      {tariff ? (
                        <div className="font-bold text-blue-750 font-mono">{fmt(tariff.tariffPrice)}</div>
                      ) : (
                        <div className="text-gray-400 text-xs">
                          {fmt(service.unitPrice)} <span className="text-[10px] text-gray-300 font-sans block">(Standard)</span>
                        </div>
                      )}
                    </td>
                    <td className="px-6 py-4 font-semibold text-gray-700">
                      {tariff?.tariffCode ? (
                        <span className="font-mono text-xs">{tariff.tariffCode}</span>
                      ) : (
                        <span className="text-gray-350 text-xs">—</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-gray-500 text-xs">
                      {tariff ? tariff.effectiveDate : '—'}
                    </td>
                    <td className="px-6 py-4">
                      {tariff ? (
                        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold ${tariff.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                          {tariff.isActive ? 'Active override' : 'Inactive'}
                        </span>
                      ) : (
                        <span className="text-gray-450 text-xs italic">Inherited</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-right space-x-3">
                      <button
                        onClick={() => openUpsertModal(service, tariff)}
                        className="text-xs text-blue-600 hover:text-blue-800 font-semibold hover:underline"
                      >
                        {tariff ? 'Edit tariff' : 'Set tariff'}
                      </button>
                      {tariff && (
                        <button
                          onClick={() => setConfirmAction({
                            message: `Are you sure you want to remove the custom tariff override for "${tariff.serviceName}"?`,
                            run: () => handleDeleteTariff(tariff),
                          })}
                          className="text-xs text-red-600 hover:text-red-800 font-semibold hover:underline"
                        >
                          Reset
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Set / Edit Tariff Modal */}
      {showModal && selectedService && (
        <div className="fixed inset-0 bg-black/45 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md flex flex-col">
            <div className="px-6 py-4 border-b border-gray-150 flex items-center justify-between">
              <h3 className="text-lg font-bold text-gray-900">
                {editingTariff ? 'Edit Tariff Override' : 'Set Tariff Override'}
              </h3>
              <button onClick={() => setShowModal(false)} aria-label="Close" className="text-gray-400 hover:text-gray-600">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div className="bg-gray-50 p-3 rounded-lg border border-gray-100 text-sm space-y-1">
                <div>
                  <span className="text-xs text-gray-500">Service:</span>{' '}
                  <span className="font-bold text-gray-800">{selectedService.name}</span>
                </div>
                <div>
                  <span className="text-xs text-gray-500">Payer:</span>{' '}
                  <span className="font-semibold text-gray-700">{selectedPayerName}</span>
                </div>
                <div>
                  <span className="text-xs text-gray-500">Standard Pricing:</span>{' '}
                  <span className="font-mono text-gray-850 font-semibold">{fmt(selectedService.unitPrice)}</span>
                </div>
              </div>

              <div>
                <label htmlFor="payer-tariff-price" className="block text-xs font-semibold text-gray-600 mb-1">
                  Custom Tariff Price ({selectedPayerName}) (GHS) *
                </label>
                <input
                  id="payer-tariff-price"
                  type="number"
                  min={0}
                  step="0.01"
                  required
                  value={tariffPrice}
                  onChange={e => setTariffPrice(Number(e.target.value))}
                  placeholder="0.00"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 font-bold"
                />
              </div>

              <div>
                <label htmlFor="payer-tariff-code" className="block text-xs font-semibold text-gray-600 mb-1">
                  Payer Tariff Code (e.g. NHIS test code)
                </label>
                <input
                  id="payer-tariff-code"
                  maxLength={50}
                  value={tariffCode}
                  onChange={e => setTariffCode(e.target.value)}
                  placeholder="e.g. GHA-LAB-001 (Optional)"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 uppercase font-mono"
                />
              </div>

              <div>
                <label htmlFor="payer-tariff-effective-date" className="block text-xs font-semibold text-gray-600 mb-1">Effective Date *</label>
                <input
                  id="payer-tariff-effective-date"
                  type="date"
                  required
                  value={effectiveDate}
                  onChange={e => setEffectiveDate(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="flex items-center gap-2 pt-2">
                <input
                  type="checkbox"
                  id="tariffIsActiveForm"
                  checked={isActive}
                  onChange={e => setIsActive(e.target.checked)}
                  className="rounded text-blue-600 focus:ring-blue-500 h-4 w-4 border-gray-300"
                />
                <label htmlFor="tariffIsActiveForm" className="text-sm text-gray-700 cursor-pointer select-none">
                  Tariff override is active
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
                  {editingTariff ? 'Save Override' : 'Apply Override'}
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
        busy={deleting}
        onConfirm={() => { confirmAction?.run(); setConfirmAction(null); }}
        onCancel={() => setConfirmAction(null)}
      />
    </div>
  );
}
