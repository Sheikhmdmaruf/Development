# Portfolio Website

Personal portfolio built with **React + TypeScript**, using **Vite** and **React Router**.

## Run it

You need [Node.js](https://nodejs.org) (LTS).

```bash
npm install      # first time only
npm run dev      # start at http://localhost:5173
npm run build    # type-check + production build into dist/
npm run preview  # preview the production build
```

## Pages

| Page | URL | File |
|------|-----|------|
| Home | `/` | `src/pages/HomePage.tsx` |
| About | `/about` | `src/pages/AboutPage.tsx` |
| Work | `/work` (`?type=web\|app\|dash`) | `src/pages/WorkPage.tsx` |
| Project case study | `/project/<slug>` | `src/pages/ProjectPage.tsx` |
| Services | `/services` | `src/pages/ServicesPage.tsx` |
| Service detail | `/service/<slug>` | `src/pages/ServicePage.tsx` |
| Blog | `/blog` | `src/pages/BlogPage.tsx` |
| Blog article | `/blog/<slug>` | `src/pages/ArticlePage.tsx` |

Shared parts (sidebar, footer + contact form, cards, FAQ, reviews) live in `src/components/`.

## Edit your content

All content is in `src/data/` — you rarely need to touch the pages:

- `content.ts` – email, projects, services, reviews, FAQ, tools, blog posts
- `cases.ts` – project case studies (Challenge / Process / Result, metrics, gallery)
- `articles.ts` – blog article text
- `serviceDetails.ts` – service detail pages
- `types.ts` – the TypeScript shapes for all of the above

Images go in `public/images/` and are used as `"/images/name.jpg"`.
