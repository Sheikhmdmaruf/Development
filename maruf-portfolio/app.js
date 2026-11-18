/* Shared logic for index.html and work.html — content lives in data.js */
const $ = id => document.getElementById(id);
const PAGE = document.body.dataset.page || 'home';
const arrowUR = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M7 17 17 7M8 7h9v9"/></svg>';

function art(p){
  if (p.img) return `<img src="${p.img}" alt="${p.t}" loading="lazy">`;
  const inner = p.type === 'dash'
    ? `<div class="side"></div><div class="col"><div class="l s"></div><div class="row"><div></div><div></div><div></div></div><div class="blk"></div></div>`
    : `<div class="bar"><i></i><i></i><i></i></div><div class="l s"></div><div class="l m"></div><div class="blk"></div>${p.type==='web'?'<div class="row"><div></div><div></div><div></div></div>':''}`;
  return `<div class="art ${p.type}" style="--c1:${p.c1};--c2:${p.c2}"><div class="frame">${inner}</div></div>`;
}
const slug = p => p.slug || p.t.toLowerCase().replace(/&/g, 'and').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
function card(p){
  return `<a class="panel pcard" data-type="${p.type}" href="project.html?p=${slug(p)}">
    <div class="pmedia">${art(p)}<span class="pill"><b>✦</b>${p.hl}</span>${p.badge?`<span class="badge">${p.badge}</span>`:''}</div>
    <div class="pstrip"><div><div class="ptitle">${p.t}<span class="dot-sep"></span><span class="cat">${p.cat}</span></div><div class="psum">${p.sum}</div></div>
    <div class="pmeta">${p.y}${arrowUR}</div></div></a>`;
}

/* Home: first 3 projects (big left + 2 stacked right) */
if ($('featured')){
  const f = PROJECTS.slice(0, 3);
  $('featured').innerHTML = card(f[0]) + `<div class="stack">${f.slice(1).map(card).join('')}</div>`;
}

/* Work page: all projects, filter tabs, grid/list toggle (link with ?type=app to pre-filter) */
if ($('allWork')){
  const grid = $('allWork');
  const render = type => {
    const list = PROJECTS.filter(p => type === 'all' || p.type === type);
    grid.innerHTML = list.map(card).join('') || '<div class="panel empty">No projects yet.</div>';
  };
  const tabs = (bar, attr, value) => bar.querySelectorAll('button').forEach(b => b.classList.toggle('is-active', b.dataset[attr] === value));

  const start = new URLSearchParams(location.search).get('type') || 'all';
  tabs($('filters'), 'f', start);
  render(start);
  $('filters').onclick = e => {
    const b = e.target.closest('button'); if (!b) return;
    tabs($('filters'), 'f', b.dataset.f);
    render(b.dataset.f);
  };

  const setView = v => {
    grid.classList.toggle('is-list', v === 'list');
    tabs($('viewToggle'), 'view', v);
    try { localStorage.setItem('workView', v); } catch (e) {}
  };
  let savedView; try { savedView = localStorage.getItem('workView'); } catch (e) {}
  setView(new URLSearchParams(location.search).get('view') || savedView || 'grid');
  $('viewToggle').onclick = e => { const b = e.target.closest('button'); if (b) setView(b.dataset.view); };

  /* "New" announcement points to the first (latest) project */
  const latest = PROJECTS[0];
  $('latest').href = latest.url;
  $('latest').querySelector('span').textContent = latest.t;
}

/* Project detail page: project.html?p=<slug> */
const ICONS = {
  cart:'<path d="M3 4h2l2.4 11h11L21 7H6.2"/><circle cx="9" cy="19.5" r="1.3"/><circle cx="17" cy="19.5" r="1.3"/>',
  clock:'<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  grid:'<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>',
  star:'<path d="M12 3l2.6 5.6L20 9.5l-4 4 1 5.8-5-2.8-5 2.8 1-5.8-4-4 5.4-.9z"/>'
};
const esc = s => String(s).replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));

/* image or generated placeholder for cover / gallery */
const media = (p, src, cls) => `<div class="${cls}">${src
  ? `<img class="gallery-image" src="${src}" alt="${esc(p.t)}" loading="lazy">`
  : art(p)}</div>`;

