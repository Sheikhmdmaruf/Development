import { Link, useParams } from 'react-router-dom';
import { CASES } from '../data/cases';
import { PROJECTS } from '../data/content';
import { pad2, SITE_TITLE, slug, useTitle } from '../lib';
import { Media } from '../components/Art';
import { ProjectCard } from '../components/Cards';
import { ArrowLeft, ArrowUpRight, METRIC_ICONS, RawIcon } from '../components/Icons';
import Reveal from '../components/Reveal';

const ROLE_BY_TYPE = { web: 'Website design (UI/UX)', app: 'Mobile app design (UI/UX)', dash: 'Dashboard design (UI/UX)' } as const;

export default function ProjectPage() {
  const { slug: id } = useParams();
  const p = PROJECTS.find(x => slug(x) === id) ?? PROJECTS[0];
  const c = CASES[slug(p)] ?? {};
  useTitle(`${p.t} — ${SITE_TITLE}`);

  // Details panel rows: [label, value, link]
  const rows: [string, string, string?][] = [
    ['Client', c.client || 'Concept project'],
    ['Year', p.y, '/work'],
    ['Role', c.role || ROLE_BY_TYPE[p.type], '/services'],
    ['Category', p.cat, `/work?type=${p.type}`],
  ];

  const metrics = c.metrics ?? [
    { label: 'Category', value: p.cat, cap: p.hl, icon: 'grid' },
    { label: 'Year', value: p.y, cap: 'project year', icon: 'clock' },
    { label: 'Platform', value: p.type === 'app' ? 'Mobile' : 'Web', cap: p.type === 'app' ? 'iOS & Android' : 'desktop & mobile', icon: 'star' },
  ];

  // 01 Challenge, 02 Process, 03 Result
  const steps = c.steps ?? [
    { title: 'Challenge', body: [`${p.sum} The goal was a ${p.cat.toLowerCase()} design that is clear at first look and easy to use on every screen size.`] },
    { title: 'Process', body: ['I started with research and wireframes to plan the layout and flow, then moved to visual design in Figma with clear type, a consistent colour palette and reusable components.'] },
    { title: 'Result', body: [`A finished ${p.hl.toLowerCase()} design, ready for development. You can see all the screens on Dribbble.`] },
  ];

  const g = c.gallery ?? [];
  const art = { type: p.type, c1: p.c1, c2: p.c2, alt: p.t };
  const i = PROJECTS.indexOf(p);
  const more = [1, 2].map(k => PROJECTS[(i + k) % PROJECTS.length]).filter(x => x !== p);

  return (
    <>
      <section id="home">
        <div className="shell">
          <div className="detail-row-grid">
            <div className="panel detail-copy">
              <div className="detail-lead">
                <Link to="/work" className="back-link"><ArrowLeft />Work</Link>
                <h1 className="detail-title">{p.t}</h1>
                <p className="detail-summary">{p.sum}</p>
              </div>
              <div className="detail-actions"><a href="#contact" className="btn">Start a similar project</a></div>
            </div>
            <div className="panel detail-panel">
              {rows.map(([label, value, to]) => (
                <div className="detail-row" key={label}>
                  <span className="detail-row-label">{label}</span>
                  {to ? <Link className="detail-row-value" to={to}>{value}</Link> : <span className="detail-row-value">{value}</span>}
                </div>
              ))}
              <div className="detail-row">
                <span className="detail-row-label">Services</span>
                <span className="inline-links">
                  <Link className="inline-link" to="/services">{p.cat} Design</Link>
                  <a className="inline-link" href={p.url} target="_blank" rel="noopener">Dribbble</a>
                </span>
              </div>
            </div>
          </div>

          <div className="detail-metrics">
            {metrics.map(m => (
              <div className="panel metric-card" key={m.label}>
                <div className="metric-card-top">
                  <span className="metric-card-label">{m.label}</span>
                  <RawIcon className="metric-card-icon" markup={METRIC_ICONS[m.icon] ?? METRIC_ICONS.star} size={18} />
                </div>
                <div className="metric-card-body"><div className="metric-card-value">{m.value}</div><div className="metric-card-caption">{m.cap}</div></div>
              </div>
            ))}
          </div>

          <Media className="detail-cover" src={p.cover || p.img} {...art} />

          <div className="story-steps">
            {steps.map((s, n) => (
              <div className="story-step" key={s.title}>
                <div className="story-step-label"><span className="story-step-index">{pad2(n + 1)}</span><h2 className="story-step-title">{s.title}</h2></div>
                <div className="story-step-body">{s.body.map(t => <p className="rich-p" key={t}>{t}</p>)}</div>
              </div>
            ))}
          </div>

          <div className="gallery">
            <Media className="gallery-media" src={g[0]} {...art} />
            <div className="gallery-row">
              <Media className="gallery-media is-half" src={g[1]} {...art} />
              <Media className="gallery-media is-half" src={g[2]} {...art} />
            </div>
          </div>
        </div>
      </section>

      <section id="more">
        <Reveal className="head">
          <h2>More work</h2>
          <Link to="/work" className="btn btn-ghost">All work <ArrowUpRight size={16} className="arr-ur" /></Link>
        </Reveal>
        <Reveal className="projects-grid">{more.map(x => <ProjectCard key={x.t} p={x} />)}</Reveal>
      </section>
    </>
  );
}
