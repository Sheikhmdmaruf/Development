import { useState } from 'react';
import { POSTS } from '../data/content';
import { SITE_TITLE, useTitle } from '../lib';
import { ArticleCard } from '../components/Cards';
import { ArrowRight } from '../components/Icons';
import Reveal from '../components/Reveal';
import { FilterBar, useView, ViewToggle } from '../components/Toolbar';

const FILTERS: [string, string][] = [['all', 'All'], ['UI/UX', 'UI/UX'], ['Web Design', 'Web Design'], ['Freelancing', 'Freelancing']];

export default function BlogPage() {
  useTitle(`Blog — ${SITE_TITLE}`);
  const [cat, setCat] = useState('all');
  const [view, setView] = useView('blogView');
  const list = POSTS.filter(b => cat === 'all' || b.cat === cat);

  return (
    <>
      <section id="home">
        <div className="shell">
          <div className="panel page-header-panel">
            <div className="page-header-lead">
              <a href="#contact" className="announce"><b>Blog</b><span>New notes every month</span><ArrowRight /></a>
              <h1 className="page-title">Notes on design<span className="m">and freelancing.</span></h1>
              <p className="hero-text">What I learn from client work, designing apps and websites, and growing as a freelance designer.</p>
            </div>
          </div>
        </div>
      </section>

      <section id="posts">
        <Reveal className="work-toolbar">
          <FilterBar options={FILTERS} value={cat} onChange={setCat} />
          <ViewToggle value={view} onChange={setView} />
        </Reveal>
        <Reveal className={`projects-grid is-three${view === 'list' ? ' is-list' : ''}`}>
          {list.length ? list.map(b => <ArticleCard key={b.t} b={b} withRow />) : <div className="panel empty">No posts yet.</div>}
        </Reveal>
      </section>
    </>
  );
}
