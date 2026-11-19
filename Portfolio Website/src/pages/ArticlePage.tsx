import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ARTICLES } from '../data/articles';
import { POSTS } from '../data/content';
import type { ArticleBlock } from '../data/types';
import { SITE_TITLE, slug, useTitle } from '../lib';
import { ArticleCard, PostCover, PostMeta } from '../components/Cards';
import { ArrowLeft, Send } from '../components/Icons';
import Reveal from '../components/Reveal';
import { SectionHead, TextLink } from '../components/Sections';

const AVATAR = 'https://cdn.dribbble.com/users/10909349/avatars/normal/786be272a45d098029b23504313c8718.jpg';

function Block({ x }: { x: ArticleBlock }) {
  if (typeof x === 'string') return <p className="rich-p">{x}</p>;
  if ('h2' in x) return <h2 className="rich-h2">{x.h2}</h2>;
  if ('h3' in x) return <h3 className="rich-h3">{x.h3}</h3>;
  if ('quote' in x) return <blockquote className="rich-quote">{x.quote}</blockquote>;
  return <ul className="rich-list">{x.list.map(i => <li className="rich-list-item" key={i}>{i}</li>)}</ul>;
}

export default function ArticlePage() {
  const { slug: id } = useParams();
  const b = POSTS.find(x => slug(x) === id) ?? POSTS[0];
  const a = ARTICLES[slug(b)] ?? {};
  const [avatarOk, setAvatarOk] = useState(true);
  useTitle(`${b.t} — ${SITE_TITLE}`);

  const body = a.body ?? ['The full article is coming soon.'];
  const i = POSTS.indexOf(b);
  const next = [1, 2, 3].map(k => POSTS[(i + k) % POSTS.length]).filter(x => x !== b);

  return (
    <>
      <section id="home">
        <div className="shell">
          <div className="detail-row-grid is-half">
            <div className="panel detail-copy is-top">
              <div className="detail-lead">
                <Link to="/blog" className="back-link"><ArrowLeft />Blog</Link>
                <div className="article-meta"><PostMeta b={b} /></div>
                <h1 className="detail-title">{b.t}</h1>
                {a.sum && <p className="detail-summary">{a.sum}</p>}
              </div>
            </div>
            <div className="article-cover"><PostCover b={b} className="article-cover-image" /></div>
          </div>
          <div className="panel article-panel"><div className="rich-text">{body.map((x, n) => <Block key={n} x={x} />)}</div></div>
          <div className="author-row">
            <div className="panel author-card">
              <div className="author-person">
                <div className="author-avatar">{avatarOk && <img className="author-avatar-image" src={AVATAR} alt="Sheikh Md Maruf" onError={() => setAvatarOk(false)} />}</div>
                <div className="author-copy"><p className="author-name">Sheikh Md Maruf</p><span className="author-role">UI/UX &amp; brand designer</span></div>
              </div>
              <p className="author-bio">I design websites, apps and dashboards, and write about UI/UX and freelancing from my client work.</p>
              <div className="author-actions">
                <a href="#contact" className="btn"><Send />Start a project</a>
                <Link to="/about" className="btn btn-sec">About me</Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section id="keep-reading">
        <SectionHead className="more-head" title="Keep reading" text="More notes on design and freelancing." action={<TextLink to="/blog">All articles</TextLink>} />
        <Reveal className="projects-grid is-three">{next.map(x => <ArticleCard key={x.t} b={x} />)}</Reveal>
      </section>
    </>
  );
}
