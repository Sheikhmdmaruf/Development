import { useParams } from 'react-router-dom';
import { CONTACT_EMAIL, PROJECTS, SERVICES } from '../data/content';
import { SERVICE_DETAILS, SERVICE_ICONS } from '../data/serviceDetails';
import { pad2, SITE_TITLE, slug, useTitle } from '../lib';
import Art from '../components/Art';
import { ProjectCard, ServiceCard } from '../components/Cards';
import { ArrowLeft, METRIC_ICONS, RawIcon, Send } from '../components/Icons';
import { Link } from 'react-router-dom';
import Reveal from '../components/Reveal';
import { SectionHead, TextLink } from '../components/Sections';

const iconFor = (name: string) => SERVICE_ICONS[name] ?? METRIC_ICONS[name] ?? METRIC_ICONS.star;

export default function ServicePage() {
  const { slug: id } = useParams();
  const s = SERVICES.find(x => slug(x) === id) ?? SERVICES[0];
  const d = SERVICE_DETAILS[slug(s)] ?? {};
  useTitle(`${s.t} — ${SITE_TITLE}`);

  const related = PROJECTS.filter(p => !d.type || p.type === d.type).slice(0, 2);
  const cover = related[0] ?? PROJECTS[0];
  const metrics: [string, string, string][] = [['Price', s.price || 'Custom quote', 'cart'], ...(d.metrics ?? [])];
  const included = d.included ?? [];
  const steps = d.steps ?? [];

  return (
    <>
      <section id="home">
        <div className="shell">
          <div className="hero-row is-half">
            <div className="panel detail-copy">
              <div className="detail-lead">
                <Link to="/services" className="back-link"><ArrowLeft />Services</Link>
                <h1 className="detail-title">{s.t}</h1>
                <p className="detail-summary">{d.summary || s.s}</p>
              </div>
              <div className="detail-actions">
                <a href="#contact" className="btn"><Send />Start a project</a>
                <a href={`mailto:${CONTACT_EMAIL}`} className="btn btn-sec">Email me</a>
              </div>
            </div>
            <div className="panel portrait"><Art type={cover.type} c1={cover.c1} c2={cover.c2} img={cover.img} alt={s.t} /></div>
          </div>
          <div className="detail-metrics is-four">
            {metrics.map(([label, value, icon]) => (
              <div className="panel metric" key={label}>
                <div className="metric-top">{label} <RawIcon markup={iconFor(icon)} size={18} /></div>
                <div><div className="metric-val">{value}</div></div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {included.length > 0 && (
        <section id="included">
          <SectionHead title="What’s included" text="Everything that comes with it, nothing hidden." />
          <Reveal className="projects-grid">
            {included.map(([icon, title, text]) => (
              <div className="sub-card" key={title}>
                <div className="sub-card-icon-box"><RawIcon markup={iconFor(icon)} size={20} /></div>
                <h3 className="sub-card-title">{title}</h3>
                <p className="sub-card-text">{text}</p>
              </div>
            ))}
          </Reveal>
        </section>
      )}

      {steps.length > 0 && (
        <section id="how">
          <SectionHead title="How it works" text="Four clear steps, so you always know what comes next." />
          <Reveal className="steps-grid">
            {steps.map(([title, text], n) => (
              <div className="process-step" key={title}>
                <span className="process-step-index">{pad2(n + 1)}</span>
                <h3 className="process-step-title">{title}</h3>
                <p className="process-step-text">{text}</p>
              </div>
            ))}
          </Reveal>
        </section>
      )}

      <section id="related">
        <SectionHead className="more-head" title="Related work" text="Projects where this service did the heavy lifting." action={<TextLink to="/work">All work</TextLink>} />
        <Reveal className="projects-grid">{related.map(p => <ProjectCard key={p.t} p={p} />)}</Reveal>
      </section>

      <section id="other-services">
        <SectionHead title="Other services" text="Browse the rest, one at a time." />
        <Reveal className="shell"><div className="grid-3 services-page">{SERVICES.filter(x => x !== s).map(x => <ServiceCard key={x.t} s={x} />)}</div></Reveal>
      </section>
    </>
  );
}