if ($('detail')){
  const id = new URLSearchParams(location.search).get('p');
  const p = PROJECTS.find(x => slug(x) === id) || PROJECTS[0];
  const c = (typeof CASES !== 'undefined' && CASES[slug(p)]) || {};
  document.title = `${p.t} — Sheikh Md Maruf`;

  // details panel (same rows component as the reference)
  const rows = [
    ['Client', c.client], ['Year', p.y], ['Role', c.role], ['Duration', c.duration],
    ['Tools', c.tools], ['Team', c.team], ['Category', p.cat]
  ].filter(r => r[1]);

  // 3 metric cards
  const metrics = c.metrics || [
    {label:'Category', value:p.cat, cap:p.hl, icon:'grid'},
    {label:'Year', value:p.y, cap:'project year', icon:'clock'},
    {label:'Platform', value:p.type === 'app' ? 'Mobile' : 'Web', cap:p.type === 'app' ? 'iOS & Android' : 'desktop & mobile', icon:'star'}
  ];

  // numbered story panels (text only)
  const steps = c.steps || [
    {title:'Overview', body:[p.sum + ' The full case study is coming soon — you can see all the screens on Dribbble.']}
  ];

  // gallery at the end: 1 wide + 2 half
  const g = c.gallery || [];

  $('detail').innerHTML = `
    <div class="detail-row-grid">
      <div class="panel detail-copy">
        <div class="detail-lead">
          <a href="work.html" class="back-link"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5M11 6l-6 6 6 6"/></svg>Work</a>
          <h1 class="detail-title">${esc(p.t)}</h1>
          <p class="detail-summary">${esc(p.sum)}</p>
        </div>
        <div class="detail-actions"><a href="#contact" class="btn">Start a similar project</a></div>
      </div>
      <div class="panel detail-panel">
        ${rows.map(r => `<div class="detail-row"><span class="detail-row-label">${r[0]}</span><span class="detail-row-value">${esc(r[1])}</span></div>`).join('')}
        <div class="detail-row"><span class="detail-row-label">Services</span><span class="inline-links"><a class="inline-link" href="index.html#services">${esc(p.cat)} Design</a><a class="inline-link" href="${p.url}" target="_blank" rel="noopener">Dribbble</a></span></div>
      </div>
    </div>
    <div class="detail-metrics">${metrics.map(m => `
      <div class="panel metric">
        <div class="metric-top">${esc(m.label)} <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">${ICONS[m.icon] || ICONS.star}</svg></div>
        <div><div class="metric-val">${esc(m.value)}</div><div class="metric-cap">${esc(m.cap)}</div></div>
      </div>`).join('')}</div>
    ${media(p, p.cover || p.img, 'detail-cover')}
    <div class="story-steps">${steps.map((s, i) => `
      <div class="story-step">
        <div class="story-step-label"><span class="story-step-index">${String(i + 1).padStart(2, '0')}</span><h2 class="story-step-title">${esc(s.title)}</h2></div>
        <div class="story-step-body">${(s.body || []).map(t => `<p class="rich-p">${esc(t)}</p>`).join('')}</div>
      </div>`).join('')}</div>
    <div class="gallery">
      ${media(p, g[0], 'gallery-media')}
      <div class="gallery-row">${media(p, g[1], 'gallery-media is-half')}${media(p, g[2], 'gallery-media is-half')}</div>
    </div>`;

  // More work: next 2 projects
  const start = PROJECTS.indexOf(p);
  $('moreWork').innerHTML = [1, 2].map(k => PROJECTS[(start + k) % PROJECTS.length]).filter(x => x !== p).map(card).join('');
}

function serviceCard(s){
  return `
  <a class="panel scard" href="service.html?s=${slug(s)}">
    <div class="spanel">
      <div class="sicon"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">${s.icon}</svg></div>
      <div class="stitle">${s.t}</div><p class="ssum">${s.s}</p>
      <div class="stags">${s.tags.map(t => `<span>${t}</span>`).join('')}</div>
    </div>
    <div class="sstrip"><span class="sprice">${s.price || "Custom quote"}</span><span class="v">View service <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M7 17 17 7M8 7h9v9"/></svg></span></div>
  </a>`;
}
if ($('services-grid')) $('services-grid').innerHTML = SERVICES.map(serviceCard).join('');

