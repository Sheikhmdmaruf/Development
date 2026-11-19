import { useEffect, useRef, useState, type ElementType, type ReactNode, type ComponentPropsWithoutRef } from 'react';

type Props<T extends ElementType> = { as?: T; className?: string; children?: ReactNode } & Omit<ComponentPropsWithoutRef<T>, 'as' | 'className' | 'children'>;

/** Fades content in when it scrolls into view (the `.rv` / `.rv.in` classes in styles.css). */
export default function Reveal<T extends ElementType = 'div'>({ as, className = '', children, ...rest }: Props<T>) {
  const Tag = (as || 'div') as ElementType;
  const ref = useRef<HTMLElement>(null);
  const [shown, setShown] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el || shown) return;
    const io = new IntersectionObserver(([e]) => {
      if (e.isIntersecting) { setShown(true); io.disconnect(); }
    }, { threshold: 0.08 });
    io.observe(el);
    return () => io.disconnect();
  }, [shown]);

  return <Tag ref={ref} className={`${className} rv${shown ? ' in' : ''}`.trim()} {...rest}>{children}</Tag>;
}
