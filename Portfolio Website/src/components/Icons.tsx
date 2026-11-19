import type { ReactNode, SVGProps } from 'react';

type P = SVGProps<SVGSVGElement> & { size?: number };

const Svg = ({ size = 16, strokeWidth = 1.8, children, ...rest }: P & { children: ReactNode }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={strokeWidth} {...rest}>
    {children}
  </svg>
);

/** Renders trusted SVG inner markup that lives in the data files. */
export const RawIcon = ({ markup, ...rest }: P & { markup: string }) => (
  <svg width={rest.size ?? 16} height={rest.size ?? 16} viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth={rest.strokeWidth ?? 1.6} strokeLinecap="round" strokeLinejoin="round" className={rest.className}
    dangerouslySetInnerHTML={{ __html: markup }} />
);

export const METRIC_ICONS: Record<string, string> = {
  cart: '<path d="M3 4h2l2.4 11h11L21 7H6.2"/><circle cx="9" cy="19.5" r="1.3"/><circle cx="17" cy="19.5" r="1.3"/>',
  clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  grid: '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>',
  star: '<path d="M12 3l2.6 5.6L20 9.5l-4 4 1 5.8-5-2.8-5 2.8 1-5.8-4-4 5.4-.9z"/>',
};

export const ArrowUpRight = ({ size = 12, ...p }: P) => <Svg size={size} strokeWidth={2} {...p}><path d="M7 17 17 7M8 7h9v9" /></Svg>;
export const ArrowRight = ({ size = 12, ...p }: P) => <Svg size={size} strokeWidth={2} {...p}><path d="M5 12h14M13 6l6 6-6 6" /></Svg>;
export const ArrowLeft = ({ size = 14, ...p }: P) => <Svg size={size} strokeWidth={2} {...p}><path d="M19 12H5M11 6l-6 6 6 6" /></Svg>;
export const Send = ({ size = 15, ...p }: P) => <Svg size={size} {...p}><path d="M21 3 10 14M21 3l-7 18-4-7-7-4z" /></Svg>;
export const Plus = ({ size = 18, ...p }: P) => <Svg size={size} {...p}><path d="M12 5v14M5 12h14" /></Svg>;
export const Home = (p: P) => <Svg {...p}><path d="M3 10.5 12 3l9 7.5V21H3z" /></Svg>;
export const User = (p: P) => <Svg {...p}><circle cx="12" cy="8" r="4" /><path d="M4 21a8 8 0 0 1 16 0" /></Svg>;
export const Grid = ({ size = 16, ...p }: P) => (
  <Svg size={size} {...p}><rect x="3" y="3" width="7" height="7" rx="1.5" /><rect x="14" y="3" width="7" height="7" rx="1.5" /><rect x="3" y="14" width="7" height="7" rx="1.5" /><rect x="14" y="14" width="7" height="7" rx="1.5" /></Svg>
);
export const List = ({ size = 14, ...p }: P) => <Svg size={size} {...p}><path d="M8 6h13M8 12h13M8 18h13M3.5 6h.01M3.5 12h.01M3.5 18h.01" /></Svg>;
export const Services = (p: P) => <Svg {...p}><path d="M12 3v18M3 12h18" /><circle cx="12" cy="12" r="9" /></Svg>;
export const Pen = ({ size = 16, ...p }: P) => <Svg size={size} {...p}><path d="M4 20h4L19 9l-4-4L4 16z" /><path d="M13.5 6.5l4 4" /></Svg>;
export const Mail = (p: P) => <Svg {...p}><rect x="3" y="5" width="18" height="14" rx="2" /><path d="m3 7 9 6 9-6" /></Svg>;
export const Dribbble = (p: P) => <Svg {...p}><circle cx="12" cy="12" r="9" /><path d="M5 6.5c4 3.5 10 4 15.5 2.5M3.5 14c5-2 11-1.5 15 3.5M8.5 3.7c3.5 4.5 6 10.5 6.8 16.7" /></Svg>;
export const GitHub = (p: P) => (
  <Svg {...p}><path d="M9 19c-4 1.3-4-2-6-2.5M15 21v-3.5c0-1 .1-1.4-.5-2 2.8-.3 5.5-1.4 5.5-6a4.6 4.6 0 0 0-1.3-3.2 4.2 4.2 0 0 0-.1-3.2s-1.1-.3-3.5 1.3a12 12 0 0 0-6.2 0C6.5 2.8 5.4 3.1 5.4 3.1a4.2 4.2 0 0 0-.1 3.2A4.6 4.6 0 0 0 4 9.5c0 4.6 2.7 5.7 5.5 6-.6.6-.6 1.2-.5 2V21" /></Svg>
);
export const LinkedIn = (p: P) => <Svg {...p}><rect x="3" y="3" width="18" height="18" rx="3" /><path d="M8 10v7M8 7v.01M12 17v-4a2 2 0 0 1 4 0v4M12 10v7" /></Svg>;
export const Moon = (p: P) => <Svg size={15} {...p}><path d="M20 14.5A8 8 0 0 1 9.5 4 8 8 0 1 0 20 14.5z" /></Svg>;
export const Sun = (p: P) => <Svg size={15} {...p}><circle cx="12" cy="12" r="4" /><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" /></Svg>;
export const Search = (p: P) => <Svg {...p}><circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" /></Svg>;
export const Menu = (p: P) => <Svg size={18} strokeLinecap="round" {...p}><path d="M4 7h16M4 12h16M4 17h16" /></Svg>;
export const ArrowUp = (p: P) => <Svg {...p}><path d="M12 19V5M6 11l6-6 6 6" /></Svg>;
export const Sparkle = ({ size = 14 }: P) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="var(--brand)"><path d="M12 2l2 8 8 2-8 2-2 8-2-8-8-2 8-2z" /></svg>
);
export const Star = ({ size = 14 }: P) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="var(--brand)"><path d="M12 2.5l2.9 6 6.6.9-4.8 4.6 1.2 6.5L12 17.4l-5.9 3.1 1.2-6.5L2.5 9.4l6.6-.9z" /></svg>
);
export const Quote = () => (
  <svg width="22" height="22" viewBox="0 0 24 24" fill="var(--brand)"><path d="M4 18v-5.5C4 8.4 6 6 9.5 5.5l.5 1.8C8 8 7.2 9.3 7.2 11H10v7zm9 0v-5.5c0-4.1 2-6.5 5.5-7l.5 1.8c-2 .7-2.8 2-2.8 3.7H19v7z" /></svg>
);