if ($('tTrack')){
  const tc = REVIEWS.map(r => `
  <div class="panel tcard"><div class="tpanel">
    <svg width="22" height="22" viewBox="0 0 24 24" fill="var(--brand)"><path d="M4 18v-5.5C4 8.4 6 6 9.5 5.5l.5 1.8C8 8 7.2 9.3 7.2 11H10v7zm9 0v-5.5c0-4.1 2-6.5 5.5-7l.5 1.8c-2 .7-2.8 2-2.8 3.7H19v7z"/></svg>
    <p class="tquote">${r.q}</p></div>
    <div class="tstrip"><i style="--a:${r.a};--b:${r.b}"></i><div><div class="tname">${r.n}</div><div class="trole">${r.r}<span class="dot-sep"></span>${r.co}</div></div></div>
  </div>`).join('');
  $('tTrack').innerHTML = tc + tc;
}

if ($('faqList')) $('faqList').innerHTML = FAQ.map((f, i) => `
  <details class="panel"${i === 0 ? ' open' : ''}><summary><span class="q"><span class="pill">${f[0]}</span>${f[1]}</span>
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 5v14M5 12h14"/></svg></summary><p>${f[2]}</p></details>`).join('');

if ($('tools')){
  const t = TOOLS.map(t => `<span><svg width="14" height="14" viewBox="0 0 24 24" fill="var(--brand)"><path d="M12 2l2 8 8 2-8 2-2 8-2-8-8-2 8-2z"/></svg>${t}</span>`).join('');
  $('tools').innerHTML = t + t;
}

/* theme */
function setTheme(t){
  document.documentElement.dataset.theme = t;
  $('tDark').classList.toggle('on', t === 'dark');
  $('tLight').classList.toggle('on', t === 'light');
  try { localStorage.setItem('theme', t); } catch (e) {}
}
let savedTheme; try { savedTheme = localStorage.getItem('theme'); } catch (e) {}
setTheme(savedTheme || 'dark');

/* sidebar: on other pages, home section links point back to index.html */
const nav = $('nav');
if (PAGE !== 'home') document.querySelectorAll('#nav a[href^="#"], .footer-bar nav a[href^="#"]').forEach(a => {
  if (a.getAttribute('href') !== '#contact') a.setAttribute('href', 'index.html' + a.getAttribute('href'));
});
nav.querySelectorAll('a').forEach(a => a.addEventListener('click', () => nav.classList.remove('open')));

/* active nav item */
const navLinks = [...document.querySelectorAll('#mainNav .nav-item')];
const setActive = fn => navLinks.forEach(l => l.classList.toggle('active', fn(l)));
if (PAGE === 'work' || PAGE === 'project') setActive(l => l.dataset.page === 'work');
else if (['about','blog','article','services','service'].includes(PAGE)) setActive(l => l.dataset.page === ({article:'blog', service:'services'}[PAGE] || PAGE));
else {
  const spy = new IntersectionObserver(es => es.forEach(e => {
    if (e.isIntersecting) setActive(l => l.getAttribute('href') === '#' + e.target.id);
  }), { rootMargin: '-40% 0px -55% 0px' });
  document.querySelectorAll('section[id]').forEach(s => spy.observe(s));
}

/* reveal on scroll */
const io = new IntersectionObserver(es => es.forEach(e => {
  if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); }
}), { threshold: .08 });
document.querySelectorAll('.rv').forEach(el => io.observe(el));

/* counters */
document.querySelectorAll('[data-count]').forEach(n => {
  const end = +n.dataset.count, suf = n.dataset.suf; let i = 0;
  const t = setInterval(() => { n.textContent = (++i) + suf; if (i >= end) clearInterval(t); }, 900 / end);
});

