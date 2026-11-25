// Dashboard — small month calendar. Hover a date for a short list of its events; click an event for full details.
(function () {
  'use strict';
  const card = document.querySelector('[data-dcal]');
  if (!card) return;

  const WEEK_START = 0; // keep the same as calendar.js (0 = Sunday)
  const cals = window.DCAL_CALS || [];
  const calOf = id => cals.find(c => c.id === id);
  const grid = card.querySelector('[data-dcal-grid]');
  const foot = card.querySelector('[data-dcal-foot]');
  const monthBtn = card.querySelector('[data-dcal-today]');
  const tip = document.querySelector('[data-dcal-tip]');
  const modal = document.querySelector('[data-dcal-modal]');

  // ── dates ──
  const pad = n => String(n).padStart(2, '0');
  const day0 = d => new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const addDays = (d, n) => { const x = new Date(d); x.setDate(x.getDate() + n); return x; };
  const sameDay = (a, b) => a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  const startOfWeek = d => { const x = day0(d); x.setDate(x.getDate() - ((x.getDay() - WEEK_START + 7) % 7)); return x; };
  const iso = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:00`;
  const isoDate = d => iso(d).slice(0, 10);
  const parse = s => { const m = /^(\d{4})-(\d\d)-(\d\d)(?:T(\d\d):(\d\d))?/.exec(s || ''); return m ? new Date(+m[1], m[2] - 1, +m[3], +(m[4] || 0), +(m[5] || 0)) : null; };
  const lastDay = e => day0(new Date(e.e.getTime() - 60000));
  const F = (d, o) => d.toLocaleDateString(undefined, o);
  const fTime = d => d.toLocaleTimeString(undefined, d.getMinutes() ? { hour: 'numeric', minute: '2-digit' } : { hour: 'numeric' });
  const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  function timeOn(e, d) { // what to show as the time on a given day
    if (e.allDay) return 'All day';
    const next = addDays(d, 1);
    if (e.s <= d && e.e >= next) return 'All day';
    if (e.s < d) return 'Until ' + fTime(e.e);
    if (e.e > next) return fTime(e.s) + ' →';
    return fTime(e.s) + ' – ' + fTime(e.e);
  }
  function whenText(e) {
    const ld = lastDay(e), long = { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' };
    if (e.allDay) return sameDay(e.s, ld) ? F(e.s, long) + ' · All day' : `${F(e.s, { month: 'short', day: 'numeric' })} – ${F(ld, { month: 'short', day: 'numeric', year: 'numeric' })} · All day`;
    if (sameDay(e.s, ld)) return `${F(e.s, long)} · ${fTime(e.s)} – ${fTime(e.e)}`;
    return `${F(e.s, { month: 'short', day: 'numeric' })}, ${fTime(e.s)} – ${F(e.e, { month: 'short', day: 'numeric' })}, ${fTime(e.e)}`;
  }
  function repeatText(e) {
    const d = parse(e.seriesStart), u = parse(e.repeatUntil);
    const t = {
      daily: 'Repeats every day', weekdays: 'Repeats every weekday (Mon–Fri)', weekly: `Repeats weekly on ${F(d, { weekday: 'long' })}`,
      monthly: `Repeats monthly on day ${d.getDate()}`, yearly: `Repeats every year on ${F(d, { month: 'long', day: 'numeric' })}`,
    }[e.repeat];
    return t ? t + (u ? `, until ${F(u, { month: 'short', day: 'numeric', year: 'numeric' })}` : '') : '';
  }

  // ── state & loading ──
  let month = new Date(new Date().getFullYear(), new Date().getMonth(), 1);
  let events = [];
  let seq = 0;

  function range() {
    const s = startOfWeek(month);
    const last = new Date(month.getFullYear(), month.getMonth() + 1, 0);
    const weeks = Math.ceil((Math.round((last - s) / 864e5) + 1) / 7);
    return [s, addDays(s, weeks * 7)];
  }

  async function load() {
    const [a, b] = range(), my = ++seq;
    render(); // draw the grid straight away, dots arrive after
    try {
      const r = await fetch(`/calendar/events?from=${iso(a)}&to=${iso(b)}`, { headers: { Accept: 'application/json' } });
      if (!r.ok || (r.redirected && r.url.includes('/account/login'))) throw new Error();
      const list = await r.json();
      if (my !== seq) return;
      events = list.filter(e => calOf(e.calendarId)?.visible !== false)
        .map(e => ({ ...e, s: parse(e.start), e: parse(e.end) }))
        .sort((x, y) => +y.allDay - +x.allDay || x.s - y.s);
      render();
    } catch {
      if (my !== seq) return;
      events = [];
      foot.textContent = 'Couldn’t load events.';
    }
  }

  const eventsOn = d => { const next = addDays(d, 1); return events.filter(e => e.s < next && e.e > d); };

  function render() {
    const [a, b] = range(), t = day0(new Date());
    monthBtn.textContent = F(month, { month: 'long', year: 'numeric' });
    let html = '';
    for (let i = 0; i < 7; i++) html += `<span class="dcal-dow">${esc(F(addDays(a, i), { weekday: 'narrow' }))}</span>`;
    for (let d = a; d < b; d = addDays(d, 1)) {
      const list = eventsOn(d);
      const colors = [...new Set(list.map(e => e.color))].slice(0, 3);
      const cls = ['dcal-day', d.getMonth() !== month.getMonth() && 'out', sameDay(d, t) && 'today', list.length && 'has', d < t && 'past'].filter(Boolean).join(' ');
      const label = F(d, { weekday: 'long', month: 'long', day: 'numeric' }) + (list.length ? ` · ${list.length} event${list.length === 1 ? '' : 's'}` : '');
      html += `<button type="button" class="${cls}" data-day="${isoDate(d)}" aria-label="${esc(label)}"><span>${d.getDate()}</span><i class="dcal-dots">${colors.map(c => `<b style="--c:${esc(c)}"></b>`).join('')}</i></button>`;
    }
    grid.innerHTML = html;

    // footer: count for this month + next event from now
    const inMonth = events.filter(e => e.s.getMonth() === month.getMonth() && e.s.getFullYear() === month.getFullYear()).length;
    const now = new Date();
    const next = events.filter(e => !e.allDay ? e.s > now : e.s >= t).sort((x, y) => x.s - y.s)[0];
    foot.innerHTML = `<span><b>${inMonth}</b> event${inMonth === 1 ? '' : 's'} this month</span>` +
      (next ? `<button type="button" class="dcal-next" data-next>Next: <b>${esc(next.title)}</b> · ${esc(sameDay(next.s, t) ? (next.allDay ? 'today' : fTime(next.s)) : F(next.s, { month: 'short', day: 'numeric' }))}</button>` : '');
    if (next) foot.querySelector('[data-next]')._ev = next;
  }

  // ── hover card ──
  let hideTimer = null, tipDay = null, pinned = false;
  function showTip(btn, pin) {
    const d = parse(btn.dataset.day), list = eventsOn(d);
    clearTimeout(hideTimer);
    if (!list.length) { hideTip(); return; }
    if (tipDay === btn.dataset.day && !tip.hidden) { pinned = pinned || pin; return; }
    tipDay = btn.dataset.day; pinned = !!pin;
    const max = 5;
    tip.innerHTML = `<div class="dt-head">${esc(F(d, { weekday: 'long', month: 'short', day: 'numeric' }))}</div>` +
      list.slice(0, max).map((e, i) => `<button type="button" class="dt-ev" data-i="${i}" style="--c:${esc(e.color)}"><i></i><span class="dt-time">${esc(timeOn(e, d))}</span><span class="dt-title">${esc(e.title)}</span></button>`).join('') +
      (list.length > max ? `<a class="dt-more" href="/calendar#/day/${isoDate(d)}">+${list.length - max} more in calendar</a>` : '');
    tip._list = list;
    tip.hidden = false;
    // position under (or above) the date
    const r = btn.getBoundingClientRect(), w = tip.offsetWidth, hgt = tip.offsetHeight, m = 8;
    let x = Math.min(Math.max(m, r.left + r.width / 2 - w / 2), innerWidth - w - m);
    let y = r.bottom + 6;
    if (y + hgt > innerHeight - m) y = Math.max(m, r.top - hgt - 6);
    tip.style.left = x + 'px'; tip.style.top = y + 'px';
  }
  function hideTip() { tip.hidden = true; tipDay = null; pinned = false; }
  const hideSoon = () => { if (pinned) return; clearTimeout(hideTimer); hideTimer = setTimeout(hideTip, 180); };

  grid.addEventListener('pointerover', e => { const b = e.target.closest('.dcal-day'); if (b && e.pointerType === 'mouse') showTip(b, false); });
  grid.addEventListener('pointerout', e => { if (e.pointerType === 'mouse' && !e.relatedTarget?.closest?.('.dcal-day')) hideSoon(); });
  grid.addEventListener('focusin', e => { const b = e.target.closest('.dcal-day'); if (b) showTip(b, false); });
  grid.addEventListener('click', e => {
    const b = e.target.closest('.dcal-day'); if (!b) return;
    if (!eventsOn(parse(b.dataset.day)).length) { location.href = '/calendar#/day/' + b.dataset.day; return; }
    showTip(b, true); // tap / click keeps the card open
  });
  tip.addEventListener('pointerenter', () => clearTimeout(hideTimer));
  tip.addEventListener('pointerleave', hideSoon);
  tip.addEventListener('click', e => { const b = e.target.closest('.dt-ev'); if (b) { const ev = tip._list[+b.dataset.i]; hideTip(); openDetail(ev); } });
  document.addEventListener('pointerdown', e => { if (!tip.hidden && !tip.contains(e.target) && !e.target.closest('.dcal-day')) hideTip(); });
  addEventListener('scroll', () => { if (!tip.hidden) hideTip(); }, { passive: true });

  // ── full details popup ──
  const svg = p => `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round">${p}</svg>`;
  const IC = {
    pin: '<path d="M12 21s-7-6.1-7-11.5A7 7 0 0 1 19 9.5C19 14.9 12 21 12 21z"/><circle cx="12" cy="9.5" r="2.5"/>',
    text: '<path d="M4 6h16M4 12h16M4 18h10"/>',
    cal: '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>',
  };
  function openDetail(e) {
    const $ = s => modal.querySelector(s);
    $('[data-dd-color]').style.background = e.color;
    $('[data-dd-title]').textContent = e.title;
    $('[data-dd-when]').textContent = whenText(e);
    const rep = repeatText(e);
    $('[data-dd-repeat]').textContent = rep;
    $('[data-dd-repeat]').hidden = !rep;
    let rows = '';
    if (e.location) {
      const loc = /^https?:\/\//i.test(e.location) ? `<a href="${esc(e.location)}" target="_blank" rel="noopener noreferrer">${esc(e.location)}</a>` : esc(e.location);
      rows += `<div class="dd-row">${svg(IC.pin)}<span>${loc}</span></div>`;
    }
    if (e.description) rows += `<div class="dd-row">${svg(IC.text)}<span class="dd-desc">${esc(e.description)}</span></div>`;
    rows += `<div class="dd-row">${svg(IC.cal)}<span>${esc(calOf(e.calendarId)?.name || '')}</span></div>`;
    $('[data-dd-rows]').innerHTML = rows;
    $('[data-dd-open]').href = '/calendar#/day/' + isoDate(e.s);
    modal.hidden = false;
    $('[data-dd-close]').focus();
  }
  const closeDetail = () => { modal.hidden = true; };
  modal.querySelectorAll('[data-dd-close]').forEach(b => b.addEventListener('click', closeDetail));
  modal.addEventListener('click', e => { if (e.target === modal) closeDetail(); });
  document.addEventListener('keydown', e => { if (e.key === 'Escape') { if (!modal.hidden) closeDetail(); else hideTip(); } });
  foot.addEventListener('click', e => { const n = e.target.closest('[data-next]'); if (n?._ev) openDetail(n._ev); });

  // ── month navigation ──
  card.addEventListener('click', e => {
    const s = e.target.closest('[data-dcal-step]');
    if (s) { month = new Date(month.getFullYear(), month.getMonth() + +s.dataset.dcalStep, 1); hideTip(); load(); }
    if (e.target.closest('[data-dcal-today]')) { const n = new Date(); month = new Date(n.getFullYear(), n.getMonth(), 1); hideTip(); load(); }
  });

  load();
})();
