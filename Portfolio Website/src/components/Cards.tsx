import { Link } from 'react-router-dom';
import type { Post, Project, Service } from '../data/types';
import { postHref, slug } from '../lib';
import Art from './Art';
import { ArrowUpRight, Pen, RawIcon } from './Icons';

/** Link that works for both internal routes and outside URLs. */
export function SmartLink({ href, className, children }: { href: string; className?: string; children: React.ReactNode }) {
  return href.startsWith('http')
    ? <a className={className} href={href} target="_blank" rel="noopener">{children}</a>
    : <Link className={className} to={href}>{children}</Link>;
}

export function ProjectCard({ p }: { p: Project }) {
  return (
    <Link className="panel pcard" data-type={p.type} to={`/project/${slug(p)}`}>
      <div className="pmedia">
        <Art type={p.type} c1={p.c1} c2={p.c2} img={p.img} alt={p.t} />
        <span className="pill"><b>✦</b>{p.hl}</span>
        {p.badge && <span className="badge">{p.badge}</span>}
      </div>
      <div className="pstrip">
        <div>
          <div className="ptitle">{p.t}<span className="dot-sep" /><span className="cat">{p.cat}</span></div>
          <div className="psum">{p.sum}</div>
        </div>
        <div className="pmeta">{p.y}<ArrowUpRight size={14} /></div>
      </div>
    </Link>
  );
}

export function ServiceCard({ s }: { s: Service }) {
  return (
    <Link className="panel scard" to={`/service/${slug(s)}`}>
      <div className="spanel">
        <div className="sicon"><RawIcon markup={s.icon} size={18} strokeWidth={1.7} /></div>
        <div className="stitle">{s.t}</div>
        <p className="ssum">{s.s}</p>
        <div className="stags">{s.tags.map(t => <span key={t}>{t}</span>)}</div>
      </div>
      <div className="sstrip">
        <span className="sprice">{s.price || 'Custom quote'}</span>
        <span className="v">View service <ArrowUpRight /></span>
      </div>
    </Link>
  );
}

export function PostCover({ b, className }: { b: Post; className: string }) {
  return b.img
    ? <img className={className} src={b.img} alt={b.t} loading="lazy" />
    : <Art type={b.type || 'web'} c1={b.c1} c2={b.c2} />;
}

export const PostMeta = ({ b }: { b: Post }) => (
  <><span className="tag-pill">{b.cat}</span><span>{b.date}</span><span>{b.read} read</span></>
);

/** Blog card used on the Blog page (grid + list layouts) and "Keep reading". */
export function ArticleCard({ b, withRow = false }: { b: Post; withRow?: boolean }) {
  return (
    <SmartLink className="panel article-card" href={postHref(b)}>
      <div className="article-card-media"><PostCover b={b} className="article-card-image" /></div>
      <div className="article-card-strip">
        <div className="article-card-meta"><PostMeta b={b} /></div>
        <h3 className="article-card-title">{b.t}</h3>
      </div>
      {withRow && (
        <div className="article-row">
          <div className="article-row-thumb"><PostCover b={b} className="article-card-image" /></div>
          <div className="article-row-body">
            <span className="article-row-title">{b.t}</span>
            <span className="article-row-category"><span className="tag-pill">{b.cat}</span></span>
            <span className="article-row-date">{b.date}</span>
            <span className="article-row-read-time">{b.read} read</span>
          </div>
          <ArrowUpRight size={16} className="article-row-arrow" />
        </div>
      )}
    </SmartLink>
  );
}

/** Store-style blog card on the home page. */
export function BlogProductCard({ b, isNew }: { b: Post; isNew: boolean }) {
  return (
    <SmartLink className="panel product-card" href={postHref(b)}>
      <div className="product-card-media">
        <PostCover b={b} className="product-card-image" />
        <div className="product-card-platform-slot"><span className="card-platform"><Pen size={13} />{b.cat}</span></div>
        {isNew && <div className="product-card-badge-slot"><span className="badge">New</span></div>}
      </div>
      <div className="product-card-strip">
        <div className="product-card-text">
          <div className="product-card-title">{b.t}</div>
          <div className="product-card-type">{b.date}</div>
        </div>
        <span className="product-card-price"><span className="mins">{b.read}</span><span className="view">View</span></span>
      </div>
    </SmartLink>
  );
}
