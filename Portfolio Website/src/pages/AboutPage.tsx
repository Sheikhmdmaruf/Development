import { Link } from 'react-router-dom';
import { SITE_TITLE, useTitle } from '../lib';
import { ArrowRight, RawIcon, Send } from '../components/Icons';
import Reveal from '../components/Reveal';
import { SectionHead } from '../components/Sections';
import { HeroButton, Portrait } from './HomePage';
import { site, toolLogo } from '../data/live';

// TODO: replace industry names with your client logos: <img className="client-logo-image" src="/images/client-1.svg" alt="Client" />
const CLIENTS = ['Fintech', 'Food delivery', 'E-commerce', 'Healthcare', 'Travel', 'Gaming', 'AI & SaaS', 'Automotive'];

// TODO: check dates and roles
const EXPERIENCE = [
  { when: '2024 — Now', title: 'Freelance UI/UX Designer', sub: 'Fiverr & direct clients', where: 'Remote', current: true },
  { when: '20XX — 20XX', title: 'UI/UX Designer', sub: 'Netro Systems', where: 'Bangladesh' },
  { when: '20XX — 20XX', title: 'Graphic Designer', sub: 'Brand, print & social media design', where: 'Bangladesh' },
];

const TOOLS: [string, string, string][] = [
  ['Figma', 'UI design and prototyping', '<path d="M9 3h3v6H9a3 3 0 0 1 0-6zM12 3h3a3 3 0 0 1 0 6h-3zM9 9h3v6H9a3 3 0 0 1 0-6zM12 12a3 3 0 1 1 6 0 3 3 0 0 1-6 0zM9 15h3v3a3 3 0 1 1-3-3z"/>'],
  ['Adobe XD', 'Wireframes and app flows', '<rect x="3" y="4" width="18" height="16" rx="3"/><path d="m7 9 4 6M11 9l-4 6M14 9v6h1.5a3 3 0 0 0 0-6z"/>'],
  ['Photoshop', 'Image editing and mockups', '<rect x="3" y="4" width="18" height="16" rx="3"/><path d="M7 15V9h2a2 2 0 0 1 0 4H7M17 10.5c-.5-.6-2.5-.8-2.5.4 0 1.4 2.8.8 2.8 2.4 0 1.2-2 1.2-3 .5"/>'],
  ['Illustrator', 'Logos, icons and brand assets', '<rect x="3" y="4" width="18" height="16" rx="3"/><path d="m7 15 2-6 2 6M7.7 13h2.6M14 11v4M14 9v.01"/>'],
  ['FigJam', 'Research, flows and workshops', '<rect x="3" y="3" width="8" height="8" rx="2"/><circle cx="17" cy="7" r="4"/><path d="M3 17h8M7 13v8M14 14l7 7M21 14l-7 7"/>'],
  ['Framer & Webflow', 'Building live websites', '<path d="M6 3h12v6H12zM6 9h6l6 6H6zM6 15h6v6z"/>'],
];

// TODO: replace with awards / features when you have them
const PROFILES = [
  { href: 'https://dribbble.com/sheikhmdmaruf', meta: 'Dribbble', title: '16+ shots published', sub: 'Websites, apps and dashboards', aside: 'dribbble.com/sheikhmdmaruf' },
  { href: 'https://www.fiverr.com/sheikhmdmaruf01', meta: 'Fiverr', title: 'UI/UX & Graphic Designer', sub: 'Landing pages, apps and flyers', aside: 'fiverr.com/sheikhmdmaruf01' },
  { href: 'https://github.com/Sheikhmdmaruf', meta: 'GitHub', title: 'Design & development projects', sub: 'Product design and code', aside: 'github.com/Sheikhmdmaruf' },
];

export default function AboutPage() {
  useTitle(`About — ${SITE_TITLE}`);
  return (
    <>
      <section id="home">
        <div className="shell">
          <div className="hero-row is-half">
            <div className="panel hero-copy">
              {/* Texts, links and photo can be changed in the dashboard (Hero section) */}
              <Link to="/work" className="announce"><b>{site('about_hero_badge', 'Open')}</b><span>{site('about_hero_announce', 'Available for new projects')}</span><ArrowRight /></Link>
              <h1>{site('about_hero_title', 'Part designer,')} <span className="m">{site('about_hero_title_accent', 'part problem solver.')}</span></h1>
              <p className="hero-text">{site('about_hero_text', "I'm Sheikh Md Maruf, a UI/UX and brand designer based in Sirajganj, Bangladesh. For over four years I've helped businesses turn ideas into websites and apps people enjoy using.")}</p>
              <div className="actions">
                <HeroButton href={site('about_hero_btn1_link', '#contact')} className="btn"><Send />{site('about_hero_btn1_text', 'Start a project')}</HeroButton>
                <HeroButton href={site('about_hero_btn2_link', '/work')} className="btn btn-sec">{site('about_hero_btn2_text', 'View work')}</HeroButton>
              </div>
            </div>
            <Portrait />
          </div>
        </div>
      </section>

      <section id="clients">
        <SectionHead title="Clients" text="Industries and teams I have designed for." />
        <Reveal className="clients-grid">
          {CLIENTS.map(c => <div className="client-logo" key={c}><span className="client-word">{c}</span></div>)}
        </Reveal>
      </section>

      <section id="experience">
        <SectionHead title="Experience" text="Freelance work and the teams I have worked with." />
        <Reveal className="timeline-shell">
          <div className="timeline">
            {EXPERIENCE.map(x => (
              <div className="timeline-row" key={x.title}>
                <div className="timeline-body">
                  <p className="timeline-meta">{x.when}</p>
                  <div className="timeline-text">
                    <div className="timeline-title-row"><h3 className="timeline-title">{x.title}</h3>{x.current && <span className="timeline-pill">Current</span>}</div>
                    <p className="timeline-subtitle">{x.sub}</p>
                  </div>
                </div>
                <p className="timeline-aside">{x.where}</p>
              </div>
            ))}
          </div>
        </Reveal>
      </section>

      <section id="toolkit">
        <SectionHead title="Tools I use" text="The small stack behind every project." />
        <Reveal className="tools-grid">
          {TOOLS.map(([name, use, icon]) => {
            // Logo uploaded in the dashboard; "Framer & Webflow" uses the Framer or Webflow logo.
            const logo = toolLogo(name) ?? name.split('&').map(part => toolLogo(part)).find(Boolean);
            return (
              <div className="tool-card" key={name}>
                <div className="tool-card-icon-box">
                  {logo ? <img className="tool-card-logo" src={logo} alt={`${name} logo`} loading="lazy" /> : <RawIcon markup={icon} size={20} />}
                </div>
                <div className="tool-card-text"><h3 className="tool-card-name">{name}</h3><p className="tool-card-use">{use}</p></div>
              </div>
            );
          })}
        </Reveal>
      </section>

      <section id="recognition">
        <SectionHead title="Recognition" text="Where my work is shared and reviewed online." />
        <Reveal className="info-list-shell">
          <div className="info-list">
            {PROFILES.map(p => (
              <a className="info-row" href={p.href} target="_blank" rel="noopener" key={p.meta}>
                <p className="info-row-meta">{p.meta}</p>
                <div className="info-row-text"><h3 className="info-row-title">{p.title}</h3><p className="info-row-subtitle">{p.sub}</p></div>
                <p className="info-row-aside">{p.aside}</p>
              </a>
            ))}
          </div>
        </Reveal>
      </section>
    </>
  );
}
