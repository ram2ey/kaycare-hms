import { useState, useRef, useEffect } from 'react';
import { searchMedications, type MedicationEntry } from '../data/medications';

interface Props {
  value: string;
  onChange: (value: string) => void;
  onSelect: (medication: MedicationEntry) => void;
  placeholder?: string;
  className?: string;
}

export default function MedicationAutocomplete({ value, onChange, onSelect, placeholder, className }: Props) {
  const [results, setResults] = useState<MedicationEntry[]>([]);
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const matches = searchMedications(value);
    setResults(matches);
    setOpen(matches.length > 0 && value.length >= 2);
    setActiveIndex(-1);
  }, [value]);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  function handleKeyDown(e: React.KeyboardEvent) {
    if (!open) return;
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setActiveIndex((i) => Math.min(i + 1, results.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setActiveIndex((i) => Math.max(i - 1, -1));
    } else if (e.key === 'Enter' && activeIndex >= 0) {
      e.preventDefault();
      pick(results[activeIndex]);
    } else if (e.key === 'Escape') {
      setOpen(false);
    }
  }

  function pick(med: MedicationEntry) {
    onSelect(med);
    setOpen(false);
    setResults([]);
  }

  return (
    <div ref={containerRef} className="relative">
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={handleKeyDown}
        onFocus={() => results.length > 0 && setOpen(true)}
        placeholder={placeholder}
        className={className}
        autoComplete="off"
      />
      {open && (
        <ul className="absolute z-50 left-0 right-0 top-full mt-1 bg-white border border-gray-200 shadow-lg max-h-64 overflow-y-auto">
          {results.map((med, i) => (
            <li
              key={med.name}
              onMouseDown={() => pick(med)}
              className={`px-3 py-2 cursor-pointer text-sm ${
                i === activeIndex ? 'bg-blue-50 text-blue-800' : 'hover:bg-gray-50'
              }`}
            >
              <div className="font-medium text-gray-800">{med.name}</div>
              <div className="text-xs text-gray-400">
                {med.genericName} · {med.strengths[0]} · {med.dosageForm}
                {med.isControlledSubstance && (
                  <span className="ml-2 text-red-600 font-medium">CS</span>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
