import { useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { POSTS, PROJECTS, SERVICES, TOOLS } from '../data/content';
import { SITE_TITLE, useTitle } from '../lib';
import { BlogProductCard, ProjectCard, ServiceCard } from '../components/Cards';
import { ArrowRight, Grid, RawIcon, METRIC_ICONS, Sparkle } from '../components/Icons';
import Reveal from '../components/Reveal';
import { toolLogo } from '../data/live';
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
  return (
    <div className="panel portrait">
      <span className="ph" aria-hidden="true">M.</span>
      {/* Profile photo: public/images/maruf.jpg */}
      {hasPhoto && <img className="photo" src="/images/maruf.jpg" alt="Sheikh Md Maruf" onError={() => setHasPhoto(false)} />}
      {children}
    </div>
  );
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
      {/* HERO */}
      <section id="home">
        <div className="shell">
          <div className="hero-row">
            <div className="panel hero-copy">
              <a href="#work" className="announce"><b>New</b><span>Latest: {PROJECTS[0].t} design</span><ArrowRight /></a>
              <h1>I design digital experiences <span className="m">that make complex things feel simple.</span></h1>
              <p className="hero-text">I'm Maruf, a UI/UX and brand designer from Bangladesh. I design websites, mobile apps and dashboards that look sharp and help businesses grow.</p>
              <div className="actions">
                <a href="#contact" className="btn">Start a project <span className="arr">→</span></a>
                <a href="#work" className="btn btn-sec">View work</a>
              </div>
            </div>
            <Portrait><span className="pill"><span className="status"><i />Sirajganj, Bangladesh</span></span></Portrait>
          </div>
          <div className="stats">
            <Stat label="Experience" icon={<RawIcon markup={METRIC_ICONS.clock} size={18} />} end={4} suffix="+ yrs" />
            <Stat label="Portfolio" icon={<Grid size={18} strokeWidth={1.6} />} end={16} suffix="+" />
            <Stat label="Services" icon={<RawIcon markup={METRIC_ICONS.star} size={18} />} end={4} />
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
