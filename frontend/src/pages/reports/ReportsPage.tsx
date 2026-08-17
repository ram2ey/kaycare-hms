import { useState } from 'react';
import ClinicalTab   from './tabs/ClinicalTab';
import InpatientTab  from './tabs/InpatientTab';
import LabTab        from './tabs/LabTab';
import ReferralsTab  from './tabs/ReferralsTab';
import PharmacyTab   from './tabs/PharmacyTab';

const TABS = ['Clinical', 'Inpatient', 'Lab', 'Referrals', 'Pharmacy'] as const;
type Tab = typeof TABS[number];

function todayStr() { return new Date().toISOString().split('T')[0]; }
function daysAgoStr(n: number) {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return d.toISOString().split('T')[0];
}

export default function ReportsPage() {
  const [tab, setTab]   = useState<Tab>('Clinical');
  const [from, setFrom] = useState(daysAgoStr(29));
  const [to, setTo]     = useState(todayStr());

  const range = { from, to };

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Reports & Analytics</h1>
          <p className="text-gray-500 text-sm mt-0.5">Operational and clinical performance overview</p>
        </div>
        <div className="flex items-center gap-2 text-sm">
          <label htmlFor="reports-from" className="text-gray-500">From</label>
          <input id="reports-from" type="date" value={from} max={to} onChange={e => setFrom(e.target.value)}
            className="px-3 py-1.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500" />
          <label htmlFor="reports-to" className="text-gray-500">To</label>
          <input id="reports-to" type="date" value={to} min={from} max={todayStr()} onChange={e => setTo(e.target.value)}
            className="px-3 py-1.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500" />
        </div>
      </div>

      {/* Tab bar */}
      <div className="flex border-b border-gray-200 mb-6">
        {TABS.map(t => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`px-5 py-2.5 text-sm font-medium border-b-2 transition-colors -mb-px ${
              tab === t
                ? 'border-blue-600 text-blue-700'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            {t}
          </button>
        ))}
      </div>

      {tab === 'Clinical'  && <ClinicalTab  range={range} />}
      {tab === 'Inpatient' && <InpatientTab range={range} />}
      {tab === 'Lab'       && <LabTab       range={range} />}
      {tab === 'Referrals' && <ReferralsTab range={range} />}
      {tab === 'Pharmacy'  && <PharmacyTab  range={range} />}
    </div>
  );
}
