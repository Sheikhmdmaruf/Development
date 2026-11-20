// Portfolio Admin — client behaviour. No framework; each feature is opt-in via data-* attributes.
(() => {
  const $ = (s, el = document) => el.querySelector(s);
  const $$ = (s, el = document) => [...el.querySelectorAll(s)];
  const csrf = () => $('meta[name="csrf-token"]')?.content ?? '';
  const icons = (root) => window.lucide?.createIcons({ attrs: {}, nameAttr: 'data-lucide', root });
  const esc = (s) => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  /** fetch with CSRF header + JSON handling */
  async function api(url, opts = {}) {
    const headers = { 'X-CSRF-TOKEN': csrf(), Accept: 'application/json', ...(opts.headers || {}) };
    if (opts.json !== undefined) { headers['Content-Type'] = 'application/json'; opts.body = JSON.stringify(opts.json); }
    const res = await fetch(url, { credentials: 'same-origin', ...opts, headers });
    if (res.status === 401) { location.href = '/login?returnUrl=' + encodeURIComponent(location.pathname); throw new Error('Signed out'); }
    const type = res.headers.get('content-type') || '';
    const data = type.includes('json') ? await res.json() : await res.text();
    if (!res.ok) throw new Error(data?.error || data?.title || `Request failed (${res.status})`);
    return data;
  }
  window.pa = { api, toast };

  // ---------------------------------------------------------------- toasts
  function toast(message, kind = 'success') {
    let box = $('.toasts');
    if (!box) { box = document.createElement('div'); box.className = 'toasts'; box.setAttribute('role', 'status'); document.body.append(box); }
    const t = document.createElement('div');
    t.className = 'toast ' + kind;
    t.innerHTML = `<i data-lucide="${kind === 'error' ? 'circle-alert' : 'circle-check'}"></i><span>${esc(message)}</span>`;
    box.append(t); icons(t);
    setTimeout(() => { t.classList.add('hide'); setTimeout(() => t.remove(), 300); }, kind === 'error' ? 6000 : 3500);
  }
  $$('[data-toast]').forEach(el => toast(el.dataset.toast, el.dataset.kind));

  // ---------------------------------------------------------------- theme
  function syncThemeButtons() {
    const pref = document.documentElement.getAttribute('data-theme-pref');
    $$('[data-theme-set]').forEach(b => b.classList.toggle('active', b.dataset.themeSet === pref));
    const t = $('[data-theme-toggle] i, [data-theme-toggle] svg');
    if (t) { const dark = document.documentElement.dataset.theme === 'dark'; const btn = $('[data-theme-toggle]'); btn.innerHTML = `<i data-lucide="${dark ? 'sun' : 'moon'}"></i>`; icons(btn); }
  }
  document.addEventListener('click', e => {
    const set = e.target.closest('[data-theme-set]');
    if (set) { window.__setTheme(set.dataset.themeSet); }
    if (e.target.closest('[data-theme-toggle]')) window.__setTheme(document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark');
  });
  document.addEventListener('themechange', () => { syncThemeButtons(); renderCharts(); });

  // ---------------------------------------------------------------- navigation layout (dock / sidebar / top)
  function syncLayoutButtons() {
    const cur = ['dock', 'sidebar', 'topnav'].find(l => document.documentElement.classList.contains('layout-' + l));
    $$('[data-layout-set]').forEach(b => b.classList.toggle('active', b.dataset.layoutSet === cur));
  }
  document.addEventListener('click', e => { const b = e.target.closest('[data-layout-set]'); if (b) window.__setLayout(b.dataset.layoutSet); });
  document.addEventListener('layoutchange', () => { syncLayoutButtons(); setTimeout(renderCharts, 300); });
  document.addEventListener('DOMContentLoaded', syncLayoutButtons);
  if (document.readyState !== 'loading') syncLayoutButtons();

  // ---------------------------------------------------------------- glass spotlight follows the pointer
  document.addEventListener('pointermove', e => {
    const card = e.target.closest?.('.card');
    if (!card) return;
    const r = card.getBoundingClientRect();
    card.style.setProperty('--mx', `${e.clientX - r.left}px`);
    card.style.setProperty('--my', `${e.clientY - r.top}px`);
  }, { passive: true });

  // ---------------------------------------------------------------- sidebar
  document.addEventListener('click', e => {
    if (e.target.closest('[data-sidebar-collapse]')) {
      const c = document.documentElement.classList.toggle('sidebar-collapsed');
      try { localStorage.setItem('pa.sidebar', c ? 'collapsed' : 'open'); } catch { }
    }
    if (e.target.closest('[data-sidebar-open]')) $('.app').classList.add('nav-open');
    if (e.target.closest('.scrim')) $('.app').classList.remove('nav-open');
  });

  // ---------------------------------------------------------------- dropdowns
  document.addEventListener('click', e => {
    const trigger = e.target.closest('[data-dropdown-toggle]');
    $$('.dropdown.open').forEach(d => { if (!d.contains(e.target) || (trigger && d !== trigger.closest('.dropdown'))) d.classList.remove('open'); });
    if (trigger) {
      const dd = trigger.closest('.dropdown');
      const open = dd.classList.toggle('open');
      trigger.setAttribute('aria-expanded', open);
      if (open) dd.dispatchEvent(new CustomEvent('dropdown:open'));
    }
  });
  document.addEventListener('keydown', e => { if (e.key === 'Escape') $$('.dropdown.open').forEach(d => d.classList.remove('open')); });

  // ---------------------------------------------------------------- dialogs
  document.addEventListener('click', e => {
    const open = e.target.closest('[data-dialog-open]');
    if (open) { const d = document.getElementById(open.dataset.dialogOpen); d?.showModal(); $('[autofocus]', d)?.focus(); }
    const close = e.target.closest('[data-dialog-close]');
    if (close) close.closest('dialog')?.close();
    if (e.target.tagName === 'DIALOG' && e.target.classList.contains('modal')) e.target.close(); // click on backdrop
  });

  // ---------------------------------------------------------------- confirm (styled, no window.confirm)
  const confirmDlg = $('#confirm-dialog');
  let confirmed = null;
  document.addEventListener('submit', e => {
    const form = e.target;
    const msg = form.dataset.confirm || e.submitter?.dataset.confirm;
    if (!msg || confirmed === form) { confirmed = null; markLoading(e.submitter); return; }
    e.preventDefault();
    $('#confirm-text', confirmDlg).textContent = msg;
    const ok = $('#confirm-ok', confirmDlg);
    ok.textContent = form.dataset.confirmLabel || e.submitter?.dataset.confirmLabel || 'Confirm';
    ok.onclick = () => { confirmDlg.close(); confirmed = form; e.submitter ? form.requestSubmit(e.submitter) : form.requestSubmit(); };
    confirmDlg.showModal();
  }, true);
  function markLoading(btn) { if (btn && !btn.hasAttribute('formnovalidate')) { btn.classList.add('loading'); setTimeout(() => btn.classList.remove('loading'), 8000); } }

  // auto-submit filters
  document.addEventListener('change', e => { if (e.target.matches('[data-autosubmit]')) e.target.form.requestSubmit(); });

  // select-all + bulk bar
  document.addEventListener('change', e => {
    if (e.target.matches('[data-check-all]')) $$('[data-check-item]').forEach(c => c.checked = e.target.checked);
    if (e.target.matches('[data-check-all], [data-check-item]')) {
      const n = $$('[data-check-item]:checked').length;
      const bar = $('[data-bulkbar]');
      if (bar) { bar.hidden = n === 0; $('[data-bulk-count]', bar).textContent = n; }
    }
  });

  // copy to clipboard
  document.addEventListener('click', async e => {
    const b = e.target.closest('[data-copy]');
    if (!b) return;
    const text = b.dataset.copy || $(b.dataset.copyFrom)?.textContent || '';
    try { await navigator.clipboard.writeText(text.trim()); toast('Copied to clipboard'); } catch { toast('Could not copy', 'error'); }
  });

  // slug from title
  $$('[data-slug-from]').forEach(slug => {
    const src = $(slug.dataset.slugFrom);
    let touched = slug.value.length > 0;
    slug.addEventListener('input', () => touched = slug.value.length > 0);
    src?.addEventListener('input', () => {
      if (!touched) slug.value = src.value.toLowerCase().replace(/&/g, 'and').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
    });
  });

  // ---------------------------------------------------------------- notifications
  const bell = $('[data-notifications]');
  async function refreshCount() {
    if (!bell || document.hidden) return;
    try {
      const { unread } = await api('/notifications/count');
      let dot = $('.dot', bell.querySelector('.icon-btn'));
      if (unread > 0) {
        if (!dot) { dot = document.createElement('span'); dot.className = 'dot'; bell.querySelector('.icon-btn').append(dot); }
        dot.textContent = unread > 99 ? '99+' : unread;
      } else dot?.remove();
    } catch { }
  }
  if (bell) {
    bell.addEventListener('dropdown:open', async () => {
      const list = $('.notif-list', bell);
      try { list.innerHTML = await api('/notifications/dropdown', { headers: { Accept: 'text/html' } }); icons(list); }
      catch { list.innerHTML = '<div class="empty small">Could not load notifications.</div>'; }
    });
    setInterval(refreshCount, 60000);
    document.addEventListener('visibilitychange', refreshCount);
  }

  // ---------------------------------------------------------------- command palette (Ctrl/Cmd + K)
  const pal = $('#palette');
  if (pal) {
    const input = $('input', pal), results = $('.palette-results', pal);
    let timer, sel = 0;
    const open = () => { pal.showModal(); input.value = ''; render(null); input.focus(); };
    document.addEventListener('keydown', e => { if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); open(); } });
    document.addEventListener('click', e => { if (e.target.closest('[data-palette-open]')) open(); });
    input.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(search, 180); });
    input.addEventListener('keydown', e => {
      const links = $$('a', results);
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault(); sel = (sel + (e.key === 'ArrowDown' ? 1 : -1) + links.length) % Math.max(links.length, 1);
        links.forEach((a, i) => a.classList.toggle('sel', i === sel)); links[sel]?.scrollIntoView({ block: 'nearest' });
      }
      if (e.key === 'Enter' && links[sel]) { e.preventDefault(); location.href = links[sel].href; }
    });
    async function search() {
      const q = input.value.trim();
      if (q.length < 2) return render(null);
      try { render(await api('/search?q=' + encodeURIComponent(q))); } catch { results.innerHTML = '<div class="palette-empty">Search failed.</div>'; }
    }
    function render(groups) {
      sel = 0;
      if (!groups) {
        results.innerHTML = $('#palette-default').innerHTML; icons(results);
        $$('a', results)[0]?.classList.add('sel'); return;
      }
      const html = groups.filter(g => g.items.length).map(g => `<div class="group eyebrow">${esc(g.label)}</div>` +
        g.items.map(i => `<a href="${esc(i.url)}"><i data-lucide="${esc(i.icon)}"></i><div class="grow"><div class="truncate">${esc(i.title)}</div><div class="sub truncate">${esc(i.sub)}</div></div></a>`).join('')).join('');
      results.innerHTML = html || '<div class="palette-empty">No results.</div>';
      icons(results); $$('a', results)[0]?.classList.add('sel');
    }
  }

  // Images that live on the portfolio host (/images/...) can't load here: fall back to the placeholder.
  document.addEventListener('error', e => {
    const img = e.target;
    if (img.tagName === 'IMG' && img.closest('.preview, .thumb, .avatar')) {
      const prev = img.parentElement; img.remove();
      if (prev.classList.contains('preview')) { prev.innerHTML = '<i data-lucide="image-off" title="Image is on the portfolio site"></i>'; icons(prev); }
    }
  }, true);

  // ---------------------------------------------------------------- image upload fields
  document.addEventListener('change', async e => {
    const input = e.target.closest('[data-upload] input[type=file]');
    if (!input?.files?.length) return;
    const box = input.closest('[data-upload]'), hidden = $('input[type=hidden], input[data-upload-value]', box), prev = $('.preview', box);
    const fd = new FormData(); fd.append('file', input.files[0]);
    box.classList.add('loading');
    try {
      const r = await api('/media/upload', { method: 'POST', body: fd });
      hidden.value = r.path; prev.innerHTML = `<img src="${esc(r.path)}" alt="">`; toast('Image uploaded');
      hidden.dispatchEvent(new Event('change', { bubbles: true }));
    } catch (err) { toast(err.message, 'error'); }
    finally { box.classList.remove('loading'); input.value = ''; }
  });
  document.addEventListener('click', e => {
    const clear = e.target.closest('[data-upload-clear]');
    if (!clear) return;
    const box = clear.closest('[data-upload]');
    $('input[data-upload-value]', box).value = ''; $('.preview', box).innerHTML = '<i data-lucide="image"></i>'; icons(box);
  });
  // typed path -> preview
  document.addEventListener('input', e => {
    if (!e.target.matches('[data-upload-value]')) return;
    const prev = $('.preview', e.target.closest('[data-upload]')), v = e.target.value.trim();
    prev.innerHTML = v ? `<img src="${esc(v)}" alt="">` : '<i data-lucide="image"></i>'; icons(prev);
  });

  // ---------------------------------------------------------------- repeaters (lists stored as JSON)
  // <div data-repeater data-shape="object|tuple" data-fields='[{"key":"label","label":"Label"},...]'> + hidden input[data-repeater-value]
  $$('[data-repeater]').forEach(rep => {
    const fields = JSON.parse(rep.dataset.fields), shape = rep.dataset.shape || 'object';
    const hidden = document.getElementById(rep.dataset.target);
    const list = $('.repeater', rep);
    let items = [];
    try { items = JSON.parse(hidden.value || '[]') || []; } catch { items = []; }

    const valueOf = (item, f, i) => {
      const v = shape === 'tuple' ? item?.[i] : shape === 'string' ? item : item?.[f.key];
      return Array.isArray(v) ? v.join('\n\n') : (v ?? '');
    };
    function row(item) {
      const el = document.createElement('div'); el.className = 'rep-item';
      el.innerHTML = `<div class="rep-tools">
          <button type="button" class="btn btn-text btn-sm btn-icon" data-rep-up title="Move up"><i data-lucide="arrow-up"></i></button>
          <button type="button" class="btn btn-text btn-sm btn-icon" data-rep-down title="Move down"><i data-lucide="arrow-down"></i></button>
          <button type="button" class="btn btn-text btn-sm btn-icon" data-rep-del title="Remove"><i data-lucide="trash-2"></i></button></div>
        <div class="rep-grid">${fields.map((f, i) => `<label class="field" style="${f.wide ? 'grid-column:1/-1' : ''}"><span class="label">${esc(f.label)}</span>${
          f.multiline ? `<textarea class="textarea" data-k="${i}" rows="${f.rows || 3}" placeholder="${esc(f.placeholder || '')}">${esc(valueOf(item, f, i))}</textarea>`
          : f.options ? `<select class="select input-sm" data-k="${i}">${f.options.map(o => `<option ${o === valueOf(item, f, i) ? 'selected' : ''}>${esc(o)}</option>`).join('')}</select>`
          : `<input class="input input-sm" data-k="${i}" value="${esc(valueOf(item, f, i))}" placeholder="${esc(f.placeholder || '')}">`}</label>`).join('')}</div>`;
      return el;
    }
    function serialize() {
      const out = $$('.rep-item', list).map(el => {
        const vals = fields.map((f, i) => { const v = $(`[data-k="${i}"]`, el).value.trim(); return f.paragraphs ? v.split(/\n\s*\n/).map(s => s.trim()).filter(Boolean) : v; });
        if (shape === 'tuple') return vals;
        if (shape === 'string') return vals[0];
        return Object.fromEntries(fields.map((f, i) => [f.key, vals[i]]));
      }).filter(v => shape === 'string' ? v : (Array.isArray(v) ? v : Object.values(v)).some(x => Array.isArray(x) ? x.length : x));
      hidden.value = out.length ? JSON.stringify(out) : '';
    }
    items.forEach(i => list.append(row(i)));
    icons(list);
    $('[data-rep-add]', rep).addEventListener('click', () => { const r = row(null); list.append(r); icons(r); $('input,textarea', r)?.focus(); serialize(); });
    list.addEventListener('click', e => {
      const item = e.target.closest('.rep-item'); if (!item) return;
      if (e.target.closest('[data-rep-del]')) item.remove();
      if (e.target.closest('[data-rep-up]') && item.previousElementSibling) item.after(item.previousElementSibling);
      if (e.target.closest('[data-rep-down]') && item.nextElementSibling) item.before(item.nextElementSibling);
      serialize();
    });
    list.addEventListener('input', serialize);
    list.addEventListener('change', serialize);
  });

  // live SVG icon preview (server sanitises on save)
  $$('[data-svg-input]').forEach(t => {
    const prev = $(t.dataset.svgInput);
    const draw = () => { const doc = new DOMParser().parseFromString(`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">${t.value}</svg>`, 'image/svg+xml');
      prev.replaceChildren(); if (!doc.querySelector('parsererror')) { const s = doc.documentElement; s.querySelectorAll('script,foreignObject').forEach(n => n.remove()); [...s.querySelectorAll('*')].forEach(n => [...n.attributes].forEach(a => a.name.startsWith('on') && n.removeAttribute(a.name))); prev.append(document.importNode(s, true)); } };
    t.addEventListener('input', draw); draw();
  });

  // ---------------------------------------------------------------- drag-to-reorder tables (CMS)
  $$('[data-sortable]').forEach(tbody => {
    let dragged = null;
    tbody.addEventListener('dragstart', e => { dragged = e.target.closest('tr'); dragged.classList.add('dragging'); e.dataTransfer.effectAllowed = 'move'; });
    tbody.addEventListener('dragover', e => {
      e.preventDefault(); const over = e.target.closest('tr'); if (!over || over === dragged) return;
      const r = over.getBoundingClientRect(); over[(e.clientY - r.top) > r.height / 2 ? 'after' : 'before'](dragged);
    });
    tbody.addEventListener('dragend', async () => {
      dragged.classList.remove('dragging');
      const ids = $$('tr[data-id]', tbody).map(tr => +tr.dataset.id);
      try { await api(tbody.dataset.sortable, { method: 'POST', json: { ids } }); toast('Order saved'); } catch (err) { toast(err.message, 'error'); }
    });
  });

  // ---------------------------------------------------------------- kanban (leads board)
  $$('[data-board]').forEach(board => {
    let card = null;
    board.addEventListener('dragstart', e => { card = e.target.closest('.board-card'); card?.classList.add('dragging'); });
    board.addEventListener('dragend', () => { card?.classList.remove('dragging'); $$('.drag-over', board).forEach(c => c.classList.remove('drag-over')); });
    board.addEventListener('dragover', e => {
      const col = e.target.closest('.board-col-body'); if (!col || !card) return;
      e.preventDefault(); $$('.drag-over', board).forEach(c => c !== col && c.classList.remove('drag-over')); col.classList.add('drag-over');
      const after = $$('.board-card:not(.dragging)', col).find(c => e.clientY < c.getBoundingClientRect().top + c.offsetHeight / 2);
      after ? col.insertBefore(card, after) : col.append(card);
    });
    board.addEventListener('drop', async e => {
      const col = e.target.closest('.board-col-body'); if (!col || !card) return;
      e.preventDefault(); col.classList.remove('drag-over');
      const ids = $$('.board-card', col).map(c => +c.dataset.id);
      $$('.board-col', board).forEach(c => { $('[data-col-count]', c).textContent = $$('.board-card', c).length; });
      try { await api(board.dataset.board, { method: 'POST', json: { id: +card.dataset.id, status: col.dataset.status, order: ids } }); }
      catch (err) { toast(err.message, 'error'); setTimeout(() => location.reload(), 800); }
    });
  });

  // ---------------------------------------------------------------- charts (Chart.js, themed from CSS tokens)
  const charts = [];
  function token(name) { return getComputedStyle(document.documentElement).getPropertyValue(name).trim(); }
  function renderCharts() {
    if (!window.Chart) return;
    charts.splice(0).forEach(c => c.destroy());
    $$('canvas[data-chart]').forEach(canvas => {
      const spec = JSON.parse($(`#${canvas.dataset.chart}`).textContent);
      const ink2 = token('--ink-2'), grid = token('--chart-grid'), axis = token('--chart-axis'), surface = token('--surface-solid');
      const horizontal = spec.horizontal === true;
      const datasets = spec.series.map(s => ({
        label: s.label, data: s.data, backgroundColor: token(s.color || '--series-1'), borderColor: token(s.color || '--series-1'),
        borderRadius: 4, borderSkipped: 'start', maxBarThickness: horizontal ? 18 : 28, categoryPercentage: .7, barPercentage: .9,
        borderWidth: spec.type === 'line' ? 2 : 0, pointRadius: spec.type === 'line' ? 0 : undefined, pointHoverRadius: 5, tension: .3,
        fill: spec.type === 'line' ? { target: 'origin', above: token(s.color || '--series-1') + '1a' } : undefined,
      }));
      charts.push(new Chart(canvas, {
        type: spec.type || 'bar',
        data: { labels: spec.labels, datasets },
        options: {
          maintainAspectRatio: false, indexAxis: horizontal ? 'y' : 'x', animation: { duration: 400 },
          interaction: { mode: 'index', intersect: false },
          plugins: {
            legend: { display: false },
            tooltip: { backgroundColor: surface, titleColor: token('--ink'), bodyColor: ink2, borderColor: token('--border-strong'), borderWidth: 1, padding: 10, cornerRadius: 8, displayColors: datasets.length > 1, boxPadding: 4 },
          },
          scales: {
            x: { grid: { display: horizontal, color: grid, drawTicks: false }, border: { display: false }, ticks: { color: axis, font: { size: 11 }, precision: 0, padding: 6 }, beginAtZero: true },
            y: { grid: { display: !horizontal, color: grid, drawTicks: false }, border: { display: false }, ticks: { color: horizontal ? ink2 : axis, font: { size: 11 }, precision: 0, padding: 8 }, beginAtZero: true },
          },
        },
      }));
    });
  }

  document.addEventListener('DOMContentLoaded', () => { icons(); syncThemeButtons(); renderCharts(); });
  if (document.readyState !== 'loading') { icons(); syncThemeButtons(); renderCharts(); }
})();
