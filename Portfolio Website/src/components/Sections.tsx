import { useState, type CSSProperties, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { FAQ, REVIEWS } from '../data/content';
import { ArrowUpRight, Plus, Quote, Star } from './Icons';
import Reveal from './Reveal';

/** Section heading: title + text on the left, optional action on the right. */
export function SectionHead({ title, text, action, className = '' }: { title: string; text?: string; action?: ReactNode; className?: string }) {
  return (
    <Reveal className={`head ${className}`.trim()}>
      <div><h2>{title}</h2>{text && <p>{text}</p>}</div>
      {action}
    </Reveal>
  );
}

/** Small "All work ↗" style link used in More work / Related work / Keep reading. */
export const TextLink = ({ to, children }: { to: string; children: ReactNode }) => (
  <Link to={to} className="text-link">{children} <ArrowUpRight /></Link>
);

/** "Kind words" — sample reviews in an endless ticker. */
export function Reviews() {
  const cards = (copy: string) => REVIEWS.map((r, i) => (
    <div className="panel tcard" key={copy + i}>
      <div className="tpanel"><Quote /><p className="tquote">{r.q}</p></div>
      <div className="tstrip">
        <i style={{ '--a': r.a, '--b': r.b } as CSSProperties} />
        <div><div className="tname">{r.n}</div><div className="trole">{r.r}<span className="dot-sep" />{r.co}</div></div>
      </div>
    </div>
  ));
  const avatars = [['#dc9058', '#6b3416'], ['#7f7bff', '#3b2bd6'], ['#4ade80', '#0e9f6e'], ['#fbbf24', '#d97706']];
  return (
    // TODO: replace sample reviews with real client reviews (e.g. from Fiverr)
    <section id="reviews">
      <SectionHead title="Kind words" text="What clients say after working together." action={
        <div className="rating">
          <div className="ravatars">{avatars.map(([a, b]) => <i key={a} style={{ '--a': a, '--b': b } as CSSProperties} />)}</div>
          <div className="rtext"><b><Star />5.0</b>Sample reviews: real ones coming</div>
        </div>
      } />
      <Reveal className="shell tshell">
        <div className="ttrack">{cards('a')}{cards('b')}</div>
      </Reveal>
    </section>
  );
}

/** FAQ accordion — only one question open at a time. */
export function FaqList() {
  const [open, setOpen] = useState(0);
  return (
    <div className="faq">
      {FAQ.map(([pill, q, a], i) => (
        <details className="panel" key={q} open={open === i}
          onToggle={e => {
            const isOpen = (e.currentTarget as HTMLDetailsElement).open;
            if (isOpen) setOpen(i); else if (open === i) setOpen(-1);
          }}>
          <summary><span className="q"><span className="pill">{pill}</span>{q}</span><Plus /></summary>
          <p>{a}</p>
        </details>
      ))}
    </div>
  );
}