/* contact form -> opens the visitor's email app */
$('form').onsubmit = e => {
  e.preventDefault();
  const f = new FormData(e.target);
  const subject = 'New project: ' + f.get('service');
  const body = 'Name: ' + f.get('name') + '\nEmail: ' + f.get('email') + '\nService: ' + f.get('service') + '\n\n' + f.get('message');
  $('fnote').style.display = 'block';
  location.href = 'mailto:' + CONTACT_EMAIL + '?subject=' + encodeURIComponent(subject) + '&body=' + encodeURIComponent(body);
};
$('yr').textContent = new Date().getFullYear();

/* ================= BLOG ================= */
const postArt = b => art({type: b.type || 'web', c1: b.c1, c2: b.c2});
const postHref = b => b.url || 'article.html?p=' + slug(b);
const postCover = (b, cls) => b.img ? `<img class="${cls}" src="${b.img}" alt="${esc(b.t)}" loading="lazy">` : postArt(b);

/* Home: "From the blog" — store-style cards */
if ($('blogHome') && typeof POSTS !== 'undefined'){
  $('blogHome').innerHTML = POSTS.slice(0, 3).map((b, i) => `
    <a class="panel product-card" href="${postHref(b)}">
      <div class="product-card-media">${postCover(b, 'product-card-image')}
        <div class="product-card-platform-slot"><span class="card-platform"><svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 20h4L19 9l-4-4L4 16z"/></svg>${esc(b.cat)}</span></div>
        ${i === 0 ? '<div class="product-card-badge-slot"><span class="badge">New</span></div>' : ''}
      </div>
      <div class="product-card-strip">
        <div class="product-card-text">
          <div class="product-card-title">${esc(b.t)}</div>
          <div class="product-card-type">${esc(b.date)}</div>
        </div>
        <span class="product-card-price"><span class="mins">${esc(b.read)}</span><span class="view">View</span></span>
      </div>
    </a>`).join('');
}

/* Blog page: filters + grid/list */
if ($('blogGrid') && typeof POSTS !== 'undefined'){
  const grid = $('blogGrid');
  const meta = b => `<span class="tag-pill">${esc(b.cat)}</span><span>${esc(b.date)}</span><span>${esc(b.read)} read</span>`;
  const render = cat => {
    const list = POSTS.filter(b => cat === 'all' || b.cat === cat);
    grid.innerHTML = list.map(b => `
      <a class="panel article-card" href="${postHref(b)}">
        <div class="article-card-media">${postCover(b, 'article-card-image')}</div>
        <div class="article-card-strip"><div class="article-card-meta">${meta(b)}</div><h3 class="article-card-title">${esc(b.t)}</h3></div>
        <div class="article-row">
          <div class="article-row-thumb">${postCover(b, 'article-card-image')}</div>
          <div class="article-row-body">
            <span class="article-row-title">${esc(b.t)}</span>
            <span class="article-row-category"><span class="tag-pill">${esc(b.cat)}</span></span>
            <span class="article-row-date">${esc(b.date)}</span>
            <span class="article-row-read-time">${esc(b.read)} read</span>
          </div>
          <svg class="article-row-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M7 17 17 7M8 7h9v9"/></svg>
        </div>
      </a>`).join('') || '<div class="panel empty">No posts yet.</div>';
  };
  const tabs = (bar, attr, value) => bar.querySelectorAll('button').forEach(b => b.classList.toggle('is-active', b.dataset[attr] === value));
  render('all');
  $('blogFilters').onclick = e => {
    const b = e.target.closest('button'); if (!b) return;
    tabs($('blogFilters'), 'f', b.dataset.f); render(b.dataset.f);
  };
  const setView = v => {
    grid.classList.toggle('is-list', v === 'list');
    tabs($('blogView'), 'view', v);
    try { localStorage.setItem('blogView', v); } catch (e) {}
  };
  let sv; try { sv = localStorage.getItem('blogView'); } catch (e) {}
  setView(new URLSearchParams(location.search).get('view') || sv || 'grid');
  $('blogView').onclick = e => { const b = e.target.closest('button'); if (b) setView(b.dataset.view); };
}

/* ================= ARTICLE PAGE ================= */
const articleCard = b => `
  <a class="panel article-card" href="${postHref(b)}">
    <div class="article-card-media">${postCover(b, 'article-card-image')}</div>
    <div class="article-card-strip"><div class="article-card-meta"><span class="tag-pill">${esc(b.cat)}</span><span>${esc(b.date)}</span><span>${esc(b.read)} read</span></div><h3 class="article-card-title">${esc(b.t)}</h3></div>
  </a>`;

