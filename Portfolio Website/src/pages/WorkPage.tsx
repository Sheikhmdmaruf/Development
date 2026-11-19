import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { PROJECTS } from '../data/content';
import type { ProjectType } from '../data/types';
import { SITE_TITLE, useTitle } from '../lib';
import { ProjectCard } from '../components/Cards';
import { ArrowRight } from '../components/Icons';
import Reveal from '../components/Reveal';
import { FilterBar, useView, ViewToggle } from '../components/Toolbar';

type Filter = 'all' | ProjectType;
const FILTERS: [Filter, string][] = [['all', 'All'], ['web', 'Website Design'], ['app', 'Mobile App Design'], ['dash', 'Dashboard Design']];

export default function WorkPage() {
  useTitle(`Work — ${SITE_TITLE}`);
  const [params] = useSearchParams();
  const start = FILTERS.find(f => f[0] === params.get('type'))?.[0] ?? 'all'; // e.g. /work?type=app
  const [filter, setFilter] = useState<Filter>(start);
  const [view, setView] = useView('workView');
  const list = PROJECTS.filter(p => filter === 'all' || p.type === filter);
  const latest = PROJECTS[0];

  return (
    <>
      <section id="home">
        <div className="shell">
          <div className="panel page-header-panel">
            <div className="page-header-lead">
              <a href={latest.url} target="_blank" rel="noopener" className="announce"><b>New</b><span>{latest.t}</span><ArrowRight /></a>
              <h1 className="page-title">Selected projects,<span className="m">start to finish.</span></h1>
              <p className="hero-text">Websites, mobile apps and dashboards, each designed around one clear business goal.</p>
            </div>
          </div>
        </div>
      </section>

      <section id="projects">
        <Reveal className="work-toolbar">
          <FilterBar options={FILTERS} value={filter} onChange={setFilter} />
          <ViewToggle value={view} onChange={setView} />
        </Reveal>
        <Reveal className={`projects-grid${view === 'list' ? ' is-list' : ''}`}>
          {list.length ? list.map(p => <ProjectCard key={p.t} p={p} />) : <div className="panel empty">No projects yet.</div>}
        </Reveal>
      </section>
    </>
  );
}
