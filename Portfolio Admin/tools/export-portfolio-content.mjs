// Exports the portfolio's hardcoded content (src/data/*.ts) to JSON so the
// dashboard can import it on first run.
//
//   node tools/export-portfolio-content.mjs "<path to Portfolio Website>" [out.json]
//
// Needs Node 23+ (runs .ts files directly via type stripping).
import { writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const site = resolve(process.argv[2] ?? '../Portfolio Website');
const out = resolve(process.argv[3] ?? 'src/PortfolioAdmin.Web/Data/Seed/portfolio-content.json');
const load = (f) => import(pathToFileURL(resolve(site, 'src/data', f)).href);

const content = await load('content.ts');
const { CASES } = await load('cases.ts');
const { ARTICLES } = await load('articles.ts');
const { SERVICE_DETAILS } = await load('serviceDetails.ts');

const data = {
  contactEmail: content.CONTACT_EMAIL,
  tools: content.TOOLS,
  projects: content.PROJECTS,
  services: content.SERVICES,
  reviews: content.REVIEWS,
  faq: content.FAQ,
  posts: content.POSTS,
  cases: CASES,
  articles: ARTICLES,
  serviceDetails: SERVICE_DETAILS,
};

writeFileSync(out, JSON.stringify(data, null, 2));
console.log(`Wrote ${out}: ${data.projects.length} projects, ${data.services.length} services, ${data.posts.length} posts`);