if ($('article') && typeof POSTS !== 'undefined'){
  const id = new URLSearchParams(location.search).get('p');
  const b = POSTS.find(x => slug(x) === id) || POSTS[0];
  const a = (typeof ARTICLES !== 'undefined' && ARTICLES[slug(b)]) || {};
  document.title = `${b.t} — Sheikh Md Maruf`;
  const body = a.body || ['The full article is coming soon.'];

  const block = x => {
    if (typeof x === 'string') return `<p class="rich-p">${esc(x)}</p>`;
    if (x.h2) return `<h2 class="rich-h2">${esc(x.h2)}</h2>`;
    if (x.h3) return `<h3 class="rich-h3">${esc(x.h3)}</h3>`;
    if (x.quote) return `<blockquote class="rich-quote">${esc(x.quote)}</blockquote>`;
    if (x.list) return `<ul class="rich-list">${x.list.map(i => `<li class="rich-list-item">${esc(i)}</li>`).join('')}</ul>`;
    return '';
  };

  $('article').innerHTML = `
    <div class="detail-row-grid is-half">
      <div class="panel detail-copy is-top">
        <div class="detail-lead">
          <a href="blog.html" class="back-link"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5M11 6l-6 6 6 6"/></svg>Blog</a>
          <div class="article-meta"><span class="tag-pill">${esc(b.cat)}</span><span>${esc(b.date)}</span><span>${esc(b.read)} read</span></div>
          <h1 class="detail-title">${esc(b.t)}</h1>
          ${a.sum ? `<p class="detail-summary">${esc(a.sum)}</p>` : ''}
        </div>
      </div>
      <div class="article-cover">${postCover(b, 'article-cover-image')}</div>
    </div>
    <div class="panel article-panel"><div class="rich-text">${body.map(block).join('')}</div></div>
    <div class="author-row">
      <div class="panel author-card">
        <div class="author-person">
          <div class="author-avatar"><img class="author-avatar-image" src="https://cdn.dribbble.com/users/10909349/avatars/normal/786be272a45d098029b23504313c8718.jpg" alt="Sheikh Md Maruf" onerror="this.remove()"></div>
          <div class="author-copy"><p class="author-name">Sheikh Md Maruf</p><span class="author-role">UI/UX & brand designer</span></div>
        </div>
        <p class="author-bio">I design websites, apps and dashboards, and write about UI/UX and freelancing from my client work.</p>
        <div class="author-actions">
          <a href="#contact" class="btn"><svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 3 10 14M21 3l-7 18-4-7-7-4z"/></svg>Start a project</a>
          <a href="about.html" class="btn btn-sec">About me</a>
        </div>
      </div>
    </div>`;

  const start = POSTS.indexOf(b);
  $('keepReading').innerHTML = [1, 2, 3].map(k => POSTS[(start + k) % POSTS.length]).filter(x => x !== b).map(articleCard).join('');
}

