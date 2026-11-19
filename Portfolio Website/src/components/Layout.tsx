import { useEffect, useState } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import Footer from './Footer';
import Sidebar, { type Theme } from './Sidebar';

function savedTheme(): Theme {
  try { return localStorage.getItem('theme') === 'light' ? 'light' : 'dark'; } catch { return 'dark'; }
}

/** Sidebar + page content + footer, shared by every page. */
export default function Layout() {
  const [theme, setTheme] = useState<Theme>(savedTheme);
  const { pathname, hash } = useLocation();

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    try { localStorage.setItem('theme', theme); } catch { /* storage blocked */ }
  }, [theme]);

  // New page: go to the top, or to the #section in the link (e.g. /#faq).
  useEffect(() => {
    if (hash) {
      requestAnimationFrame(() => document.getElementById(hash.slice(1))?.scrollIntoView());
    } else {
      window.scrollTo(0, 0);
    }
  }, [pathname, hash]);

  return (
    <>
      <Sidebar theme={theme} setTheme={setTheme} />
      <div className="page">
        <main className="window">
          <Outlet />
          <Footer />
        </main>
      </div>
    </>
  );
}
