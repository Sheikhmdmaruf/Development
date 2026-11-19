export type ProjectType = 'web' | 'app' | 'dash';

export interface Project {
  t: string;          // title
  type: ProjectType;
  cat: string;        // category label
  y: string;          // year
  hl: string;         // highlight pill
  sum: string;        // one-line summary
  url: string;        // Dribbble link
  c1: string;         // placeholder colour 1
  c2: string;         // placeholder colour 2
  badge?: string;
  img?: string;       // e.g. "/images/agency.jpg"
  cover?: string;
  slug?: string;
}

export interface Service {
  t: string;
  s: string;
  tags: string[];
  icon: string;       // SVG inner markup
  price?: string;
}

export interface Review {
  q: string; n: string; r: string; co: string; a: string; b: string;
}

/** [pill, question, answer] */
export type FaqItem = [string, string, string];

export interface Post {
  t: string;
  cat: string;
  date: string;
  read: string;
  c1: string;
  c2: string;
  type?: ProjectType;
  img?: string;
  url?: string;
}

export type MetricIcon = 'cart' | 'clock' | 'grid' | 'star' | (string & {});

export interface CaseStudy {
  client?: string;
  role?: string;
  duration?: string;
  tools?: string;
  team?: string;
  metrics?: { label: string; value: string; cap: string; icon: MetricIcon }[];
  steps?: { title: string; body: string[] }[];
  gallery?: string[];
}

export type ArticleBlock =
  | string
  | { h2: string }
  | { h3: string }
  | { quote: string }
  | { list: string[] };

export interface Article {
  sum?: string;
  body?: ArticleBlock[];
}

export interface ServiceDetail {
  type?: ProjectType;
  summary?: string;
  /** [label, value, icon] */
  metrics?: [string, string, string][];
  /** [icon, title, text] */
  included?: [string, string, string][];
  /** [title, text] */
  steps?: [string, string][];
}