/* ================= SIDEBAR SEARCH (projects, blog posts, pages) ================= */
if ($('navSearch')){
  const input = $('navSearchInput'), box = $('searchResults');
  const PAGES = [
    {t:'Home', s:'Page', h:'index.html'}, {t:'About', s:'Page', h:'about.html'},
    {t:'Work', s:'Page', h:'work.html'}, {t:'Blog', s:'Page', h:'blog.html'},
    {t:'Services', s:'Page', h:'services.html'}, {t:'FAQ', s:'Page', h:'index.html#faq'}
  ];
  const items = () => [
    ...PAGES,
    ...PROJECTS.map(p => ({t:p.t, s:'Project · ' + p.cat, h:'project.html?p=' + slug(p), k:p.sum + ' ' + p.hl})),
    ...(typeof POSTS !== 'undefined' ? POSTS.map(b => ({t:b.t, s:'Blog · ' + b.cat, h:postHref(b)})) : [])
  ];
  let found = [], sel = -1;
  const show = () => {
    const q = input.value.trim().toLowerCase();
    if (!q){ box.hidden = true; return; }
    found = items().filter(i => (i.t + ' ' + i.s + ' ' + (i.k || '')).toLowerCase().includes(q)).slice(0, 8);
    sel = found.length ? 0 : -1;
    box.innerHTML = found.length
      ? found.map((i, n) => `<a href="${i.h}"${n === sel ? ' class="on"' : ''}>${esc(i.t)}<span>${esc(i.s)}</span></a>`).join('')
      : '<div class="none">No results</div>';
    box.hidden = false;
  };
  input.addEventListener('input', show);
  input.addEventListener('keydown', e => {
    if (!found.length) return;
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp'){
      e.preventDefault();
      sel = (sel + (e.key === 'ArrowDown' ? 1 : -1) + found.length) % found.length;
      box.querySelectorAll('a').forEach((a, n) => a.classList.toggle('on', n === sel));
    }
    if (e.key === 'Escape'){ box.hidden = true; input.blur(); }
  });
  $('navSearch').onsubmit = e => { e.preventDefault(); if (found[sel]) location.href = found[sel].h; };
  document.addEventListener('click', e => { if (!$('navSearch').contains(e.target)) box.hidden = true; });
}

/* ================= SERVICE DETAIL PAGE ================= */
if ($('serviceDetail')){
  const id = new URLSearchParams(location.search).get('s');
  const s = SERVICES.find(x => slug(x) === id) || SERVICES[0];
  const d = (typeof SERVICE_DETAILS !== 'undefined' && SERVICE_DETAILS[slug(s)]) || {};
  const icon = (name, size) => `<svg width="${size}" height="${size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round">${(typeof SERVICE_ICONS !== 'undefined' && SERVICE_ICONS[name]) || ICONS[name] || ICONS.star}</svg>`;
  document.title = `${s.t} — Sheikh Md Maruf`;
  const related = PROJECTS.filter(p => !d.type || p.type === d.type).slice(0, 2);
  const cover = related[0] || PROJECTS[0];

  const metrics = [['Price', s.price || 'Custom quote', 'cart'], ...(d.metrics || [])];

  $('serviceDetail').innerHTML = `
    <div class="hero-row is-half">
      <div class="panel detail-copy">
        <div class="detail-lead">
          <a href="services.html" class="back-link"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5M11 6l-6 6 6 6"/></svg>Services</a>
          <h1 class="detail-title">${esc(s.t)}</h1>
          <p class="detail-summary">${esc(d.summary || s.s)}</p>
        </div>
        <div class="detail-actions">
          <a href="#contact" class="btn"><svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 3 10 14M21 3l-7 18-4-7-7-4z"/></svg>Start a project</a>
          <a href="mailto:${CONTACT_EMAIL}" class="btn btn-sec">Email me</a>
        </div>
      </div>
      <div class="panel portrait">${cover.img ? `<img src="${cover.img}" alt="${esc(s.t)}">` : art(cover)}</div>
    </div>
    <div class="detail-metrics is-four">${metrics.map(m => `
      <div class="panel metric">
        <div class="metric-top">${esc(m[0])} ${icon(m[2], 18)}</div>
        <div><div class="metric-val">${esc(m[1])}</div></div>
      </div>`).join('')}</div>`;

  $('included-grid').innerHTML = (d.included || []).map(i => `
    <div class="sub-card">
      <div class="sub-card-icon-box">${icon(i[0], 20)}</div>
      <h3 class="sub-card-title">${esc(i[1])}</h3>
      <p class="sub-card-text">${esc(i[2])}</p>
    </div>`).join('');

  $('stepsGrid').innerHTML = (d.steps || []).map((st, n) => `
    <div class="process-step">
      <span class="process-step-index">${String(n + 1).padStart(2, '0')}</span>
      <h3 class="process-step-title">${esc(st[0])}</h3>
      <p class="process-step-text">${esc(st[1])}</p>
    </div>`).join('');

  $('relatedWork').innerHTML = related.map(card).join('');

  $('otherServices').innerHTML = SERVICES.filter(x => x !== s).map(serviceCard).join('');

  ['included', 'how'].forEach(sec => { if (!(d[sec === 'how' ? 'steps' : 'included'] || []).length) $(sec).remove(); });
}
