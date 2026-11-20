/* ======================================================
   LIVE CONTENT FROM THE ADMIN DASHBOARD
   When VITE_ADMIN_API_URL and VITE_SITE_KEY are set (see .env.example),
   the site loads projects, services, posts, reviews, FAQ and settings
   from the dashboard before the first render, and the contact form
   sends inquiries to the dashboard.
   Without them — or if the dashboard can't be reached — the site
   uses the content in this folder, so it never breaks.
   ====================================================== */
import { POSTS, PROJECTS, REVIEWS, SERVICES, FAQ, TOOLS, setContactEmail, CONTACT_EMAIL } from './content';
import { CASES } from './cases';
import { ARTICLES } from './articles';
import { SERVICE_DETAILS } from './serviceDetails';
import type { Article, CaseStudy, FaqItem, Post, Project, Review, Service, ServiceDetail } from './types';

const API = (import.meta.env.VITE_ADMIN_API_URL as string | undefined)?.replace(/\/$/, '');
const KEY = import.meta.env.VITE_SITE_KEY as string | undefined;

export const isConnected = Boolean(API && KEY);

interface LivePayload {
  settings: Record<string, string>;
  contactEmail?: string;
  tools: string[];
  projects: Project[];
  services: Service[];
  reviews: Review[];
  faq: FaqItem[];
  posts: Post[];
  cases: Record<string, CaseStudy>;
  articles: Record<string, Article>;
  serviceDetails: Record<string, ServiceDetail>;
}

const settings: Record<string, string> = {};

/** A text or link managed under Site Settings in the dashboard, with the built-in value as fallback. */
export const site = (key: string, fallback: string): string => settings[key]?.trim() || fallback;

function replaceArray<T>(target: T[], next: T[] | undefined) {
  if (Array.isArray(next)) target.splice(0, target.length, ...next);
}
function replaceRecord<T>(target: Record<string, T>, next: Record<string, T> | undefined) {
  if (!next) return;
  for (const k of Object.keys(target)) delete target[k];
  Object.assign(target, next);
}

/** Loads published content. Resolves either way; on failure the built-in content stays. */
export async function loadLiveContent(timeoutMs = 3500): Promise<void> {
  if (!isConnected) return;
  const ctrl = new AbortController();
  const timer = setTimeout(() => ctrl.abort(), timeoutMs);
  try {
    const res = await fetch(`${API}/api/v1/content?key=${encodeURIComponent(KEY!)}`, { signal: ctrl.signal });
    if (!res.ok) throw new Error(`Content API ${res.status}`);
    const data = (await res.json()) as LivePayload;
    replaceArray(PROJECTS, data.projects);
    replaceArray(SERVICES, data.services);
    replaceArray(REVIEWS, data.reviews);
    replaceArray(FAQ, data.faq);
    replaceArray(POSTS, data.posts);
    replaceArray(TOOLS, data.tools);
    replaceRecord(CASES, data.cases);
    replaceRecord(ARTICLES, data.articles);
    replaceRecord(SERVICE_DETAILS, data.serviceDetails);
    Object.assign(settings, data.settings ?? {});
    if (data.contactEmail) setContactEmail(data.contactEmail);
  } catch (err) {
    console.warn('[portfolio] Using built-in content:', err);
  } finally {
    clearTimeout(timer);
  }
}

export interface ContactForm {
  name: string;
  email: string;
  service?: string;
  message: string;
  gotcha?: string;
}

/**
 * Sends the contact form to the dashboard. Returns "sent" or "error".
 * When the dashboard isn't configured it opens the visitor's email app instead ("mailto").
 */
export async function submitContact(form: ContactForm): Promise<'sent' | 'error' | 'mailto'> {
  if (!isConnected) {
    const subject = 'New project: ' + (form.service ?? '');
    const body = `Name: ${form.name}\nEmail: ${form.email}\nService: ${form.service ?? ''}\n\n${form.message}`;
    window.location.href = `mailto:${CONTACT_EMAIL}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
    return 'mailto';
  }
  try {
    const res = await fetch(`${API}/api/v1/submissions`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Site-Key': KEY! },
      body: JSON.stringify({
        name: form.name, email: form.email, inquiryType: form.service, subject: form.service ? `New project: ${form.service}` : undefined,
        message: form.message, gotcha: form.gotcha, pageUrl: window.location.href,
      }),
    });
    return res.ok ? 'sent' : 'error';
  } catch {
    return 'error';
  }
}
