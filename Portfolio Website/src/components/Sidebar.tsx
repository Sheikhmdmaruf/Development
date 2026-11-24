import { useEffect, useState, type ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { site } from '../data/live';
import { ArrowUpRight, Dribbble, GitHub, Grid, Home, LinkedIn, Mail, Menu, Moon, Pen, Send, Services, Sun, User } from './Icons';

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
  { key: 'services', to: '/services', label: 'Services', icon: <Services /> },
  { key: 'work', to: '/work', label: 'Work', icon: <Grid /> },
  { key: 'about', to: '/about', label: 'About', icon: <User /> },
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
            <span><div className="nav-name">Sheikh Md Maruf</div><div className="nav-role">Senior Product Designer</div></span>
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
            <External href={site('linkedin_url', 'https://www.linkedin.com/in/sheikhmdmaruf/')} icon={<LinkedIn />} label="LinkedIn" />
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
