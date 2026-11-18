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
function card(p){
  return `<a class="panel pcard" data-type="${p.type}" href="${p.url}" target="_blank" rel="noopener">
    <div class="pmedia">${art(p)}<span class="pill"><b>✦</b>${p.hl}</span>${p.badge?`<span class="badge">${p.badge}</span>`:''}</div>
    <div class="pstrip"><div><div class="ptitle">${p.t}<span class="dot-sep"></span><span class="cat">${p.cat}</span></div><div class="psum">${p.sum}</div></div>
    <div class="pmeta">${p.y}${arrowUR}</div></div></a>`;
}

/* Home: first 3 projects (big left + 2 stacked right) */
if ($('featured')){
  const f = PROJECTS.slice(0, 3);
  $('featured').innerHTML = card(f[0]) + `<div class="stack">${f.slice(1).map(card).join('')}</div>`;
}

/* Work page: all projects + filters (link with ?type=app etc. to pre-filter) */
if ($('allWork')){
  const render = type => {
    const list = PROJECTS.filter(p => type === 'all' || p.type === type);
    $('allWork').innerHTML = list.map(card).join('') || '<div class="panel empty">No projects yet.</div>';
    $('workCount').textContent = list.length + (list.length === 1 ? ' project' : ' projects');
  };
  const start = new URLSearchParams(location.search).get('type') || 'all';
  $('filters').querySelectorAll('button').forEach(b => b.classList.toggle('on', b.dataset.f === start));
  render(start);
  $('filters').onclick = e => {
    const b = e.target.closest('button'); if (!b) return;
    $('filters').querySelectorAll('button').forEach(x => x.classList.toggle('on', x === b));
    render(b.dataset.f);
  };
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
if (PAGE === 'work') setActive(l => l.dataset.page === 'work');
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
