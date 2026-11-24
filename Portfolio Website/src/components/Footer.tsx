import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { site, submitContact } from '../data/live';
import { ArrowUp, SocialIcon } from './Icons';
import { SOCIALS } from '../data/content';
import Reveal from './Reveal';

type FormState = 'idle' | 'sending' | 'sent' | 'error' | 'mailto';

const NOTES: Record<Exclude<FormState, 'idle'>, string> = {
  sending: 'Sending…',
  sent: "Thanks! Your message was sent. I'll reply within one business day.",
  error: 'Sorry, something went wrong. Please try again or email me directly.',
  mailto: 'Opening your email app…',
};

/** CTA + contact form + footer bar — shown at the bottom of every page. */
export default function Footer() {
  const [state, setState] = useState<FormState>('idle');

  // Sends the inquiry to the admin dashboard (or opens the email app when it isn't connected).
  const submit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formEl = e.currentTarget;
    const f = new FormData(formEl);
    const get = (k: string) => String(f.get(k) ?? '');
    setState('sending');
    const result = await submitContact({ name: get('name'), email: get('email'), service: get('service'), message: get('message'), gotcha: get('gotcha') });
    setState(result);
    if (result === 'sent') formEl.reset();
  };

  return (
    <footer className="container" id="contact">
      <Reveal className="shell">
        <div className="cta-row">
          <div className="panel cta-card">
            <div className="cta-body">
              <span className="status"><i />{site('availability', 'Available for new projects')}</span>
              <h2 className="cta-title">Let's work together. <span className="m">Your next project starts here.</span></h2>
              <p className="cta-text">Tell me about your project and I'll reply within one business day with ideas and a clear plan.</p>
              <div className="actions">
                <a href="#form" className="btn">Start a project <span className="arr">→</span></a>
                <a href={site('fiverr_url', 'https://www.fiverr.com/sheikhmdmaruf01')} target="_blank" rel="noopener" className="btn btn-sec">Hire on Fiverr</a>
              </div>
            </div>
            <div className="cta-strip"><span>{site('reply_time', 'Replies within 24 hours')}</span><span>{site('location_note', 'Working worldwide · GMT+6')}</span></div>
          </div>
          <div className="panel cta-card">
            <form id="form" onSubmit={submit}>
              <h3>Project details</h3>
              <p className="sub">A few details help me give you a better answer.</p>
              <input name="name" placeholder="Your name" required maxLength={150} />
              <input name="email" type="email" placeholder="Email" required maxLength={256} />
              <select name="service" className="full">
                <option>Website design</option><option>Mobile app design</option><option>Dashboard &amp; SaaS UI</option>
                <option>Brand &amp; graphic design</option><option>Something else</option>
              </select>
              <textarea name="message" className="full" placeholder="Tell me about your project…" required maxLength={10000} />
              {/* Spam trap: hidden from people, bots fill it in. */}
              <input name="gotcha" tabIndex={-1} autoComplete="off" aria-hidden="true" style={{ position: 'absolute', left: '-9999px', width: 1, height: 1 }} />
              <button className="btn full" type="submit" disabled={state === 'sending'}>
                {state === 'sending' ? 'Sending…' : 'Send message'} <span className="arr">→</span>
              </button>
              <p className="fnote full" role="status" style={state !== 'idle' ? { display: 'block' } : undefined}>
                {state !== 'idle' ? NOTES[state] : ''}
              </p>
            </form>
          </div>
        </div>
        <div className="footer-bar">
          <span>© {new Date().getFullYear()} Sheikh Md Maruf</span>
          <nav>
            <Link to="/work">Work</Link><Link to="/services">Services</Link><Link to="/#faq">FAQ</Link><a href="#contact">Contact</a>
          </nav>
          <div className="socials">
            {/* Footer icons: dashboard → Social links */}
            {SOCIALS.filter(s => s.footer).map((s, i) => (
              <a key={i + s.name} className="icon-btn" href={s.url} target="_blank" rel="noopener" aria-label={s.name} title={s.name}><SocialIcon icon={s.icon} img={s.img} /></a>
            ))}
            <a className="icon-btn" href="#top" onClick={e => { e.preventDefault(); window.scrollTo({ top: 0, behavior: 'smooth' }); }} aria-label="Back to top"><ArrowUp /></a>
          </div>
        </div>
      </Reveal>
    </footer>
  );
}
