import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getLabOrderById, receiveSample, enterManualResult, signItem, downloadLabReport, recordCriticalCallLog } from '../../api/labOrders';
import type { LabOrderDetail, LabOrderItemResponse, LabOrderItem } from '../../types/labOrders';
import { ITEM_STATUS_COLORS, ORDER_STATUS_COLORS } from '../../types/labOrders';
import { safeArray } from '../../utils/array';
import { printLabel } from '../../utils/printLabel';

function printBarcode(item: LabOrderItem, patientName: string, patientMrn: string) {
  printLabel({
    patientName,
    patientMrn,
    testName: item.testName,
    department: item.department,
    accessionNumber: item.accessionNumber,
  });
}

export default function LabOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [order, setOrder] = useState<LabOrderDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [manualItem, setManualItem] = useState<LabOrderItemResponse | null>(null);
  const [manualResult, setManualResult] = useState('');
  const [manualNotes, setManualNotes] = useState('');
  const [manualUnit, setManualUnit] = useState('');
  const [manualRefRange, setManualRefRange] = useState('');
  const [saving, setSaving] = useState(false);
  const [downloading, setDownloading] = useState(false);

  const [selectedCallLogItem, setSelectedCallLogItem] = useState<LabOrderItemResponse | null>(null);
  const [recipientName, setRecipientName] = useState('');
  const [callLogNotes, setCallLogNotes] = useState('Read-back confirmed');
  const [callLogSubmitting, setCallLogSubmitting] = useState(false);
  const [callLogError, setCallLogError] = useState<string | null>(null);

  const load = async () => {
    if (!id) return;
    const data = await getLabOrderById(id);
    setOrder(data);
    setLoading(false);
  };

  useEffect(() => { load(); }, [id]);

  const handleReceive = async (item: LabOrderItemResponse) => {
    await receiveSample(item.labOrderItemId);
    load();
  };

  const handleOpenCallLogModal = (item: LabOrderItemResponse) => {
    setSelectedCallLogItem(item);
    setRecipientName('');
    setCallLogNotes('Read-back confirmed');
    setCallLogError(null);
  };

  const handleCallLogSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCallLogItem) return;
    if (!recipientName.trim()) {
      setCallLogError('Recipient clinician name is required.');
      return;
    }
    setCallLogSubmitting(true);
    setCallLogError(null);
    try {
      await recordCriticalCallLog(selectedCallLogItem.labOrderItemId, {
        recipientName: recipientName.trim(),
        notes: callLogNotes.trim(),
      });
      setSelectedCallLogItem(null);
      load();
    } catch (err: any) {
      setCallLogError(err.response?.data?.message || 'Failed to record call log.');
    } finally {
      setCallLogSubmitting(false);
    }
  };

  const handleManualSave = async () => {
    if (!manualItem) return;
    setSaving(true);
    await enterManualResult(
      manualItem.labOrderItemId,
      manualResult,
      manualNotes || undefined,
      manualUnit || undefined,
      manualRefRange || undefined,
    );
    setManualItem(null);
    setManualResult('');
    setManualNotes('');
    setManualUnit('');
    setManualRefRange('');
    setSaving(false);
    load();
  };

  const handleDownloadReport = async () => {
    if (!id) return;
    setDownloading(true);
    try {
      const blob = await downloadLabReport(id);
      const url  = URL.createObjectURL(blob);
      const a    = document.createElement('a');
      a.href     = url;
      a.download = `LabReport-${order?.patientMrn}-${new Date().toISOString().slice(0, 10)}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } finally {
      setDownloading(false);
    }
  };

  const handleSign = async (item: LabOrderItem) => {
    await signItem(item.labOrderItemId);
    load();
  };

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (!order) return <div className="p-8 text-gray-400">Order not found.</div>;

  const dob = order.patientDob ? new Date(order.patientDob) : null;
  const age = dob ? Math.floor((Date.now() - dob.getTime()) / (365.25 * 24 * 3600 * 1000)) : '';

  const unloggedCriticalItems = order.items.filter(i => i.isCritical && !i.criticalCallLogId);

  return (
    <div className="p-6 max-w-4xl mx-auto">
      {/* Back link */}
      <Link to="/lab-orders" className="text-sm text-blue-600 hover:underline mb-4 inline-block">
        ← Waiting List
      </Link>

      {/* Critical warning banner */}
      {unloggedCriticalItems.length > 0 && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-5 flex items-start justify-between gap-4">
          <div className="flex items-center gap-2">
            <span className="flex h-3 w-3 relative shrink-0">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-3 w-3 bg-red-600"></span>
            </span>
            <div>
              <p className="text-sm font-bold text-red-800 uppercase">
                Critical Panic Value Detected
              </p>
              <p className="text-xs text-red-700 mt-0.5">
                Verbal verification is required immediately. Please contact the ordering clinician and log the call below:{" "}
                <span className="font-semibold">{unloggedCriticalItems.map(i => i.testName).join(', ')}</span>
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="bg-white border rounded-lg p-5 mb-5">
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-xl font-bold text-gray-800">{order.patientName}</h1>
            <p className="text-sm text-gray-500 mt-0.5">
              {age} yrs / {order.patientGender} · MRN: {order.patientMrn}
            </p>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={handleDownloadReport}
              disabled={downloading}
              className="px-3 py-1.5 bg-indigo-600 text-white text-sm rounded hover:bg-indigo-700 disabled:opacity-50 flex items-center gap-1.5"
            >
              {downloading ? 'Generating…' : '↓ Download Report'}
            </button>
            <span className={`px-3 py-1 rounded-full text-sm font-medium ${ORDER_STATUS_COLORS[order.status]}`}>
              {order.status}
            </span>
          </div>
        </div>

        <div className="grid grid-cols-3 gap-4 mt-4 text-sm">
          <div>
            <span className="text-gray-400 text-xs uppercase tracking-wide">Ordered by</span>
            <p className="font-medium">{order.orderingDoctorName}</p>
          </div>
          <div>
            <span className="text-gray-400 text-xs uppercase tracking-wide">Organisation</span>
            <p className="font-medium">{order.organisation}</p>
          </div>
          <div>
            <span className="text-gray-400 text-xs uppercase tracking-wide">Bill</span>
            <p className="font-medium">{order.billNumber ?? '—'}</p>
          </div>
        </div>

        <div className="flex gap-6 mt-4 pt-4 border-t text-sm">
          <span className="text-orange-600">Incomplete: <strong>{order.incompleteCount}</strong></span>
          <span className="text-green-600">Completed: <strong>{order.completedCount}</strong></span>
          <span className="text-purple-600">Signed: <strong>{order.signedCount}</strong></span>
        </div>
      </div>

      {/* Test items */}
      <div className="bg-white border rounded-lg overflow-hidden">
        <div className="px-5 py-3 border-b bg-gray-50">
          <h2 className="text-sm font-semibold text-gray-700">Tests ({order.items.length})</h2>
        </div>
        <div className="divide-y">
          {safeArray(order.items).map(item => (
            <div key={item.labOrderItemId} className="p-4">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-gray-800">{item.testName}</span>
                    {item.isCritical && (
                      <span className="text-[10px] bg-red-600 text-white font-extrabold px-1.5 py-0.5 rounded animate-pulse">
                        ⚠ CRITICAL
                      </span>
                    )}
                    {item.isTatExceeded && (
                      <span className="text-xs bg-red-100 text-red-700 px-2 py-0.5 rounded font-medium">
                        ⚠ TAT Exceeded
                      </span>
                    )}
                    {item.isManualEntry && (
                      <span className="text-xs bg-orange-100 text-orange-700 px-2 py-0.5 rounded">
                        Manual
                      </span>
                    )}
                  </div>
                  <div className="text-xs text-gray-400 mt-0.5">
                    {item.department}
                    {item.instrumentType && ` · ${item.instrumentType}`}
                    {' · TAT '}{item.tatHours}h
                  </div>
                  {item.accessionNumber && (
                    <div className="text-xs font-mono mt-1 text-gray-600">
                      ACC: {item.accessionNumber}
                    </div>
                  )}
                  {item.manualResult && (
                    <div className="mt-2 text-sm flex items-center gap-2 flex-wrap">
                      <span className="text-gray-500">Result:</span>
                      <span className="font-medium">{item.manualResult}</span>
                      {item.manualResultUnit && (
                        <span className="text-gray-400">{item.manualResultUnit}</span>
                      )}
                      {item.manualResultReferenceRange && (
                        <span className="text-gray-400 text-xs">[{item.manualResultReferenceRange}]</span>
                      )}
                      {item.manualResultFlag && (
                        <span className={`px-1.5 py-0.5 rounded text-xs font-bold ${
                          item.manualResultFlag === 'H' ? 'bg-red-100 text-red-700' :
                          item.manualResultFlag === 'L' ? 'bg-blue-100 text-blue-700' :
                          'bg-green-100 text-green-700'
                        }`}>
                          {item.manualResultFlag}
                        </span>
                      )}
                      {item.manualResultNotes && (
                        <span className="text-gray-400">— {item.manualResultNotes}</span>
                      )}
                    </div>
                  )}
                  <div className="text-xs text-gray-400 mt-1 space-x-3">
                    {item.sampleReceivedAt && (
                      <span>Received: {new Date(item.sampleReceivedAt).toLocaleString()}</span>
                    )}
                    {item.resultedAt && (
                      <span>Resulted: {new Date(item.resultedAt).toLocaleString()}</span>
                    )}
                    {item.signedAt && (
                      <span>Signed: {new Date(item.signedAt).toLocaleString()}</span>
                    )}
                  </div>
                </div>

                <div className="flex items-center gap-2 ml-4">
                  {item.isCritical && (
                    item.criticalCallLogId ? (
                      <span className="text-[10px] text-green-700 bg-green-50 border border-green-200 px-2 py-0.5 rounded font-semibold" title={item.criticalCallLogNotes || undefined}>
                        📞 Logged: {item.criticalCallLogRecipient} ({new Date(item.criticalCallLogCalledAt!).toLocaleDateString()})
                      </span>
                    ) : (
                      <button
                        onClick={() => handleOpenCallLogModal(item)}
                        className="px-2.5 py-1 bg-red-650 hover:bg-red-700 text-white text-xs font-bold rounded shadow-sm transition-colors cursor-pointer"
                      >
                        📞 Log Call
                      </button>
                    )
                  )}

                  <span className={`px-2 py-0.5 rounded text-xs font-medium ${ITEM_STATUS_COLORS[item.status]}`}>
                    {item.status}
                  </span>

                  {item.status === 'Ordered' && (
                    <button
                      onClick={() => handleReceive(item)}
                      className="px-3 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700"
                    >
                      Received
                    </button>
                  )}
                  {item.status === 'SampleReceived' && item.accessionNumber && (
                    <button
                      onClick={() => printBarcode(item, order.patientName, order.patientMrn)}
                      className="px-3 py-1 bg-gray-600 text-white text-xs rounded hover:bg-gray-700"
                    >
                      Print
                    </button>
                  )}
                  {item.status === 'SampleReceived' && item.isManualEntry && (
                    <button
                      onClick={() => {
                        setManualItem(item);
                        setManualResult('');
                        setManualNotes('');
                        // no catalog defaults available on item — user fills in
                        setManualUnit('');
                        setManualRefRange('');
                      }}
                      className="px-3 py-1 bg-orange-500 text-white text-xs rounded hover:bg-orange-600"
                    >
                      Enter Result
                    </button>
                  )}
                  {item.status === 'Resulted' && (
                    <button
                      onClick={() => handleSign(item)}
                      className="px-3 py-1 bg-purple-600 text-white text-xs rounded hover:bg-purple-700"
                    >
                      Sign
                    </button>
                  )}
                  {item.labResultId && (
                    <Link
                      to={`/lab-results/${item.labResultId}`}
                      className="px-3 py-1 bg-green-600 text-white text-xs rounded hover:bg-green-700"
                    >
                      View Result
                    </Link>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Manual result modal */}
      {manualItem && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
            <h3 className="text-lg font-semibold mb-1">Enter Result</h3>
            <p className="text-sm text-gray-500 mb-4">{manualItem.testName}</p>

            <textarea
              className="w-full border rounded px-3 py-2 text-sm mb-3 h-20 resize-none"
              placeholder="Result (e.g. Negative, Positive, Grade I, 5.2, etc.)"
              value={manualResult}
              onChange={e => setManualResult(e.target.value)}
            />

            <div className="flex gap-2 mb-3">
              <div className="flex-1">
                <label className="text-xs text-gray-500 mb-1 block">Unit (optional)</label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm"
                  placeholder="e.g. mmol/L"
                  value={manualUnit}
                  onChange={e => setManualUnit(e.target.value)}
                />
              </div>
              <div className="flex-1">
                <label className="text-xs text-gray-500 mb-1 block">Reference Range (optional)</label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm"
                  placeholder="e.g. 3.9-5.6"
                  value={manualRefRange}
                  onChange={e => setManualRefRange(e.target.value)}
                />
              </div>
            </div>
            {manualRefRange && (
              <p className="text-xs text-indigo-600 mb-2">
                Flag (H/L/N) will be auto-computed if the result is numeric.
              </p>
            )}

            <textarea
              className="w-full border rounded px-3 py-2 text-sm mb-4 h-14 resize-none"
              placeholder="Notes (optional)"
              value={manualNotes}
              onChange={e => setManualNotes(e.target.value)}
            />
            <div className="flex gap-2 justify-end">
              <button
                onClick={() => {
                  setManualItem(null);
                  setManualResult('');
                  setManualNotes('');
                  setManualUnit('');
                  setManualRefRange('');
                }}
                className="px-4 py-2 text-sm border rounded hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                disabled={!manualResult.trim() || saving}
                onClick={handleManualSave}
                className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
              >
                {saving ? 'Saving…' : 'Save Result'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Log Call Modal */}
      {selectedCallLogItem && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl border border-gray-150 shadow-2xl max-w-md w-full overflow-hidden font-outfit">
            <div className="bg-red-650 p-4 text-white">
              <h3 className="font-extrabold text-base flex items-center gap-2">
                📞 Record Critical Value Call Log
              </h3>
              <p className="text-red-100 text-xs mt-1">
                CLIA/CAP regulations require verbal verification of panic values.
              </p>
            </div>

            <form onSubmit={handleCallLogSubmit} className="p-5 space-y-4">
              {callLogError && (
                <div className="bg-red-50 border border-red-200 text-red-800 text-xs rounded-xl p-3 font-semibold">
                  ⚠️ {callLogError}
                </div>
              )}

              <div className="space-y-1">
                <label className="block text-xs font-bold text-gray-600 uppercase">Patient</label>
                <div className="bg-gray-50 border border-gray-100 rounded-xl p-2.5 text-xs text-gray-700 font-semibold">
                  {order.patientName} ({order.patientMrn})
                </div>
              </div>

              <div className="space-y-1">
                <label className="block text-xs font-bold text-gray-600 uppercase">Test & Result</label>
                <div className="bg-red-50/50 border border-red-100 rounded-xl p-2.5 text-xs text-red-950 font-bold">
                  {selectedCallLogItem.testName}: {selectedCallLogItem.manualResult || selectedCallLogItem.hl7ResultValue} {selectedCallLogItem.manualResultUnit || selectedCallLogItem.hl7ResultUnit}
                </div>
              </div>

              <div className="space-y-1">
                <label htmlFor="recipientName" className="block text-xs font-bold text-gray-700 uppercase">
                  Recipient Clinician Name <span className="text-red-500">*</span>
                </label>
                <input
                  id="recipientName"
                  type="text"
                  required
                  placeholder="e.g. Dr. Jane Smith"
                  value={recipientName}
                  onChange={(e) => setRecipientName(e.target.value)}
                  className="w-full border border-gray-250 rounded-xl px-3.5 py-2.5 text-xs focus:ring-2 focus:ring-sky-500 focus:border-sky-500 outline-none"
                />
              </div>

              <div className="space-y-1">
                <label htmlFor="callLogNotes" className="block text-xs font-bold text-gray-700 uppercase">
                  Read-back Notes / Confirmation
                </label>
                <textarea
                  id="callLogNotes"
                  rows={3}
                  value={callLogNotes}
                  onChange={(e) => setCallLogNotes(e.target.value)}
                  placeholder="e.g. Read-back confirmed. Patient being scheduled for immediate IV saline."
                  className="w-full border border-gray-250 rounded-xl px-3.5 py-2.5 text-xs focus:ring-2 focus:ring-sky-500 focus:border-sky-500 outline-none resize-none"
                />
              </div>

              <div className="flex gap-2 justify-end pt-2 border-t border-gray-100">
                <button
                  type="button"
                  onClick={() => setSelectedCallLogItem(null)}
                  disabled={callLogSubmitting}
                  className="px-4 py-2.5 border border-gray-200 text-gray-600 font-semibold text-xs rounded-xl hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={callLogSubmitting}
                  className="px-4 py-2.5 bg-red-650 hover:bg-red-700 text-white font-bold text-xs rounded-xl shadow-sm transition-all"
                >
                  {callLogSubmitting ? 'Recording...' : 'Confirm & Resolve Alert'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
