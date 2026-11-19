import { useEffect } from 'react';
import type { Post } from './data/types';

/** "Food Delivery App" -> "food-delivery-app" */
export const slug = (x: { t: string; slug?: string }): string =>
  x.slug || x.t.toLowerCase().replace(/&/g, 'and').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');

export const postHref = (b: Post): string => b.url || `/blog/${slug(b)}`;

export const pad2 = (n: number): string => String(n).padStart(2, '0');

/** Sets the browser tab title. */
export function useTitle(title: string): void {
  useEffect(() => { document.title = title; }, [title]);
}

export const SITE_TITLE = 'Sheikh Md Maruf';
