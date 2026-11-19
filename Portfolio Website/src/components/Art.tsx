import type { CSSProperties } from 'react';
import type { ProjectType } from '../data/types';

interface ArtProps { type: ProjectType; c1: string; c2: string; img?: string; alt?: string }

/** Project image, or a generated placeholder mock-up in the project's colours. */
export default function Art({ type, c1, c2, img, alt = '' }: ArtProps) {
  if (img) return <img src={img} alt={alt} loading="lazy" />;
  const row = <div className="row"><div /><div /><div /></div>;
  return (
    <div className={`art ${type}`} style={{ '--c1': c1, '--c2': c2 } as CSSProperties}>
      <div className="frame">
        {type === 'dash' ? (
          <><div className="side" /><div className="col"><div className="l s" />{row}<div className="blk" /></div></>
        ) : (
          <><div className="bar"><i /><i /><i /></div><div className="l s" /><div className="l m" /><div className="blk" />{type === 'web' && row}</>
        )}
      </div>
    </div>
  );
}

/** Image box with a cover image or the placeholder art. */
export function Media({ className, src, type, c1, c2, alt }: { className: string; src?: string } & Omit<ArtProps, 'img'>) {
  return (
    <div className={className}>
      {src ? <img className="gallery-image" src={src} alt={alt} loading="lazy" /> : <Art type={type} c1={c1} c2={c2} />}
    </div>
  );
}
