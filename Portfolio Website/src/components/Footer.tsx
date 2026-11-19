import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { CONTACT_EMAIL } from '../data/content';
import { ArrowUp, Dribbble, GitHub } from './Icons';
import Reveal from './Reveal';

/** CTA + contact form + footer bar — shown at the bottom of every page. */
export default function Footer() {
  const [sent, setSent] = useState(false);

  // Opens the visitor's email app with the form filled in.
  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    const get = (k: string) => String(f.get(k) ?? '');
    const subject = 'New project: ' + get('service');
    const body = `Name: ${get('name')}\nEmail: ${get('email')}\nService: ${get('service')}\n\n${get('message')}`;
    setSent(true);
    window.location.href = `mailto:${CONTACT_EMAIL}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
  };

  return (
    <footer className="container" id="contact">
      <Reveal className="shell">
        <div className="cta-row">
          <div className="panel cta-card">
            <div className="cta-body">
              <span className="status"><i />Available for new projects</span>
              <h2 className="cta-title">Let's work together. <span className="m">Your next project starts here.</span></h2>
              <p className="cta-text">Tell me about your project and I'll reply within one business day with ideas and a clear plan.</p>
              <div className="actions">
                <a href="#form" className="btn">Start a project <span className="arr">→</span></a>
                <a href="https://www.fiverr.com/sheikhmdmaruf01" target="_blank" rel="noopener" className="btn btn-sec">Hire on Fiverr</a>
              </div>
            </div>
            <div className="cta-strip"><span>Replies within 24 hours</span><span>Working worldwide · GMT+6</span></div>
          </div>
          <div className="panel cta-card">
            <form id="form" onSubmit={submit}>
              <h3>Project details</h3>
              <p className="sub">A few details help me give you a better answer.</p>
              <input name="name" placeholder="Your name" required />
              <input name="email" type="email" placeholder="Email" required />
              <select name="service" className="full">
                <option>Website design</option><option>Mobile app design</option><option>Dashboard &amp; SaaS UI</option>
                <option>Brand &amp; graphic design</option><option>Something else</option>
              </select>
              <textarea name="message" className="full" placeholder="Tell me about your project…" required />
              <button className="btn full" type="submit">Send message <span className="arr">→</span></button>
              <p className="fnote full" style={sent ? { display: 'block' } : undefined}>Opening your email app…</p>
            </form>
          </div>
        </div>
        <div className="footer-bar">
          <span>© {new Date().getFullYear()} Sheikh Md Maruf</span>
          <nav>
            <Link to="/work">Work</Link><Link to="/services">Services</Link><Link to="/#faq">FAQ</Link><a href="#contact">Contact</a>
          </nav>
          <div className="socials">
            <a className="icon-btn" href="https://dribbble.com/sheikhmdmaruf" target="_blank" rel="noopener" aria-label="Dribbble"><Dribbble /></a>
            <a className="icon-btn" href="https://github.com/Sheikhmdmaruf" target="_blank" rel="noopener" aria-label="GitHub"><GitHub /></a>
            <a className="icon-btn" href="#top" onClick={e => { e.preventDefault(); window.scrollTo({ top: 0, behavior: 'smooth' }); }} aria-label="Back to top"><ArrowUp /></a>
          </div>
        </div>
      </Reveal>
    </footer>
  );
}
