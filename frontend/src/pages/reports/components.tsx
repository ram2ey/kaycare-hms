// Shared components for report tabs

export function StatCard({ label, value, sub, color = 'text-gray-800' }: {
  label: string; value: string | number; sub?: string; color?: string;
}) {
  return (
    <div className="bg-white border border-gray-200 rounded-xl p-5">
      <p className="text-xs text-gray-500 uppercase tracking-wide mb-1">{label}</p>
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
      {sub && <p className="text-xs text-gray-400 mt-1">{sub}</p>}
    </div>
  );
}

export function BarTable({ title, rows }: {
  title: string;
  rows: { name: string; count: number }[];
}) {
  const max = Math.max(...rows.map(r => r.count), 1);
  return (
    <div className="bg-white border border-gray-200 rounded-xl p-5">
      <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">{title}</h3>
      {rows.length === 0 ? (
        <p className="text-sm text-gray-400">No data.</p>
      ) : (
        <div className="space-y-2">
          {rows.map(r => (
            <div key={r.name} className="flex items-center gap-3 text-sm">
              <div className="w-36 shrink-0 text-gray-600 truncate" title={r.name}>{r.name}</div>
              <div className="flex-1 bg-gray-100 rounded-full h-2">
                <div className="bg-blue-500 h-2 rounded-full" style={{ width: `${(r.count / max) * 100}%` }} />
              </div>
              <div className="w-10 text-right font-medium text-gray-800">{r.count}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export function MiniLineChart({ title, data }: {
  title: string;
  data: { date: string; count: number }[];
}) {
  const max = Math.max(...data.map(d => d.count), 1);
  const w = 4;
  const h = 60;
  const pts = data.map((d, i) => `${i * w},${h - (d.count / max) * h}`).join(' ');

  return (
    <div className="bg-white border border-gray-200 rounded-xl p-5">
      <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">{title}</h3>
      {data.length === 0 ? (
        <p className="text-sm text-gray-400">No data.</p>
      ) : (
        <div className="overflow-x-auto">
          <svg width={data.length * w} height={h + 4} className="min-w-full">
            <polyline
              points={pts}
              fill="none"
              stroke="#3b82f6"
              strokeWidth="1.5"
              strokeLinejoin="round"
            />
          </svg>
          <div className="flex justify-between text-xs text-gray-400 mt-1">
            <span>{data[0]?.date}</span>
            <span>{data[data.length - 1]?.date}</span>
          </div>
        </div>
      )}
    </div>
  );
}

export function LoadingState() {
  return <div className="py-16 text-center text-gray-400">Loading…</div>;
}

export function ErrorState({ msg }: { msg: string }) {
  return <div className="py-16 text-center text-red-500">{msg}</div>;
}

export function EmptyState({ period }: { period?: string }) {
  return (
    <div className="py-16 text-center">
      <p className="text-gray-400 text-sm">No activity recorded{period ? ` for ${period}` : ' for this period'}.</p>
      <p className="text-gray-300 text-xs mt-1">Try expanding the date range.</p>
    </div>
  );
}
