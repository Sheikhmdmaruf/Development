import { useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { POSTS, PROJECTS, SERVICES, TOOLS } from '../data/content';
import { SITE_TITLE, useTitle } from '../lib';
import { BlogProductCard, ProjectCard, ServiceCard } from '../components/Cards';
import { ArrowRight, Grid, RawIcon, METRIC_ICONS, Sparkle } from '../components/Icons';
import Reveal from '../components/Reveal';
import { site, toolLogo } from '../data/live';
import { FaqList, Reviews, SectionHead } from '../components/Sections';

/** Counts up from 0 to `end` once. */
function Counter({ end, suffix = '' }: { end: number; suffix?: string }) {
  const [n, setN] = useState(0);
  useEffect(() => {
    let i = 0;
    const t = setInterval(() => { i += 1; setN(i); if (i >= end) clearInterval(t); }, 900 / end);
    return () => clearInterval(t);
  }, [end]);
  return <>{n}{suffix}</>;
}

function Stat({ label, icon, end, suffix }: { label: string; icon: ReactNode; end: number; suffix?: string }) {
  return (
    <div className="panel metric">
      <div className="metric-top">{label} {icon}</div>
      <div><div className="metric-val"><Counter end={end} suffix={suffix} /></div></div>
    </div>
  );
}

const STEPS: [string, string][] = [
  ['Discover', 'We talk about your goals, users and competitors. I write a short, clear brief.'],
  ['Wireframe', 'I plan the structure and user flow, so every screen has a reason to exist.'],
  ['Design', 'Pixel-perfect UI in Figma with a clickable prototype for your feedback.'],
  ['Handoff', 'Organized files, components and support so developers can build fast.'],
];

export function Portrait({ children }: { children?: ReactNode }) {
  const [hasPhoto, setHasPhoto] = useState(true);
  // Profile photo: uploaded in the dashboard (Hero section), otherwise public/images/maruf.jpg
  const photo = site('hero_photo', '/images/maruf.jpg');
  return (
    <div className="panel portrait">
      <span className="ph" aria-hidden="true">M.</span>
      {hasPhoto && <img className="photo" src={photo} alt="Sheikh Md Maruf" onError={() => setHasPhoto(false)} />}
      {children}
    </div>
  );
}

/** A hero button: "/work" opens a page, "#contact" scrolls, a full URL opens in a new tab. */
export function HeroButton({ href, className, children }: { href: string; className: string; children: ReactNode }) {
  if (href.startsWith('/')) return <Link to={href} className={className}>{children}</Link>;
  if (href.startsWith('http')) return <a href={href} className={className} target="_blank" rel="noopener">{children}</a>;
  return <a href={href} className={className}>{children}</a>;
}

/** A number box under the Home hero (texts from the dashboard). Counts up when the value is a whole number. */
function StatBox({ n, icon, label, value, suffix }: { n: number; icon: ReactNode; label: string; value: string; suffix: string }) {
  const l = site(`stat${n}_label`, label), v = site(`stat${n}_value`, value), s = site(`stat${n}_suffix`, suffix);
  const num = Number(v);
  return Number.isInteger(num) && num > 0
    ? <Stat label={l} icon={icon} end={num} suffix={s} />
    : <div className="panel metric"><div className="metric-top">{l} {icon}</div><div><div className="metric-val">{v}{s}</div></div></div>;
}

export default function HomePage() {
  useTitle(`${SITE_TITLE} — UI/UX & Brand Designer`);
  const [first, ...rest] = PROJECTS.slice(0, 3);
  const tools = (copy: string) => TOOLS.map(t => {
    const logo = toolLogo(t);
    return <span key={copy + t}>{logo ? <img className="track-logo" src={logo} alt="" loading="lazy" /> : <Sparkle />}{t}</span>;
  });

  return (
    <>
      {/* HERO — every text, link and the photo can be changed in the dashboard (Hero section) */}
      <section id="home">
        <div className="shell">
          <div className="hero-row">
            <div className="panel hero-copy">
              <a href="#work" className="announce"><b>{site('home_hero_badge', 'New')}</b><span>{site('home_hero_announce', `Latest: ${PROJECTS[0]?.t ?? 'new'} design`)}</span><ArrowRight /></a>
              <h1>{site('home_hero_title', 'I design digital experiences')} <span className="m">{site('home_hero_title_accent', 'that make complex things feel simple.')}</span></h1>
              <p className="hero-text">{site('home_hero_text', "I'm Maruf, a UI/UX and brand designer from Bangladesh. I design websites, mobile apps and dashboards that look sharp and help businesses grow.")}</p>
              <div className="actions">
                <HeroButton href={site('home_hero_btn1_link', '#contact')} className="btn">{site('home_hero_btn1_text', 'Start a project')} <span className="arr">→</span></HeroButton>
                <HeroButton href={site('home_hero_btn2_link', '#work')} className="btn btn-sec">{site('home_hero_btn2_text', 'View work')}</HeroButton>
              </div>
            </div>
            <Portrait><span className="pill"><span className="status"><i />{site('home_hero_location', 'Sirajganj, Bangladesh')}</span></span></Portrait>
          </div>
          <div className="stats">
            <StatBox n={1} icon={<RawIcon markup={METRIC_ICONS.clock} size={18} />} label="Experience" value="4" suffix="+ yrs" />
            <StatBox n={2} icon={<Grid size={18} strokeWidth={1.6} />} label="Portfolio" value="16" suffix="+" />
            <StatBox n={3} icon={<RawIcon markup={METRIC_ICONS.star} size={18} />} label="Services" value="4" suffix="" />
          </div>
          <div className="panel logos">
            <div className="logos-label">Tools I design with</div>
            <div className="ticker"><div className="track">{tools('a')}{tools('b')}</div></div>
          </div>
        </div>
      </section>

      {/* WORK — first 3 projects */}
      <section id="work">
        <SectionHead title="Selected work" text="Websites, apps and dashboards, each designed around one clear goal for the business."
          action={<Link to="/work" className="btn btn-sec">All work <span className="arr">→</span></Link>} />
        <Reveal className="shell">
          <div className="work-feature">
            <ProjectCard p={first} />
            <div className="stack">{rest.map(p => <ProjectCard key={p.t} p={p} />)}</div>
          </div>
        </Reveal>
      </section>

      {/* SERVICES */}
      <section id="services">
        <SectionHead title="Services" text="Four ways to work together. Clear scope, clear timeline, no surprises."
          action={<a href="#contact" className="btn btn-sec">Get a quote <span className="arr">→</span></a>} />
        <Reveal className="shell"><div className="grid-2">{SERVICES.map(s => <ServiceCard key={s.t} s={s} />)}</div></Reveal>
      </section>

      {/* PROCESS */}
      <section id="process">
        <SectionHead title="How we'll work" text="From first message to final files in four simple steps." />
        <Reveal className="shell">
          <div className="grid-4">
            {STEPS.map(([h, p], i) => <div className="panel step" key={h}><span className="n">0{i + 1}</span><h3>{h}</h3><p>{p}</p></div>)}
          </div>
        </Reveal>
      </section>

      <Reviews />

      {/* FROM THE BLOG — first 3 posts */}
      <section id="blog">
        <SectionHead title="From the blog" text="Notes on UI/UX, web design and freelancing from my client work."
          action={<Link to="/blog" className="btn btn-sec">Visit blog <span className="arr">→</span></Link>} />
        <Reveal className="shell"><div className="card-grid-3">{POSTS.slice(0, 3).map((b, i) => <BlogProductCard key={b.t} b={b} isNew={i === 0} />)}</div></Reveal>
      </section>

      {/* FAQ */}
      <section id="faq">
        <SectionHead title="Questions" text="Can't find your answer? Send me a message." />
        <Reveal className="shell"><FaqList /></Reveal>
      </section>
    </>
  );
}
