// Portfolio CMS — Calendar (Day / Week / Month / Schedule), in the spirit of Google Calendar.
// Click or drag on empty space to create, drag events to move, drag the bottom edge to resize.
(function () {
  'use strict';
  const root = document.querySelector('[data-cal]');
  if (!root) return;

  // ───────── settings ─────────
  const WEEK_START = 0;      // 0 = Sunday, 1 = Monday, 6 = Saturday
  const HOUR = 48;           // px per hour in Day/Week
  const SNAP = 15;           // minutes
  const LANE = 22;           // px per bar row in Month / all-day
  const SCHEDULE_DAYS = 60;
  const PALETTE = [
    ['#3d7bff', 'Cobalt'], ['#d50000', 'Tomato'], ['#e67c73', 'Flamingo'], ['#f4511e', 'Tangerine'],
    ['#f6bf26', 'Banana'], ['#33b679', 'Sage'], ['#0b8043', 'Basil'], ['#039be5', 'Peacock'],
    ['#3f51b5', 'Blueberry'], ['#7986cb', 'Lavender'], ['#8e24aa', 'Grape'], ['#616161', 'Graphite'],
  ];
  const ICON = {
    edit: '<path d="M4 20h4L19 9l-4-4L4 16z"/><path d="M13.5 6.5l4 4"/>',
    trash: '<path d="M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"/>',
    copy: '<rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V5a1 1 0 0 0-1-1H5a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1h3"/>',
    close: '<path d="M6 6l12 12M18 6 6 18"/>',
    pin: '<path d="M12 21s-7-6.1-7-11.5A7 7 0 0 1 19 9.5C19 14.9 12 21 12 21z"/><circle cx="12" cy="9.5" r="2.5"/>',
    text: '<path d="M4 6h16M4 12h16M4 18h10"/>',
    cal: '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>',
    repeat: '<path d="M17 2l4 4-4 4"/><path d="M3 11V9a3 3 0 0 1 3-3h15M7 22l-4-4 4-4"/><path d="M21 13v2a3 3 0 0 1-3 3H3"/>',
    left: '<path d="m15 18-6-6 6-6"/>', right: '<path d="m9 18 6-6-6-6"/>',
    ok: '<circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/>', err: '<circle cx="12" cy="12" r="9"/><path d="M12 8v5M12 16h.01"/>',
  };
  const icon = (n, cls = '') => `<svg class="${cls}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round">${ICON[n]}</svg>`;

  // ───────── small helpers ─────────
  const $ = (s, el = document) => el.querySelector(s);
  const $$ = (s, el = document) => [...el.querySelectorAll(s)];
  const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const h = (tag, cls, html) => { const e = document.createElement(tag); if (cls) e.className = cls; if (html != null) e.innerHTML = html; return e; };
  const token = () => $('input[name="__RequestVerificationToken"]')?.value || '';
  const store = { get(k) { try { return localStorage.getItem('cal-' + k); } catch { return null; } }, set(k, v) { try { localStorage.setItem('cal-' + k, v); } catch { } } };

  const pad = n => String(n).padStart(2, '0');
  const day0 = d => new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const addDays = (d, n) => { const x = new Date(d); x.setDate(x.getDate() + n); return x; };
  const addMin = (d, n) => { const x = new Date(d); x.setMinutes(x.getMinutes() + n); return x; };
  const addMonths = (d, n) => { const x = new Date(d.getFullYear(), d.getMonth() + n, 1); x.setDate(Math.min(d.getDate(), new Date(x.getFullYear(), x.getMonth() + 1, 0).getDate())); return x; };
  const sameDay = (a, b) => a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  const startOfWeek = d => { const x = day0(d); x.setDate(x.getDate() - ((x.getDay() - WEEK_START + 7) % 7)); return x; };
  const wall = d => Date.UTC(d.getFullYear(), d.getMonth(), d.getDate(), d.getHours(), d.getMinutes());
  const minsBetween = (a, b) => Math.round((wall(b) - wall(a)) / 60000);
  const daysBetween = (a, b) => Math.round(minsBetween(day0(a), day0(b)) / 1440);
  const minOfDay = d => d.getHours() * 60 + d.getMinutes();
  const iso = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:00`;
  const isoDate = d => iso(d).slice(0, 10);
  const hhmm = d => `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  const parse = s => { const m = /^(\d{4})-(\d\d)-(\d\d)(?:T(\d\d):(\d\d)(?::(\d\d))?)?/.exec(s || ''); return m ? new Date(+m[1], m[2] - 1, +m[3], +(m[4] || 0), +(m[5] || 0), +(m[6] || 0)) : null; };
  const now = () => new Date();
  const today = () => day0(new Date());
  const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
  const snapTo = (m, step, how = 'round') => Math[how](m / step) * step;
  const lastDay = e => day0(addMin(e.e, -1)); // inclusive last day (end is exclusive)

  const F = (d, o) => d.toLocaleDateString(undefined, o);
  const fTime = d => d.toLocaleTimeString(undefined, d.getMinutes() ? { hour: 'numeric', minute: '2-digit' } : { hour: 'numeric' });
  const fRange = (a, b) => `${fTime(a)} – ${fTime(b)}`;
  const fDayLong = d => F(d, { weekday: 'long', month: 'long', day: 'numeric' });

  function textOn(hex) { // readable text colour on a filled event
    const n = parseInt(hex.slice(1), 16), r = n >> 16 & 255, g = n >> 8 & 255, b = n & 255;
    return (0.299 * r + 0.587 * g + 0.114 * b) / 255 > 0.66 ? '#14171f' : '#fff';
  }
  const tzLabel = () => { const o = -new Date().getTimezoneOffset(), s = o < 0 ? '-' : '+', a = Math.abs(o); return `GMT${s}${pad(Math.floor(a / 60))}${a % 60 ? ':' + pad(a % 60) : ''}`; };

  async function api(url, body) {
    const opt = body === undefined
      ? { headers: { Accept: 'application/json' } }
      : { method: 'POST', headers: { 'Content-Type': 'application/json', Accept: 'application/json', RequestVerificationToken: token() }, body: JSON.stringify(body) };
    const r = await fetch(url, opt);
    if (r.status === 401 || (r.redirected && r.url.includes('/account/login'))) { location.href = '/account/login?returnUrl=/calendar'; throw new Error('login'); }
    let data = null; try { data = await r.json(); } catch { }
    if (!r.ok) throw new Error(data?.error || 'Something went wrong. Please try again.');
    return data;
  }

  // ───────── state ─────────
  const S = {
    view: store.get('view') || (innerWidth < 700 ? 'day' : 'week'),
    date: today(),
    cals: window.CAL_INIT || [],
    events: [],
    q: '',
    mini: null,       // month shown in the mini calendar (null = follow S.date)
    scroll: null,     // remembered scroll of the time grid
  };
  { // #/week/2026-11-25
    const m = /^#\/(day|week|month|schedule)\/(\d{4}-\d\d-\d\d)/.exec(location.hash);
    if (m) { S.view = m[1]; S.date = parse(m[2]) || today(); }
  }
  const calOf = id => S.cals.find(c => c.id === id);
  const visible = () => S.events.filter(e => calOf(e.calendarId)?.visible !== false);
  const defaultCal = () => { const id = +store.get('lastcal'); return (calOf(id)?.visible && id) || S.cals.find(c => c.visible)?.id || S.cals[0]?.id; };

  const body = $('[data-body]');
  const pop = $('[data-pop]');

  // ───────── ranges & loading ─────────
  function range() {
    const d = S.date;
    if (S.view === 'day') return [day0(d), addDays(day0(d), 1)];
    if (S.view === 'week') { const s = startOfWeek(d); return [s, addDays(s, 7)]; }
    if (S.view === 'month') {
      const first = new Date(d.getFullYear(), d.getMonth(), 1), s = startOfWeek(first);
      const last = new Date(d.getFullYear(), d.getMonth() + 1, 0);
      return [s, addDays(s, Math.ceil((daysBetween(s, last) + 1) / 7) * 7)];
    }
    return [day0(d), addDays(day0(d), SCHEDULE_DAYS)];
  }

  let seq = 0;
  async function load() {
    const [a, b] = range(), my = ++seq;
    try {
      const list = S.q
        ? await api('/calendar/search?q=' + encodeURIComponent(S.q))
        : await api(`/calendar/events?from=${iso(a)}&to=${iso(b)}`);
      if (my !== seq) return;
      S.events = (list || []).map(e => ({ ...e, s: parse(e.start), e: parse(e.end) }));
    } catch (err) {
      if (my !== seq) return;
      if (err.message !== 'login') toast(err.message, 'err');
      S.events = [];
    }
    render();
  }

  function go(view, date) {
    if (view) { S.view = view; store.set('view', view); }
    if (date) S.date = day0(date);
    S.mini = null;
    closePop();
    load();
  }
  function step(dir) {
    const d = S.date;
    const next = S.view === 'day' ? addDays(d, dir) : S.view === 'week' ? addDays(d, 7 * dir) : addMonths(d, dir);
    go(null, next);
  }

  // ───────── render ─────────
  function render() {
    const keepScroll = $('.tg-scroll', body);
    if (keepScroll) S.scroll = keepScroll.scrollTop;
    $$('[data-view]').forEach(b => { b.classList.toggle('on', b.dataset.view === S.view && !S.q); b.setAttribute('aria-selected', b.dataset.view === S.view); });
    $('[data-title]').textContent = title();
    history.replaceState(null, '', `#/${S.view}/${isoDate(S.date)}`);
    renderMini();
    renderLists();
    body.className = 'cal-body v-' + (S.q ? 'search' : S.view);
    if (S.q) renderSchedule(true);
    else if (S.view === 'day') renderGrid([S.date]);
    else if (S.view === 'week') { const s = startOfWeek(S.date); renderGrid([...Array(7)].map((_, i) => addDays(s, i))); }
    else if (S.view === 'month') renderMonth();
    else renderSchedule(false);
  }

  function title() {
    if (S.q) return 'Search results';
    const d = S.date;
    if (S.view === 'day') return F(d, { day: 'numeric', month: 'long', year: 'numeric' });
    if (S.view === 'week') {
      const a = startOfWeek(d), b = addDays(a, 6);
      if (a.getMonth() === b.getMonth()) return F(a, { month: 'long', year: 'numeric' });
      if (a.getFullYear() === b.getFullYear()) return `${F(a, { month: 'short' })} – ${F(b, { month: 'short', year: 'numeric' })}`;
      return `${F(a, { month: 'short', year: 'numeric' })} – ${F(b, { month: 'short', year: 'numeric' })}`;
    }
    return F(d, { month: 'long', year: 'numeric' });
  }

  // ── Mini month (left side) ──
  function renderMini() {
    const box = $('[data-mini]');
    const m = S.mini || new Date(S.date.getFullYear(), S.date.getMonth(), 1);
    const s = startOfWeek(m), t = today();
    const [ra, rb] = S.view === 'week' || S.view === 'day' ? range() : [null, null];
    let html = `<div class="mini-head"><b>${esc(F(m, { month: 'long', year: 'numeric' }))}</b>
      <button type="button" class="mini-nav" data-mini-step="-1" aria-label="Previous month">${icon('left')}</button>
      <button type="button" class="mini-nav" data-mini-step="1" aria-label="Next month">${icon('right')}</button></div><div class="mini-grid">`;
    for (let i = 0; i < 7; i++) html += `<span class="mini-dow">${esc(F(addDays(s, i), { weekday: 'narrow' }))}</span>`;
    for (let i = 0; i < 42; i++) {
      const d = addDays(s, i);
      const cls = [d.getMonth() !== m.getMonth() && 'out', sameDay(d, t) && 'today', sameDay(d, S.date) && 'sel', ra && d >= ra && d < rb && 'inrange'].filter(Boolean).join(' ');
      html += `<button type="button" class="${cls}" data-mini-date="${isoDate(d)}" aria-label="${esc(fDayLong(d))}">${d.getDate()}</button>`;
    }
    box.innerHTML = html + '</div>';
  }

  // ── "My calendars" ──
  function renderLists() {
    const box = $('[data-cal-lists]');
    box.innerHTML = S.cals.map(c => `
      <div class="cl-row">
        <label class="cl-check" style="--c:${esc(c.color)}">
          <input type="checkbox" data-cal-toggle="${c.id}" ${c.visible ? 'checked' : ''} />
          <span class="cl-box"></span><span class="cl-name">${esc(c.name)}</span>
        </label>
        <button type="button" class="cl-btn" data-cal-edit="${c.id}" title="Edit ${esc(c.name)}" aria-label="Edit ${esc(c.name)}">${icon('edit')}</button>
      </div>`).join('');
  }

  // ── Bars for Month rows and the all-day strip ──
  function layoutBars(evs, days) {
    const n = days.length, a0 = days[0], b0 = addDays(days[n - 1], 1);
    const items = evs.filter(e => e.s < b0 && e.e > a0).map(e => ({
      e, a: Math.max(0, daysBetween(a0, e.s)), b: Math.min(n - 1, daysBetween(a0, lastDay(e))),
      contL: e.s < a0, contR: e.e > b0,
    })).sort((x, y) => x.a - y.a || (y.b - y.a) - (x.b - x.a) || (+y.e.allDay - +x.e.allDay) || x.e.s - y.e.s);
    const lanes = [];
    for (const it of items) {
      let l = 0;
      for (; ; l++) {
        lanes[l] ??= [];
        let free = true;
        for (let d = it.a; d <= it.b; d++) if (lanes[l][d]) { free = false; break; }
        if (free) break;
      }
      for (let d = it.a; d <= it.b; d++) lanes[l][d] = true;
      it.lane = l;
    }
    return { items, laneCount: lanes.length };
  }
  const isBlock = e => e.allDay || minsBetween(e.s, e.e) >= 1440;

  function barEl(it, n, top, monthStyle) {
    const e = it.e, block = !monthStyle || isBlock(e) || it.b > it.a;
    const el = h('div', 'bar' + (block ? ' block' : ' dot') + (it.contL ? ' cl' : '') + (it.contR ? ' cr' : '') + (e.e < now() ? ' past' : ''));
    el.style.cssText = `left:calc(${it.a / n * 100}% + 2px);width:calc(${(it.b - it.a + 1) / n * 100}% - 5px);top:${top}px;--c:${e.color};--on:${textOn(e.color)}`;
    el.innerHTML = block
      ? `<span class="t">${!e.allDay && !it.contL ? `<em>${esc(fTime(e.s))}</em> ` : ''}${esc(e.title)}</span>`
      : `<i></i><em>${esc(fTime(e.s))}</em><span class="t">${esc(e.title)}</span>`;
    el.tabIndex = 0;
    el._ev = e;
    el.title = `${e.title} · ${whenText(e)}`;
    return el;
  }

  // ── Month ──
  function renderMonth() {
    const [a, b] = range(), weeks = daysBetween(a, b) / 7, t = today(), month = S.date.getMonth();
    body.innerHTML = '';
    const head = h('div', 'mo-head');
    for (let i = 0; i < 7; i++) head.append(h('div', '', esc(F(addDays(a, i), { weekday: 'short' }))));
    const grid = h('div', 'mo-grid');
    grid.style.setProperty('--weeks', weeks);
    body.append(head, grid);
    const rows = [];
    for (let w = 0; w < weeks; w++) {
      const row = h('div', 'mo-row'), days = [];
      for (let i = 0; i < 7; i++) {
        const d = addDays(a, w * 7 + i); days.push(d);
        const c = h('div', 'dcell' + (d.getMonth() !== month ? ' out' : '') + (sameDay(d, t) ? ' today' : '') + (d < t ? ' past' : ''));
        c.dataset.date = isoDate(d);
        const label = d.getDate() === 1 ? F(d, { month: 'short', day: 'numeric' }) : d.getDate();
        c.innerHTML = `<button type="button" class="num" data-goto="${isoDate(d)}" aria-label="${esc(fDayLong(d))}">${esc(label)}</button>`;
        row.append(c);
      }
      const layer = h('div', 'bars'); row.append(layer);
      grid.append(row); rows.push({ row, days, layer });
    }
    // Lay out after the rows have a height.
    const rowH = rows[0].row.clientHeight;
    const maxLanes = Math.max(1, Math.floor((rowH - 30) / LANE));
    const evs = visible();
    for (const { days, layer } of rows) {
      const { items, laneCount } = layoutBars(evs, days);
      const showLanes = laneCount > maxLanes ? maxLanes - 1 : maxLanes;
      const hidden = Array(7).fill(0);
      for (const it of items) {
        if (it.lane < showLanes) layer.append(barEl(it, 7, 28 + it.lane * LANE, true));
        else for (let d = it.a; d <= it.b; d++) hidden[d]++;
      }
      hidden.forEach((cnt, d) => {
        if (!cnt) return;
        const m = h('button', 'more', `${cnt} more`);
        m.type = 'button';
        m.style.cssText = `left:calc(${d / 7 * 100}% + 2px);width:calc(${100 / 7}% - 5px);top:${28 + showLanes * LANE}px`;
        m.dataset.more = isoDate(days[d]);
        layer.append(m);
      });
    }
    bindDayCells(grid);
  }

  // ── Day / Week time grid ──
  function renderGrid(days) {
    const t = today(), n = days.length, evs = visible();
    body.innerHTML = '';
    const wrap = h('div', 'tg');
    wrap.style.setProperty('--cols', n);

    const head = h('div', 'tg-head');
    head.append(h('div', 'tg-gutter tz', esc(tzLabel())));
    for (const d of days) {
      const c = h('div', 'tg-day' + (sameDay(d, t) ? ' today' : '') + (d < t ? ' past' : ''));
      c.innerHTML = `<span class="dow">${esc(F(d, { weekday: 'short' }))}</span><button type="button" class="num" data-goto="${isoDate(d)}" aria-label="${esc(fDayLong(d))}">${d.getDate()}</button>`;
      head.append(c);
    }

    // All-day strip
    const ad = h('div', 'tg-allday');
    ad.append(h('div', 'tg-gutter', '<span>all-day</span>'));
    const adGrid = h('div', 'ad-grid');
    days.forEach(d => { const c = h('div', 'dcell'); c.dataset.date = isoDate(d); adGrid.append(c); });
    const layer = h('div', 'bars');
    const { items, laneCount } = layoutBars(evs.filter(isBlock), days);
    for (const it of items) layer.append(barEl(it, n, 3 + it.lane * LANE, false));
    adGrid.style.height = Math.max(26, laneCount * LANE + 6) + 'px';
    adGrid.append(layer);
    ad.append(adGrid);

    // Hours
    const sc = h('div', 'tg-scroll');
    const inner = h('div', 'tg-inner');
    const gut = h('div', 'tg-gutter hours');
    for (let i = 1; i < 24; i++) gut.append(h('span', '', esc(fTime(new Date(2000, 0, 1, i)))));
    inner.append(gut);
    const cols = [];
    for (const d of days) {
      const col = h('div', 'tg-col' + (sameDay(d, t) ? ' today' : ''));
      col.dataset.date = isoDate(d);
      const dayEnd = addDays(d, 1);
      const segs = evs.filter(e => !isBlock(e) && e.s < dayEnd && e.e > d).map(e => ({
        e, s: e.s < d ? d : e.s, en: e.e > dayEnd ? dayEnd : e.e, isStart: e.s >= d, isEnd: e.e <= dayEnd,
      }));
      layoutDay(segs);
      for (const sg of segs) col.append(evEl(sg, d));
      if (sameDay(d, t)) { const line = h('div', 'now'); line.style.top = minOfDay(now()) / 60 * HOUR + 'px'; col.append(line); }
      inner.append(col); cols.push(col);
    }
    sc.append(inner);
    wrap.append(head, ad, sc);
    body.append(wrap);

    sc.scrollTop = S.scroll ?? Math.max(0, (sameDay(days[0], t) || n > 1 ? Math.min(minOfDay(now()) / 60 - 2, 16) : 7) * HOUR);
    S.scroll = null;
    bindGrid(inner, cols);
    bindDayCells(adGrid);
  }

  function layoutDay(segs) {
    segs.sort((a, b) => a.s - b.s || b.en - a.en);
    let cluster = [], cols = [], clusterEnd = 0;
    const flush = () => { for (const x of cluster) x.n = cols.length; cluster = []; cols = []; clusterEnd = 0; };
    for (const sg of segs) {
      const s = +sg.s, e = Math.max(+sg.en, s + 25 * 60000);
      if (cluster.length && s >= clusterEnd) flush();
      let c = cols.findIndex(end => end <= s);
      if (c < 0) { c = cols.length; cols.push(e); } else cols[c] = e;
      sg.col = c; cluster.push(sg); clusterEnd = Math.max(clusterEnd, e);
    }
    flush();
  }

  function evEl(sg, d) {
    const e = sg.e, top = minsBetween(d, sg.s) / 60 * HOUR, mins = minsBetween(sg.s, sg.en);
    const el = h('div', 'ev' + (mins < 45 ? ' short' : '') + (e.e < now() ? ' past' : ''));
    const w = 100 / sg.n;
    el.style.cssText = `top:${top}px;height:${Math.max(mins / 60 * HOUR - 2, 18)}px;left:calc(${sg.col * w}% + 1px);width:calc(${sg.col === sg.n - 1 ? w : w * 1.6}% - ${sg.col === sg.n - 1 ? 6 : 2}px);z-index:${2 + sg.col};--c:${e.color};--on:${textOn(e.color)}`;
    el.innerHTML = `<div class="ev-t">${esc(e.title)}</div>${e.subtitle && mins >= 60 ? `<div class="ev-sub">${esc(e.subtitle)}</div>` : ''}<div class="ev-time">${esc(fRange(e.s, e.e))}</div>${e.location ? `<div class="ev-loc">${esc(e.location)}</div>` : ''}${sg.isEnd ? '<div class="ev-resize" aria-hidden="true"></div>' : ''}`;
    el.tabIndex = 0;
    el._ev = e; el._sg = sg;
    el.title = `${e.title} · ${whenText(e)}`;
    return el;
  }

  // ── Schedule / search results ──
  function renderSchedule(search) {
    body.innerHTML = '';
    const evs = visible().slice().sort((a, b) => a.s - b.s || +b.allDay - +a.allDay);
    const groups = new Map();
    const [ra, rb] = range();
    for (const e of evs) {
      if (search) { const k = isoDate(e.s); if (!groups.has(k)) groups.set(k, []); groups.get(k).push(e); continue; }
      // a multi-day event shows on each of its days inside the range
      for (let d = day0(e.s < ra ? ra : e.s); d <= lastDay(e) && d < rb; d = addDays(d, 1)) {
        const k = isoDate(d); if (!groups.has(k)) groups.set(k, []); groups.get(k).push(e);
      }
    }
    const list = h('div', 'sch');
    if (!groups.size) {
      list.innerHTML = search
        ? `<div class="empty">No events match “${esc(S.q)}”.</div>`
        : `<div class="empty"><b>Nothing planned</b><br>No events in the next ${SCHEDULE_DAYS} days.<br><button type="button" class="btn primary" data-act="create" style="margin-top:14px">Create an event</button></div>`;
    }
    const t = today();
    for (const [k, items] of [...groups.entries()].sort()) {
      const d = parse(k);
      const day = h('div', 'sch-day' + (sameDay(d, t) ? ' today' : '') + (d < t ? ' past' : ''));
      day.innerHTML = `<button type="button" class="sch-date" data-goto="${k}"><b>${d.getDate()}</b><span>${esc(F(d, { month: 'short', weekday: 'short' }))}${d.getFullYear() !== t.getFullYear() ? ' ' + d.getFullYear() : ''}</span></button>`;
      const ul = h('div', 'sch-list');
      for (const e of items) {
        const r = h('button', 'sch-ev' + (e.e < now() ? ' past' : ''));
        r.type = 'button';
        r.style.setProperty('--c', e.color);
        const multi = daysBetween(e.s, lastDay(e)) > 0;
        const time = e.allDay ? 'All day' : multi ? (sameDay(e.s, d) ? `${fTime(e.s)} –` : sameDay(lastDay(e), d) && !sameDay(day0(e.e), e.e) ? `– ${fTime(e.e)}` : 'All day') : fRange(e.s, e.e);
        r.innerHTML = `<i></i><span class="sch-time">${esc(time)}</span><span class="sch-title"><span class="sch-tt">${esc(e.title)}${e.subtitle ? ` <small>· ${esc(e.subtitle)}</small>` : ''}</span>${e.repeat !== 'none' ? icon('repeat', 'rep') : ''}</span><span class="sch-loc">${e.imageUrl ? `<img src="${esc(e.imageUrl)}" alt="" loading="lazy" />` : ''}${esc(e.location || '')}</span>`;
        r._ev = e;
        ul.append(r);
      }
      day.append(ul);
      list.append(day);
    }
    body.append(list);
    list.addEventListener('click', e => { const r = e.target.closest('.sch-ev'); if (r) openDetail(r._ev, r); });
  }

  // ───────── text helpers ─────────
  function whenText(e) {
    const ld = lastDay(e);
    if (e.allDay) return sameDay(e.s, ld) ? fDayLong(e.s) : `${F(e.s, { month: 'short', day: 'numeric' })} – ${F(ld, { month: 'short', day: 'numeric', year: 'numeric' })}`;
    if (sameDay(e.s, e.e) || (sameDay(e.s, ld) && minOfDay(e.e) === 0)) return `${fDayLong(e.s)} · ${fRange(e.s, e.e)}`;
    return `${F(e.s, { month: 'short', day: 'numeric' })}, ${fTime(e.s)} – ${F(e.e, { month: 'short', day: 'numeric' })}, ${fTime(e.e)}`;
  }
  function repeatLabel(kind, d) {
    return {
      none: 'Does not repeat', daily: 'Every day', weekdays: 'Every weekday (Monday to Friday)',
      weekly: `Weekly on ${F(d, { weekday: 'long' })}`, monthly: `Monthly on day ${d.getDate()}`,
      yearly: `Annually on ${F(d, { month: 'long', day: 'numeric' })}`,
    }[kind] || '';
  }
  function repeatText(e) {
    if (e.repeat === 'none') return '';
    const u = parse(e.repeatUntil);
    return repeatLabel(e.repeat, parse(e.seriesStart)) + (u ? `, until ${F(u, { month: 'short', day: 'numeric', year: 'numeric' })}` : '');
  }

  // ───────── popover ─────────
  let popCleanup = null, justClosed = false;
  function showPop(html, anchor, cls = '') {
    closePop();
    pop.className = 'cal-pop ' + cls;
    pop.innerHTML = html;
    pop.hidden = false;
    place(anchor.getBoundingClientRect ? anchor.getBoundingClientRect() : anchor);
  }
  function place(r) {
    const m = 10, vw = innerWidth, vh = innerHeight;
    if (vw < 600) { pop.classList.add('sheet'); pop.style.left = pop.style.top = ''; return; }
    const pw = pop.offsetWidth, ph = pop.offsetHeight;
    let x = r.right + m;
    if (x + pw > vw - m) x = r.left - pw - m;
    if (x < m) x = clamp(r.left + r.width / 2 - pw / 2, m, vw - pw - m);
    let y = clamp(r.top, m, vh - ph - m);
    pop.style.left = x + 'px'; pop.style.top = y + 'px';
  }
  function closePop() {
    if (pop.hidden) return false;
    pop.hidden = true; pop.innerHTML = '';
    const f = popCleanup; popCleanup = null; f?.();
    return true;
  }
  // A click outside the card only closes it (like Google), it doesn't start a new event.
  document.addEventListener('pointerdown', e => {
    if (pop.hidden || pop.contains(e.target)) return;
    if (closePop()) { justClosed = true; setTimeout(() => { justClosed = false; }, 0); }
  }, true);
  const swallow = () => { if (justClosed) { justClosed = false; return true; } return false; };

  // Quick create card
  function openQuick(draft, anchor, cleanup) {
    const cals = S.cals.map(c => `<option value="${c.id}" ${c.id === draft.calendarId ? 'selected' : ''}>${esc(c.name)}</option>`).join('');
    const when = whenText({ s: draft.start, e: draft.end, allDay: draft.allDay });
    showPop(`
      <div class="pop-tools"><button type="button" class="pop-x" data-p="close" aria-label="Close">${icon('close')}</button></div>
      <form class="pop-quick" data-p-form>
        <input type="text" class="pop-title" name="title" placeholder="Add title" maxlength="200" aria-label="Title" />
        <div class="pop-line">${icon('cal')}<span>${esc(when)}</span></div>
        <div class="pop-line"><span class="sq" data-p-sq style="--c:${esc(calOf(draft.calendarId)?.color || '#3d7bff')}"></span><select name="calendarId" aria-label="Calendar">${cals}</select></div>
        <div class="pop-foot"><button type="button" class="btn" data-p="more">More options</button><button type="submit" class="btn primary">Save</button></div>
      </form>`, anchor, 'quick');
    popCleanup = cleanup;
    const form = $('[data-p-form]', pop), sel = form.calendarId;
    sel.addEventListener('change', () => $('[data-p-sq]', pop).style.setProperty('--c', calOf(+sel.value)?.color));
    setTimeout(() => form.title.focus(), 0);
    const collect = () => ({ ...draft, title: form.title.value.trim(), calendarId: +sel.value });
    form.addEventListener('submit', async e => {
      e.preventDefault();
      const d = collect();
      closePop();
      await saveDraft(d, 'Event saved');
    });
    pop.querySelector('[data-p="more"]').addEventListener('click', () => { const d = collect(); closePop(); openEditor(d); });
    pop.querySelector('[data-p="close"]').addEventListener('click', closePop);
  }

  // Event details card
  function openDetail(e, anchor) {
    const cal = calOf(e.calendarId);
    const loc = e.location ? (/^https?:\/\//i.test(e.location)
      ? `<a href="${esc(e.location)}" target="_blank" rel="noopener noreferrer">${esc(e.location)}</a>` : esc(e.location)) : '';
    showPop(`
      <div class="pop-tools">
        <button type="button" data-p="edit" title="Edit event" aria-label="Edit event">${icon('edit')}</button>
        <button type="button" data-p="dup" title="Duplicate" aria-label="Duplicate">${icon('copy')}</button>
        <button type="button" data-p="del" title="Delete event" aria-label="Delete event">${icon('trash')}</button>
        <button type="button" class="pop-x" data-p="close" aria-label="Close">${icon('close')}</button>
      </div>
      ${e.imageUrl ? `<div class="pop-img"><img src="${esc(e.imageUrl)}" alt="" loading="lazy" /></div>` : ''}
      <div class="pop-detail">
        <div class="pop-line top"><span class="sq" style="--c:${esc(e.color)}"></span><div><h3>${esc(e.title)}</h3>${e.subtitle ? `<p class="pop-sub">${esc(e.subtitle)}</p>` : ''}<p>${esc(whenText(e))}</p>${e.repeat !== 'none' ? `<p class="muted">${esc(repeatText(e))}</p>` : ''}</div></div>
        ${loc ? `<div class="pop-line">${icon('pin')}<span>${loc}</span></div>` : ''}
        ${e.description ? `<div class="pop-line">${icon('text')}<span class="desc">${esc(e.description)}</span></div>` : ''}
        <div class="pop-line">${icon('cal')}<span>${esc(cal?.name || '')}</span></div>
      </div>`, anchor, 'detail');
    const act = a => pop.querySelector(`[data-p="${a}"]`);
    act('close').addEventListener('click', closePop);
    act('edit').addEventListener('click', () => { closePop(); openEditor(draftOf(e)); });
    act('dup').addEventListener('click', () => { closePop(); openEditor({ ...draftOf(e), id: null, title: e.title + ' (copy)' }); });
    act('del').addEventListener('click', () => { closePop(); deleteEvent(e); });
  }

  // "+3 more" card
  function openMore(dateKey, anchor) {
    const d = parse(dateKey), end = addDays(d, 1);
    const evs = visible().filter(e => e.s < end && e.e > d).sort((a, b) => +b.allDay - +a.allDay || a.s - b.s);
    showPop(`<div class="pop-tools"><button type="button" class="pop-x" data-p="close" aria-label="Close">${icon('close')}</button></div>
      <div class="pop-more"><div class="pm-head"><span>${esc(F(d, { weekday: 'short' }))}</span><button type="button" class="num" data-goto="${dateKey}">${d.getDate()}</button></div><div class="pm-list"></div></div>`, anchor, 'more-pop');
    const list = $('.pm-list', pop);
    for (const e of evs) {
      const it = { e, a: 0, b: 0, contL: e.s < d, contR: e.e > end, lane: 0 };
      const b = barEl(it, 1, 0, true);
      b.style.cssText = `position:relative;left:0;width:100%;top:0;--c:${e.color};--on:${textOn(e.color)}`;
      b.addEventListener('click', () => openDetail(e, b));
      list.append(b);
    }
    place(anchor.getBoundingClientRect());
    pop.querySelector('[data-p="close"]').addEventListener('click', closePop);
  }

  // ───────── interactions: time grid ─────────
  function bindGrid(inner, cols) {
    const colAt = x => cols.find(c => { const r = c.getBoundingClientRect(); return x >= r.left && x < r.right; });
    const minutesAt = (col, y) => clamp((y - col.getBoundingClientRect().top) / HOUR * 60, 0, 1440);

    inner.addEventListener('pointerdown', e => {
      if (e.button !== 0 || swallow()) return;
      const evNode = e.target.closest('.ev'), col = e.target.closest('.tg-col');
      if (!col) return;
      if (evNode) dragEvent(e, evNode, e.target.closest('.ev-resize') ? 'resize' : 'move');
      else selectRange(e, col);
    });
    inner.addEventListener('keydown', e => { const n = e.target.closest('.ev'); if (n && (e.key === 'Enter' || e.key === ' ')) { e.preventDefault(); openDetail(n._ev, n); } });

    function selectRange(e, col) {
      const day = parse(col.dataset.date);
      const m0 = snapTo(minutesAt(col, e.clientY), SNAP, 'floor');
      const ghost = h('div', 'ev ghost');
      let a = m0, b = m0 + SNAP, moved = false;
      const y0 = e.clientY;
      const draw = () => {
        ghost.style.cssText = `top:${a / 60 * HOUR}px;height:${(b - a) / 60 * HOUR - 2}px;left:1px;width:calc(100% - 6px)`;
        ghost.innerHTML = `<div class="ev-t">(No title)</div><div class="ev-time">${esc(fRange(addMin(day, a), addMin(day, b)))}</div>`;
      };
      const move = ev => {
        if (!moved && Math.abs(ev.clientY - y0) < 5) return;
        if (!moved) { moved = true; col.append(ghost); }
        const cur = snapTo(minutesAt(col, ev.clientY), SNAP, 'floor');
        if (cur >= m0) { a = m0; b = Math.min(1440, cur + SNAP); } else { a = cur; b = m0 + SNAP; }
        draw();
      };
      const up = () => {
        off();
        if (!moved) {
          a = clamp(snapTo(minutesAt(col, y0), 30, 'floor'), 0, 1380); b = a + 60;
          col.append(ghost);
        }
        draw();
        openQuick({ id: null, calendarId: defaultCal(), start: addMin(day, a), end: addMin(day, b), allDay: false, repeat: 'none' }, ghost, () => ghost.remove());
      };
      const cancel = () => { off(); ghost.remove(); };
      const off = () => { removeEventListener('pointermove', move); removeEventListener('pointerup', up); removeEventListener('pointercancel', cancel); };
      addEventListener('pointermove', move); addEventListener('pointerup', up); addEventListener('pointercancel', cancel);
    }

    function dragEvent(e, node, mode) {
      const ev = node._ev, sg = node._sg, col0 = node.parentElement, day0d = parse(col0.dataset.date);
      const x0 = e.clientX, y0 = e.clientY;
      const grab = minutesAt(col0, y0) - minsBetween(day0d, sg.s);
      let moved = false, dStart = 0, dEnd = 0;
      e.preventDefault();
      const move = p => {
        if (!moved && Math.hypot(p.clientX - x0, p.clientY - y0) < 5) return;
        moved = true; node.classList.add('dragging');
        if (mode === 'move') {
          const col = colAt(p.clientX) || node.parentElement, d = parse(col.dataset.date);
          const m = clamp(snapTo(minutesAt(col, p.clientY) - grab, SNAP), 0, 1440 - SNAP);
          if (col !== node.parentElement) col.append(node);
          const ns = addMin(d, m);
          dStart = dEnd = minsBetween(sg.s, ns);
          node.style.top = m / 60 * HOUR + 'px'; node.style.left = '1px'; node.style.width = 'calc(100% - 6px)'; node.style.zIndex = 30;
          $('.ev-time', node).textContent = fRange(addMin(ev.s, dStart), addMin(ev.e, dEnd));
        } else {
          const startM = minsBetween(day0d, sg.s);
          const m = clamp(snapTo(minutesAt(col0, p.clientY), SNAP), startM + SNAP, 1440);
          dStart = 0; dEnd = minsBetween(sg.en, addMin(day0d, m));
          node.style.height = (m - startM) / 60 * HOUR - 2 + 'px';
          $('.ev-time', node).textContent = fRange(ev.s, addMin(ev.e, dEnd));
        }
      };
      const up = () => {
        off();
        if (!moved) { openDetail(ev, node); return; }
        if (!dStart && !dEnd) { render(); return; }
        moveEvent(ev, dStart, dEnd, mode === 'move' ? 'Event moved' : 'Event updated');
      };
      const cancel = () => { off(); if (moved) render(); };
      const off = () => { removeEventListener('pointermove', move); removeEventListener('pointerup', up); removeEventListener('pointercancel', cancel); };
      addEventListener('pointermove', move); addEventListener('pointerup', up); addEventListener('pointercancel', cancel);
    }
  }

  // ───────── interactions: month cells & all-day strip ─────────
  function bindDayCells(container) {
    const cellAt = (x, y) => {
      for (const n of document.elementsFromPoint(x, y)) { const c = n.closest?.('.dcell'); if (c && container.contains(c)) return c; }
      return null;
    };
    const cells = () => $$('.dcell', container);
    const highlight = (a, b) => { for (const c of cells()) { const d = parse(c.dataset.date); c.classList.toggle('hl', d >= a && d <= b); } };
    const clear = () => cells().forEach(c => c.classList.remove('hl'));

    container.addEventListener('click', e => {
      const more = e.target.closest('[data-more]');
      if (more) openMore(more.dataset.more, more);
    });
    container.addEventListener('keydown', e => { const b = e.target.closest('.bar'); if (b && (e.key === 'Enter' || e.key === ' ')) { e.preventDefault(); openDetail(b._ev, b); } });

    container.addEventListener('pointerdown', e => {
      if (e.button !== 0 || e.target.closest('[data-goto],[data-more]') || swallow()) return;
      const bar = e.target.closest('.bar'), start = cellAt(e.clientX, e.clientY);
      if (!start) return;
      e.preventDefault();
      const d0 = parse(start.dataset.date), x0 = e.clientX, y0 = e.clientY;
      let moved = false, cur = d0;
      const move = p => {
        if (!moved && Math.hypot(p.clientX - x0, p.clientY - y0) < 5) return;
        moved = true;
        if (bar) bar.classList.add('dragging');
        const c = cellAt(p.clientX, p.clientY); if (!c) return;
        cur = parse(c.dataset.date);
        if (bar) { const dd = daysBetween(d0, cur); highlight(addDays(day0(bar._ev.s), dd), addDays(lastDay(bar._ev), dd)); }
        else highlight(cur < d0 ? cur : d0, cur < d0 ? d0 : cur);
      };
      const up = () => {
        off();
        if (bar) {
          bar.classList.remove('dragging'); clear();
          if (!moved) { openDetail(bar._ev, bar); return; }
          const dd = daysBetween(d0, cur);
          if (dd) moveEvent(bar._ev, dd * 1440, dd * 1440, 'Event moved');
          return;
        }
        const a = cur < d0 ? cur : d0, b = cur < d0 ? d0 : cur;
        highlight(a, b);
        const anchor = container.querySelector(`.dcell[data-date="${isoDate(b)}"]`) || start;
        openQuick({ id: null, calendarId: defaultCal(), start: a, end: addDays(b, 1), allDay: true, repeat: 'none' }, anchor, clear);
      };
      const cancel = () => { off(); clear(); bar?.classList.remove('dragging'); };
      const off = () => { removeEventListener('pointermove', move); removeEventListener('pointerup', up); removeEventListener('pointercancel', cancel); };
      addEventListener('pointermove', move); addEventListener('pointerup', up); addEventListener('pointercancel', cancel);
    });
  }

  // ───────── saving ─────────
  const draftOf = e => ({
    id: e.id, calendarId: e.calendarId, title: e.title, subtitle: e.subtitle || '', imageUrl: e.imageUrl || '',
    description: e.description || '', location: e.location || '',
    start: parse(e.seriesStart), end: parse(e.seriesEnd), allDay: e.allDay, color: e.ownColor || '',
    repeat: e.repeat, repeatUntil: e.repeatUntil || '', occurrence: e.repeat !== 'none',
  });
  const payload = d => ({
    id: d.id || null, calendarId: d.calendarId, title: d.title, subtitle: d.subtitle || null, imageUrl: d.imageUrl || null,
    description: d.description || null, location: d.location || null,
    start: iso(d.start), end: iso(d.end), allDay: !!d.allDay, color: d.color || null, repeat: d.repeat || 'none',
    repeatUntil: d.repeat && d.repeat !== 'none' && d.repeatUntil ? d.repeatUntil + 'T00:00:00' : null,
  });

  async function saveDraft(d, msg) {
    try {
      const r = await api('/calendar/save', payload(d));
      store.set('lastcal', d.calendarId);
      await load();
      toast(msg);
      return r;
    } catch (err) { if (err.message !== 'login') toast(err.message, 'err'); throw err; }
  }
  async function moveEvent(e, ds, de, msg) {
    try {
      await api('/calendar/move', { id: e.id, startMinutes: ds, endMinutes: de });
      await load();
      toast(msg + (e.repeat !== 'none' ? ' (all repeats)' : ''), 'ok', { label: 'Undo', fn: async () => { await api('/calendar/move', { id: e.id, startMinutes: -ds, endMinutes: -de }); load(); } });
    } catch (err) { if (err.message !== 'login') toast(err.message, 'err'); render(); }
  }
  async function deleteEvent(e) {
    const backup = draftOf(e);
    try {
      await api('/calendar/delete', { id: e.id });
      await load();
      toast(e.repeat !== 'none' ? 'Event and all its repeats deleted' : 'Event deleted', 'ok', {
        label: 'Undo', fn: async () => { await api('/calendar/save', payload({ ...backup, id: null })); load(); },
      });
    } catch (err) { if (err.message !== 'login') toast(err.message, 'err'); }
  }

  // ───────── full editor ─────────
  const ed = $('[data-editor]'), edForm = $('[data-editor-form]');
  let edDraft = null;
  function swatches(box, name, selected, withDefault) {
    box.innerHTML = (withDefault ? `<label class="sw def" title="Calendar colour"><input type="radio" name="${name}" value="" ${!selected ? 'checked' : ''}/><span></span></label>` : '')
      + PALETTE.map(([c, n]) => `<label class="sw" title="${n}" style="--c:${c}"><input type="radio" name="${name}" value="${c}" ${selected === c ? 'checked' : ''} aria-label="${n}"/><span></span></label>`).join('')
      + (!withDefault ? `<label class="sw custom" title="Custom colour"><input type="color" value="${PALETTE.some(p => p[0] === selected) ? '#3d7bff' : esc(selected || '#3d7bff')}" aria-label="Custom colour"/><input type="radio" name="${name}" value="${!PALETTE.some(p => p[0] === selected) ? esc(selected || '') : ''}" ${selected && !PALETTE.some(p => p[0] === selected) ? 'checked' : ''}/><span></span></label>` : '');
    const custom = $('.custom', box);
    if (custom) {
      const picker = $('input[type=color]', custom), radio = $('input[type=radio]', custom);
      const sync = () => { radio.value = picker.value; custom.style.setProperty('--c', picker.value); };
      if (radio.checked) custom.style.setProperty('--c', radio.value);
      picker.addEventListener('input', () => { sync(); radio.checked = true; });
      picker.addEventListener('click', () => { sync(); radio.checked = true; });
    }
  }
  function fillRepeat(d) {
    const sel = edForm.repeat, cur = sel.value || edDraft?.repeat || 'none';
    sel.innerHTML = ['none', 'daily', 'weekdays', 'weekly', 'monthly', 'yearly'].map(k => `<option value="${k}">${esc(repeatLabel(k, d))}</option>`).join('');
    sel.value = cur;
    $('[data-until-wrap]').hidden = sel.value === 'none';
  }
  function edDates() {
    const f = edForm, allDay = f.allDay.checked;
    const sd = parse(f.startDate.value), edd = parse(f.endDate.value);
    if (!sd || !edd) return null;
    if (allDay) return { start: sd, end: addDays(edd, 1), allDay };
    const [sh, sm] = (f.startTime.value || '09:00').split(':').map(Number), [eh, em] = (f.endTime.value || '10:00').split(':').map(Number);
    return { start: addMin(sd, sh * 60 + sm), end: addMin(edd, eh * 60 + em), allDay };
  }
  function openEditor(d) {
    closePop();
    edDraft = d;
    const f = edForm;
    $('[data-ed-heading]').textContent = d.id ? 'Edit event' : 'New event';
    f.title.value = d.title || '';
    f.subtitle.value = d.subtitle || '';
    setImage(d.imageUrl || '');
    f.location.value = d.location || '';
    f.description.value = d.description || '';
    f.allDay.checked = !!d.allDay;
    f.startDate.value = isoDate(d.start);
    f.startTime.value = hhmm(d.start);
    const endShown = d.allDay ? addDays(d.end, -1) : d.end;
    f.endDate.value = isoDate(d.allDay ? endShown : d.end);
    f.endTime.value = hhmm(d.end);
    f.repeat.value = '';
    fillRepeat(d.start);
    f.repeat.value = d.repeat || 'none';
    $('[data-until-wrap]').hidden = f.repeat.value === 'none';
    f.repeatUntil.value = d.repeatUntil ? String(d.repeatUntil).slice(0, 10) : '';
    $('[data-cal-select]').innerHTML = S.cals.map(c => `<option value="${c.id}">${esc(c.name)}</option>`).join('');
    f.calendarId.value = d.calendarId || defaultCal();
    swatches($('[data-ed-colors]'), 'color', d.color || '', true);
    $('[data-ed-colors] .def').style.setProperty('--c', calOf(+f.calendarId.value)?.color || '#3d7bff');
    $('[data-series-hint]').hidden = !(d.id && d.repeat && d.repeat !== 'none');
    $('[data-ed-delete]').hidden = !d.id;
    edError('');
    toggleTimed();
    ed.hidden = false;
    setTimeout(() => f.title.focus(), 0);
  }
  const closeEditor = () => { ed.hidden = true; edDraft = null; };

  // ── event image: click / drag & drop / paste → /media/upload (same library as the Media page) ──
  const imgBox = $('[data-ed-image]'), imgFile = $('[data-ed-file]'), drop = $('[data-ed-drop]');
  let uploading = false;
  function setImage(url) {
    edForm.imageUrl.value = url || '';
    const img = $('[data-ed-img]');
    img.hidden = !url; if (url) img.src = url; else img.removeAttribute('src');
    $('[data-ed-empty]').hidden = !!url;
    $('[data-ed-tools]').hidden = !url;
    imgBox.classList.toggle('has', !!url);
  }
  async function uploadImage(file) {
    if (!file || uploading) return;
    if (!file.type.startsWith('image/')) { edError('Please choose an image file (JPG, PNG, WebP, GIF or SVG).'); return; }
    if (file.size > 10 * 1024 * 1024) { edError('Images must be 10 MB or smaller.'); return; }
    edError('');
    uploading = true; $('[data-ed-busy]').hidden = false; drop.classList.add('busy');
    try {
      const fd = new FormData(); fd.append('file', file); fd.append('__RequestVerificationToken', token());
      const r = await fetch('/media/upload?json=true', { method: 'POST', body: fd });
      let data = null; try { data = await r.json(); } catch { }
      if (!r.ok || !data?.url) throw new Error(data?.error || 'Upload failed. Please try again.');
      setImage(data.url);
    } catch (err) { edError(err.message); }
    finally { uploading = false; $('[data-ed-busy]').hidden = true; drop.classList.remove('busy'); imgFile.value = ''; }
  }
  drop.addEventListener('click', () => { if (!edForm.imageUrl.value) imgFile.click(); });
  drop.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); imgFile.click(); } });
  $('[data-ed-change]').addEventListener('click', () => imgFile.click());
  $('[data-ed-remove]').addEventListener('click', () => setImage(''));
  imgFile.addEventListener('change', () => uploadImage(imgFile.files[0]));
  drop.addEventListener('dragover', e => { e.preventDefault(); drop.classList.add('over'); });
  drop.addEventListener('dragleave', () => drop.classList.remove('over'));
  drop.addEventListener('drop', e => { e.preventDefault(); drop.classList.remove('over'); uploadImage(e.dataTransfer.files[0]); });
  ed.addEventListener('paste', e => { const f = [...(e.clipboardData?.files || [])].find(x => x.type.startsWith('image/')); if (f) { e.preventDefault(); uploadImage(f); } });
  const edError = m => { const p = $('[data-ed-error]'); p.textContent = m; p.hidden = !m; };
  const toggleTimed = () => $$('[data-timed]', edForm).forEach(i => { i.hidden = edForm.allDay.checked; });

  let lastStart = null;
  edForm.addEventListener('focusin', e => { if (e.target.name === 'startDate' || e.target.name === 'startTime') lastStart = edDates()?.start; });
  edForm.addEventListener('change', e => {
    const f = edForm, n = e.target.name;
    if (n === 'allDay') {
      toggleTimed();
      if (!f.allDay.checked && f.startTime.value === '00:00' && f.endTime.value === '00:00') {
        f.startTime.value = '09:00'; f.endTime.value = '10:00'; f.endDate.value = f.startDate.value;
      }
    }
    if (n === 'repeat') $('[data-until-wrap]').hidden = f.repeat.value === 'none';
    if (n === 'calendarId') $('[data-ed-colors] .def').style.setProperty('--c', calOf(+f.calendarId.value)?.color || '#3d7bff');
    if ((n === 'startDate' || n === 'startTime') && lastStart) {
      // keep the same length when the start changes (like Google)
      const cur = edDates(); if (!cur) return;
      const old = { start: lastStart, end: cur.end };
      const shift = minsBetween(old.start, cur.start);
      const newEnd = addMin(cur.end, shift);
      f.endDate.value = isoDate(cur.allDay ? addDays(newEnd, -1) : newEnd);
      f.endTime.value = hhmm(newEnd);
      lastStart = cur.start;
      if (n === 'startDate') fillRepeat(cur.start);
    }
  });
  edForm.addEventListener('submit', async e => {
    e.preventDefault();
    const f = edForm, dt = edDates();
    if (uploading) return edError('Please wait until the image has finished uploading.');
    if (!dt) return edError('Choose the dates.');
    if (dt.end <= dt.start) return edError('The end must be after the start.');
    const d = {
      ...edDraft, ...dt, title: f.title.value.trim(), subtitle: f.subtitle.value.trim(), imageUrl: f.imageUrl.value,
      location: f.location.value.trim(), description: f.description.value.trim(),
      calendarId: +f.calendarId.value, color: (f.querySelector('input[name=color]:checked')?.value) || '',
      repeat: f.repeat.value, repeatUntil: f.repeatUntil.value,
    };
    if (d.repeat !== 'none' && d.repeatUntil && parse(d.repeatUntil) < day0(d.start)) return edError('“Until” must be on or after the start date.');
    try { await saveDraft(d, d.id ? 'Event updated' : 'Event saved'); closeEditor(); }
    catch (err) { edError(err.message); }
  });
  $$('[data-ed-close]').forEach(b => b.addEventListener('click', closeEditor));
  ed.addEventListener('pointerdown', e => { if (e.target === ed) closeEditor(); });
  $('[data-ed-delete]').addEventListener('click', () => {
    const d = edDraft; if (!d?.id) return;
    const ev = S.events.find(x => x.id === d.id) || {
      id: d.id, calendarId: d.calendarId, title: d.title, description: d.description, location: d.location,
      seriesStart: iso(d.start), seriesEnd: iso(d.end), allDay: d.allDay, ownColor: d.color, repeat: d.repeat, repeatUntil: d.repeatUntil,
    };
    closeEditor(); deleteEvent(ev);
  });

  // ───────── calendar editor ─────────
  const cm = $('[data-calmodal]'), cmForm = $('[data-calmodal-form]');
  let cmId = null, cmArmed = false;
  function openCal(c) {
    cmId = c?.id ?? null; cmArmed = false;
    $('[data-cm-heading]').textContent = c ? 'Edit calendar' : 'New calendar';
    cmForm.name.value = c?.name || '';
    swatches($('[data-cm-colors]'), 'ccolor', c?.color || PALETTE[(S.cals.length + 3) % PALETTE.length][0], false);
    const del = $('[data-cm-delete]');
    del.hidden = !c || S.cals.length <= 1;
    del.textContent = 'Delete calendar';
    del.classList.remove('btn-danger-solid');
    cmErr('');
    cm.hidden = false;
    setTimeout(() => cmForm.name.focus(), 0);
  }
  const closeCal = () => { cm.hidden = true; };
  const cmErr = m => { const p = $('[data-cm-error]'); p.textContent = m; p.hidden = !m; };
  cmForm.addEventListener('submit', async e => {
    e.preventDefault();
    const color = cmForm.querySelector('input[name=ccolor]:checked')?.value;
    try {
      S.cals = await api('/calendar/savecalendar', { id: cmId, name: cmForm.name.value, color });
      closeCal(); toast(cmId ? 'Calendar updated' : 'Calendar added'); load();
    } catch (err) { cmErr(err.message); }
  });
  $('[data-cm-delete]').addEventListener('click', async ev => {
    const c = calOf(cmId); if (!c) return;
    if (!cmArmed) { // second click confirms
      cmArmed = true;
      ev.currentTarget.textContent = c.count ? `Delete “${c.name}” and its ${c.count} event${c.count === 1 ? '' : 's'}?` : `Delete “${c.name}”?`;
      ev.currentTarget.classList.add('btn-danger-solid');
      return;
    }
    try { S.cals = await api('/calendar/deletecalendar', { id: cmId }); closeCal(); toast('Calendar deleted'); load(); }
    catch (err) { cmErr(err.message); }
  });
  $$('[data-cm-close]').forEach(b => b.addEventListener('click', closeCal));
  cm.addEventListener('pointerdown', e => { if (e.target === cm) closeCal(); });

  // ───────── toasts ─────────
  function toast(msg, kind = 'ok', action) {
    let box = $('.toasts'); if (!box) { box = h('div', 'toasts'); document.body.append(box); }
    $$('.toast.cal-t', box).forEach(t => t.remove());
    const t = h('div', `toast ${kind} cal-t`, `${icon(kind === 'err' ? 'err' : 'ok')}<span></span>${action ? `<button type="button" class="toast-act">${esc(action.label)}</button>` : ''}<button type="button" class="toast-close" aria-label="Close">×</button>`);
    $('span', t).textContent = msg;
    t.setAttribute('role', kind === 'err' ? 'alert' : 'status');
    box.append(t);
    const close = () => { t.classList.add('hide'); setTimeout(() => t.remove(), 250); };
    $('.toast-close', t).addEventListener('click', close);
    if (action) $('.toast-act', t).addEventListener('click', async () => { close(); try { await action.fn(); } catch (err) { toast(err.message, 'err'); } });
    setTimeout(close, action ? 8000 : 4000);
  }

  // ───────── global clicks & keys ─────────
  function newEvent() {
    const n = now(), base = sameDay(S.date, today()) || S.view === 'month' && S.date.getMonth() === n.getMonth()
      ? addMin(day0(n), Math.ceil((minOfDay(n) + 1) / 30) * 30) : addMin(S.date, 9 * 60);
    const start = minOfDay(base) >= 23 * 60 ? addMin(day0(base), 23 * 60) : base;
    openEditor({ id: null, calendarId: defaultCal(), start, end: addMin(start, 60), allDay: false, repeat: 'none' });
  }

  root.addEventListener('click', e => {
    const a = e.target.closest('[data-act]'), v = e.target.closest('[data-view]'), g = e.target.closest('[data-goto]');
    if (a) {
      const act = a.dataset.act;
      if (act === 'today') go(null, today());
      if (act === 'prev') step(-1);
      if (act === 'next') step(1);
      if (act === 'create') newEvent();
      if (act === 'new-cal') openCal(null);
      if (act === 'side') root.classList.toggle('side-open');
      return;
    }
    if (v) { clearSearch(); go(v.dataset.view); return; }
    if (g) { clearSearch(); go('day', parse(g.dataset.goto)); return; }
    const md = e.target.closest('[data-mini-date]');
    if (md) { root.classList.remove('side-open'); clearSearch(); go(null, parse(md.dataset.miniDate)); return; }
    const ms = e.target.closest('[data-mini-step]');
    if (ms) { S.mini = addMonths(S.mini || new Date(S.date.getFullYear(), S.date.getMonth(), 1), +ms.dataset.miniStep); renderMini(); return; }
    const ce = e.target.closest('[data-cal-edit]');
    if (ce) openCal(calOf(+ce.dataset.calEdit));
  });
  pop.addEventListener('click', e => { const g = e.target.closest('[data-goto]'); if (g) go('day', parse(g.dataset.goto)); });

  root.addEventListener('change', async e => {
    const t = e.target.closest('[data-cal-toggle]'); if (!t) return;
    const c = calOf(+t.dataset.calToggle); if (!c) return;
    c.visible = t.checked; render();
    try { await api('/calendar/togglecalendar', { id: c.id, visible: c.visible }); } catch (err) { toast(err.message, 'err'); }
  });

  const search = $('[data-search]');
  let st;
  search.addEventListener('input', () => { clearTimeout(st); st = setTimeout(() => { S.q = search.value.trim(); load(); }, 250); });
  search.addEventListener('keydown', e => { if (e.key === 'Escape') { clearSearch(); load(); search.blur(); } });
  function clearSearch() { if (!S.q && !search.value) return; S.q = ''; search.value = ''; }

  document.addEventListener('keydown', e => {
    if (e.key === 'Escape') {
      if (!ed.hidden) { closeEditor(); return; }
      if (!cm.hidden) { closeCal(); return; }
      if (closePop()) return;
      root.classList.remove('side-open');
      return;
    }
    if (e.ctrlKey || e.metaKey || e.altKey || !ed.hidden || !cm.hidden) return;
    if (e.target.closest('input,textarea,select,[contenteditable]') || document.querySelector('[data-cmdk].open')) return;
    const k = e.key.toLowerCase();
    const map = { d: () => go('day'), w: () => go('week'), m: () => go('month'), a: () => go('schedule'), t: () => go(null, today()), j: () => step(1), n: () => step(1), k: () => step(-1), p: () => step(-1), c: newEvent, '/': () => search.focus() };
    if (map[k]) { e.preventDefault(); closePop(); if ('dwma'.includes(k)) clearSearch(); map[k](); }
  });

  // Keep the red "now" line and month layout fresh.
  setInterval(() => { const l = $('.tg-col.today .now'); if (l) l.style.top = minOfDay(now()) / 60 * HOUR + 'px'; }, 60000);
  let rt; addEventListener('resize', () => { clearTimeout(rt); rt = setTimeout(() => { if (S.view === 'month' && !S.q) render(); closePop(); }, 150); });

  // Opened from the command palette as /calendar?new=1
  if (new URLSearchParams(location.search).get('new')) { history.replaceState(null, '', '/calendar'); setTimeout(newEvent, 50); }
  load();
})();
