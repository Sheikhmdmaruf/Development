import { SERVICES } from '../data/content';
import { SITE_TITLE, useTitle } from '../lib';
import { ServiceCard } from '../components/Cards';
import { ArrowRight } from '../components/Icons';
import Reveal from '../components/Reveal';
import { Reviews } from '../components/Sections';

export default function ServicesPage() {
  useTitle(`Services — ${SITE_TITLE}`);
  return (
    <>
      <section id="home">
        <div className="shell">
          <div className="panel page-header-panel">
            <div className="page-header-lead">
              <a href="#contact" className="announce"><b>Now</b><span>Open for new projects this month</span><ArrowRight /></a>
              <h1 className="page-title">Four ways<span className="m">to work together.</span></h1>
              <p className="hero-text">Clear scope, honest timelines and one designer responsible from start to finish.</p>
            </div>
          </div>
        </div>
      </section>

      <section id="services">
        <Reveal className="shell"><div className="grid-2 services-page">{SERVICES.map(s => <ServiceCard key={s.t} s={s} />)}</div></Reveal>
      </section>

      <Reviews />
    </>
  );
}
