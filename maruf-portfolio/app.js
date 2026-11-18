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

if ($('services-grid')) $('services-grid').innerHTML = SERVICES.map(s => `
  <a class="panel scard" href="#contact">
    <div class="spanel">
      <div class="sicon"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">${s.icon}</svg></div>
      <div class="stitle">${s.t}</div><p class="ssum">${s.s}</p>
      <div class="stags">${s.tags.map(t => `<span>${t}</span>`).join('')}</div>
    </div>
    <div class="sstrip"><span class="muted">Custom quote</span><span class="v">Start project <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M13 6l6 6-6 6"/></svg></span></div>
  </a>`).join('');

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
else if (PAGE === 'about' || PAGE === 'blog') setActive(l => l.dataset.page === PAGE);
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
const postCover = (b, cls) => b.img ? `<img class="${cls}" src="${b.img}" alt="${esc(b.t)}" loading="lazy">` : postArt(b);

/* Home: "From the blog" — store-style cards */
if ($('blogHome') && typeof POSTS !== 'undefined'){
  $('blogHome').innerHTML = POSTS.slice(0, 3).map((b, i) => `
    <a class="panel product-card" href="${b.url || 'blog.html'}">
      <div class="product-card-media">${postCover(b, 'product-card-image')}
        <div class="product-card-platform-slot"><span class="card-platform"><svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 20h4L19 9l-4-4L4 16z"/></svg>${esc(b.cat)}</span></div>
        ${i === 0 ? '<div class="product-card-badge-slot"><span class="badge">New</span></div>' : ''}
      </div>
      <div class="product-card-strip">
        <div class="product-card-text">
          <div class="product-card-title">${esc(b.t)}</div>
          <div class="product-card-type">${esc(b.date)}</div>
        </div>
        <span class="product-card-price">${esc(b.read)}</span>
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
      <a class="panel article-card" href="${b.url || '#'}">
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
