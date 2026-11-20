import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { POSTS, PROJECTS } from '../data/content';
import { site } from '../data/live';
import { postHref, slug } from '../lib';
import { ArrowUpRight, Dribbble, GitHub, Grid, Home, LinkedIn, Mail, Menu, Moon, Pen, Search, Send, Services, Sun, User } from './Icons';

export type Theme = 'dark' | 'light';

const AVATAR = 'https://cdn.dribbble.com/users/10909349/avatars/normal/786be272a45d098029b23504313c8718.jpg';

/** Which main menu item is selected for the current URL. */
function activeKey(path: string): string {
  if (path === '/') return 'home';
  if (path.startsWith('/work') || path.startsWith('/project')) return 'work';
  if (path.startsWith('/service')) return 'services';
  if (path.startsWith('/blog')) return 'blog';
  if (path.startsWith('/about')) return 'about';
  return '';
}

const MAIN: { key: string; to: string; label: string; icon: ReactNode }[] = [
  { key: 'home', to: '/', label: 'Home', icon: <Home /> },
  { key: 'about', to: '/about', label: 'About', icon: <User /> },
  { key: 'work', to: '/work', label: 'Work', icon: <Grid /> },
  { key: 'services', to: '/services', label: 'Services', icon: <Services /> },
  { key: 'blog', to: '/blog', label: 'Blog', icon: <Pen /> },
];

const External = ({ href, icon, label }: { href: string; icon: ReactNode; label: string }) => (
  <a className="nav-item" href={href} target="_blank" rel="noopener">{icon}{label}<ArrowUpRight className="na" /></a>
);

export default function Sidebar({ theme, setTheme }: { theme: Theme; setTheme: (t: Theme) => void }) {
  const { pathname } = useLocation();
  const [open, setOpen] = useState(false);
  const active = activeKey(pathname);

  useEffect(() => setOpen(false), [pathname]);

  return (
    <aside className={`nav${open ? ' open' : ''}`} id="nav">
      <div className="nav-top">
        <div className="nav-group">
          <Link to="/" className="panel nav-profile">
            <Avatar />
            <span><div className="nav-name">Sheikh Md Maruf</div><div className="nav-role">UI/UX &amp; brand designer</div></span>
          </Link>
          <div className="nav-status-strip"><span className="status"><i />{site('availability', 'Available for work')}</span></div>
        </div>
        <button className="menu-btn" onClick={() => setOpen(o => !o)} aria-label="Menu"><Menu /></button>
      </div>

      <div className="nav-rest">
        <div className="nav-group">
          <div className="nav-label">Main</div>
          <div className="panel nav-panel" id="mainNav">
            {MAIN.map(m => (
              <Link key={m.key} className={`nav-item${active === m.key ? ' active' : ''}`} to={m.to}>{m.icon}{m.label}</Link>
            ))}
          </div>
        </div>

        <div className="nav-group">
          <div className="nav-label">Connect</div>
          <div className="panel nav-panel">
            <a className="nav-item" href="#contact" onClick={() => setOpen(false)}><Mail />Contact</a>
            <External href={site('dribbble_url', 'https://dribbble.com/sheikhmdmaruf')} icon={<Dribbble />} label="Dribbble" />
            <External href={site('github_url', 'https://github.com/Sheikhmdmaruf')} icon={<GitHub />} label="GitHub" />
            {/* LinkedIn shows once its URL is set in the admin dashboard → Site Settings */}
            {site('linkedin_url', '') && <External href={site('linkedin_url', '')} icon={<LinkedIn />} label="LinkedIn" />}
          </div>
        </div>

        <div className="nav-group">
          <div className="nav-theme">
            Theme
            <div className="toggle">
              <button className={theme === 'dark' ? 'on' : ''} onClick={() => setTheme('dark')} aria-label="Dark theme"><Moon /></button>
              <button className={theme === 'light' ? 'on' : ''} onClick={() => setTheme('light')} aria-label="Light theme"><Sun /></button>
            </div>
          </div>
          <div className="panel nav-panel nav-bottom">
            <NavSearch />
            <a href="#contact" className="btn btn-full" onClick={() => setOpen(false)}><Send />Start a project</a>
          </div>
        </div>
      </div>
    </aside>
  );
}

function Avatar() {
  const [failed, setFailed] = useState(false);
  return <span className="avatar">{failed ? 'SM' : <img src={AVATAR} alt="Sheikh Md Maruf" onError={() => setFailed(true)} />}</span>;
}

interface Hit { t: string; s: string; h: string; k?: string }

/** Sidebar search over pages, projects and blog posts. */
function NavSearch() {
  const navigate = useNavigate();
  const formRef = useRef<HTMLFormElement>(null);
  const [q, setQ] = useState('');
  const [sel, setSel] = useState(0);
  const [visible, setVisible] = useState(false);

  const all = useMemo<Hit[]>(() => [
    { t: 'Home', s: 'Page', h: '/' }, { t: 'About', s: 'Page', h: '/about' },
    { t: 'Work', s: 'Page', h: '/work' }, { t: 'Blog', s: 'Page', h: '/blog' },
    { t: 'Services', s: 'Page', h: '/services' }, { t: 'FAQ', s: 'Page', h: '/#faq' },
    ...PROJECTS.map(p => ({ t: p.t, s: 'Project · ' + p.cat, h: '/project/' + slug(p), k: p.sum + ' ' + p.hl })),
    ...POSTS.map(b => ({ t: b.t, s: 'Blog · ' + b.cat, h: postHref(b) })),
  ], []);

  const query = q.trim().toLowerCase();
  const found = query ? all.filter(i => `${i.t} ${i.s} ${i.k || ''}`.toLowerCase().includes(query)).slice(0, 8) : [];

  useEffect(() => {
    const close = (e: MouseEvent) => { if (!formRef.current?.contains(e.target as Node)) setVisible(false); };
    document.addEventListener('click', close);
    return () => document.removeEventListener('click', close);
  }, []);

  const go = (h: string) => {
    setVisible(false); setQ('');
    if (h.startsWith('http')) window.open(h, '_blank', 'noopener'); else navigate(h);
  };

  return (
    <form className="nav-search" role="search" autoComplete="off" ref={formRef}
      onSubmit={e => { e.preventDefault(); if (found[sel]) go(found[sel].h); }}>
      <Search />
      <label className="visually-hidden" htmlFor="navSearchInput">Search</label>
      <input className="nav-search-input" id="navSearchInput" type="search" placeholder="Search" value={q}
        onChange={e => { setQ(e.target.value); setSel(0); setVisible(true); }}
        onKeyDown={e => {
          if (e.key === 'Escape') { setVisible(false); e.currentTarget.blur(); return; }
          if (!found.length) return;
          if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            e.preventDefault();
            setSel(s => (s + (e.key === 'ArrowDown' ? 1 : -1) + found.length) % found.length);
          }
        }} />
      <div className="search-results" hidden={!visible || !query}>
        {found.length
          ? found.map((i, n) => (
            <a key={i.h + i.t} href={i.h} className={n === sel ? 'on' : undefined}
              onClick={e => { e.preventDefault(); go(i.h); }}>{i.t}<span>{i.s}</span></a>
          ))
          : <div className="none">No results</div>}
      </div>
    </form>
  );
}
