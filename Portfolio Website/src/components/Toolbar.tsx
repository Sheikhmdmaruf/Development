import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Grid, List } from './Icons';

export type View = 'grid' | 'list';

/** Grid/list choice: ?view= in the URL, else the last choice saved in the browser. */
export function useView(storageKey: string): [View, (v: View) => void] {
  const [params] = useSearchParams();
  const [view, setView] = useState<View>(() => {
    const fromUrl = params.get('view');
    if (fromUrl === 'grid' || fromUrl === 'list') return fromUrl;
    try { return localStorage.getItem(storageKey) === 'list' ? 'list' : 'grid'; } catch { return 'grid'; }
  });
  useEffect(() => { try { localStorage.setItem(storageKey, view); } catch { /* storage blocked */ } }, [storageKey, view]);
  return [view, setView];
}

export function FilterBar<T extends string>({ options, value, onChange }: { options: [T, string][]; value: T; onChange: (v: T) => void }) {
  return (
    <div className="filter-bar">
      {options.map(([v, label]) => (
        <button key={v} className={`filter-tab${value === v ? ' is-active' : ''}`} onClick={() => onChange(v)}>{label}</button>
      ))}
    </div>
  );
}

export function ViewToggle({ value, onChange }: { value: View; onChange: (v: View) => void }) {
  return (
    <div className="filter-bar view-toggle">
      <button className={`filter-tab${value === 'grid' ? ' is-active' : ''}`} onClick={() => onChange('grid')}><Grid size={14} />Grid</button>
      <button className={`filter-tab${value === 'list' ? ' is-active' : ''}`} onClick={() => onChange('list')}><List />List</button>
    </div>
  );
}
