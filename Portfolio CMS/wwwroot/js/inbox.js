// Portfolio CMS — Inbox helpers (composer, keyboard shortcuts).
(function () {
  'use strict';
  const ol = document.querySelector('[data-ol]');
  if (!ol) return;

  // Keep the selected message visible in the list.
  ol.querySelector('.ol-item.sel')?.scrollIntoView({ block: 'nearest' });

  // ── composer ──
  const form = ol.querySelector('[data-ol-compose]');
  if (form) {
    const body = form.querySelector('[data-oc-body]');
    const send = form.querySelector('[data-oc-send]');
    const start = body.value;
    let sending = false;

    // Reply / forward: cursor at the top, above the quoted message (like Outlook).
    const to = form.querySelector('input[name=To]');
    setTimeout(() => {
      if (!to.value) { to.focus(); return; }
      body.focus(); body.setSelectionRange(0, 0); body.scrollTop = 0;
    }, 0);
    const grow = () => { body.style.height = 'auto'; body.style.height = Math.min(Math.max(body.scrollHeight + 2, 220), 640) + 'px'; };
    body.addEventListener('input', grow); grow();

    form.querySelector('[data-oc-cc-toggle]')?.addEventListener('click', e => {
      e.currentTarget.hidden = true;
      const cc = form.querySelector('[data-oc-cc]'); cc.hidden = false; cc.querySelector('input').focus();
    });
    // ── attachments: Attach button, drag & drop or paste → uploaded straight away to /inbox/attach ──
    const MAX = 30 * 1024 * 1024;
    const OK = ['jpg', 'jpeg', 'png', 'gif', 'doc', 'docx', 'xls', 'xlsx', 'zip', 'rar', 'mp4', 'mov', 'avi', 'mkv', 'webm', 'wmv', 'm4v'];
    const box = form.querySelector('[data-oc-files]'), list = form.querySelector('[data-oc-list]'), total = form.querySelector('[data-oc-total]');
    const picker = form.querySelector('[data-oc-file]'), drop = form.querySelector('[data-oc-drop]');
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const fmt = b => b >= 1048576 ? (b / 1048576).toFixed(1).replace(/\.0$/, '') + ' MB' : Math.max(1, Math.round(b / 1024)) + ' KB';
    const ext = n => (n.split('.').pop() || '').toLowerCase();
    const kind = e => ['jpg', 'jpeg', 'png', 'gif'].includes(e) ? 'img' : ['doc', 'docx'].includes(e) ? 'doc' : ['xls', 'xlsx'].includes(e) ? 'xls' : ['zip', 'rar'].includes(e) ? 'zip' : 'vid';
    const esc = s => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    let uploads = 0;
    const used = () => [...list.querySelectorAll('.oc-file')].reduce((s, c) => s + (+c.dataset.size || 0), 0);
    function refresh() {
      const n = list.querySelectorAll('.oc-file').length, u = used();
      box.hidden = n === 0;
      total.innerHTML = n ? `${n} file${n === 1 ? '' : 's'} · <b class="${u > MAX ? 'over' : ''}">${fmt(u)}</b> of 30 MB` + (u > 25 * 1048576 ? ' <span class="oc-gmail">Gmail allows about 25 MB per email</span>' : '') : '';
      if (send && !send.hasAttribute('data-locked')) send.disabled = uploads > 0 || u > MAX;
      if (send) send.title = uploads > 0 ? 'Wait for the files to finish uploading' : u > MAX ? 'Attachments are over 30 MB' : '';
    }
    if (send?.disabled) send.setAttribute('data-locked', ''); // email not set up: stays disabled
    function error(msg) {
      const t = document.createElement('div');
      t.className = 'oc-file err';
      t.innerHTML = `<span class="oc-ficon err">!</span><span class="oc-fname"><b></b><small>Not attached</small></span><button type="button" class="oc-fx" aria-label="Dismiss">×</button>`;
      t.querySelector('b').textContent = msg;
      t.querySelector('.oc-fx').addEventListener('click', () => { t.remove(); refresh(); });
      list.append(t); box.hidden = false;
      setTimeout(() => { t.remove(); refresh(); }, 9000);
    }
    function upload(file) {
      const e = ext(file.name);
      if (!OK.includes(e)) return error(`“${file.name}” can't be attached (allowed: JPG, PNG, GIF, Word, Excel, ZIP, RAR, video).`);
      if (file.size > MAX) return error(`“${file.name}” is ${fmt(file.size)}. Files can be up to 30 MB.`);
      if (used() + file.size > MAX) return error(`“${file.name}” would take this email over 30 MB.`);
      const chip = document.createElement('div');
      chip.className = 'oc-file busy';
      chip.dataset.size = file.size;
      chip.innerHTML = `<span class="oc-ficon ${kind(e)}">${esc(e.toUpperCase().slice(0, 4))}</span><span class="oc-fname"><b title="${esc(file.name)}">${esc(file.name)}</b><small>Uploading… 0%</small></span><button type="button" class="oc-fx" data-oc-remove aria-label="Cancel">×</button><i class="oc-prog"></i>`;
      list.append(chip); uploads++; refresh();
      const xhr = new XMLHttpRequest();
      chip._xhr = xhr;
      xhr.open('POST', '/inbox/attach');
      xhr.setRequestHeader('Accept', 'application/json');
      xhr.upload.onprogress = ev => {
        if (!ev.lengthComputable) return;
        const p = Math.round(ev.loaded / ev.total * 100);
        chip.querySelector('small').textContent = `Uploading… ${p}%`;
        chip.querySelector('.oc-prog').style.width = p + '%';
      };
      xhr.onloadend = () => {
        uploads--;
        let data = null; try { data = JSON.parse(xhr.responseText); } catch { }
        if (xhr.status === 200 && data?.id) {
          chip.classList.remove('busy');
          chip.dataset.id = data.id;
          chip.querySelector('small').textContent = data.sizeText;
          chip.querySelector('.oc-prog').remove();
          chip.querySelector('[data-oc-remove]').setAttribute('aria-label', 'Remove ' + file.name);
          chip.insertAdjacentHTML('beforeend', `<input type="hidden" name="AttachmentIds" value="${+data.id}" />`);
        } else if (xhr.status !== 0 || !chip._cancelled) {
          chip.remove();
          if (!chip._cancelled) error(data?.error || (xhr.status === 413 || (xhr.status === 400 && !data) ? `“${file.name}” is too big. Files can be up to 30 MB.` : `Couldn't upload “${file.name}”. Please try again.`));
        }
        refresh();
      };
      const fd = new FormData(); fd.append('file', file); fd.append('__RequestVerificationToken', token());
      xhr.send(fd);
    }
    const addFiles = files => [...files].forEach(upload);
    form.querySelector('[data-oc-attach]').addEventListener('click', () => picker.click());
    picker.addEventListener('change', () => { addFiles(picker.files); picker.value = ''; });
    list.addEventListener('click', e => {
      const x = e.target.closest('[data-oc-remove]'); if (!x) return;
      const chip = x.closest('.oc-file');
      if (chip._xhr && chip.classList.contains('busy')) { chip._cancelled = true; chip._xhr.abort(); chip.remove(); refresh(); return; }
      const id = chip.dataset.id; chip.remove(); refresh();
      if (id) { const fd = new FormData(); fd.append('__RequestVerificationToken', token()); fetch('/inbox/detach/' + id, { method: 'POST', body: fd }); }
    });
    let depth = 0;
    form.addEventListener('dragenter', e => { if ([...(e.dataTransfer?.types || [])].includes('Files')) { depth++; drop.classList.add('on'); } });
    form.addEventListener('dragleave', () => { if (--depth <= 0) { depth = 0; drop.classList.remove('on'); } });
    form.addEventListener('dragover', e => { if ([...(e.dataTransfer?.types || [])].includes('Files')) e.preventDefault(); });
    form.addEventListener('drop', e => { if (!e.dataTransfer?.files?.length) return; e.preventDefault(); depth = 0; drop.classList.remove('on'); addFiles(e.dataTransfer.files); });
    form.addEventListener('paste', e => { const f = [...(e.clipboardData?.files || [])]; if (f.length) { e.preventDefault(); addFiles(f); } });
    refresh();

    form.addEventListener('submit', e => {
      if (uploads > 0) { e.preventDefault(); return; }
      if (sending || send?.disabled) { e.preventDefault(); return; }
      sending = true;
      if (send) { send.lastChild.textContent = 'Sending…'; send.setAttribute('aria-busy', 'true'); }
      setTimeout(() => { if (send) send.disabled = true; }, 0); // after the browser has taken the form data
    });
    form.addEventListener('keydown', e => {
      if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); form.requestSubmit(); }
    });
    form.querySelector('[data-oc-discard]')?.addEventListener('click', e => {
      if (body.value !== start && body.value.trim() && !window.confirm('Discard this draft?')) e.preventDefault();
    });
    window.addEventListener('beforeunload', e => { if (!sending && body.value !== start && body.value.trim()) e.preventDefault(); });
  }

  // ── select several messages (checkboxes, Shift+click for a range) → delete them together ──
  const listBox = ol.querySelector('[data-ol-items]');
  const bulk = ol.querySelector('[data-ol-bulk]');
  const allBox = ol.querySelector('[data-ol-all]');
  const checks = () => [...ol.querySelectorAll('[data-ol-check]')];
  const picked = () => checks().filter(c => c.checked);
  let lastCheck = null;
  function syncSelection() {
    if (!bulk) return;
    const all = checks(), n = all.filter(c => c.checked).length;
    all.forEach(c => c.closest('[data-ol-row]').classList.toggle('checked', c.checked));
    ol.classList.toggle('selecting', n > 0);
    bulk.hidden = n === 0;
    bulk.querySelector('[data-ol-count]').textContent = `${n} selected`;
    if (allBox) { allBox.checked = n > 0 && n === all.length; allBox.indeterminate = n > 0 && n < all.length; }
  }
  function clearSelection() { checks().forEach(c => { c.checked = false; }); syncSelection(); }
  if (bulk && listBox) {
    listBox.addEventListener('click', e => {
      const c = e.target.closest('[data-ol-check]'); if (!c) return;
      const all = checks();
      if (e.shiftKey && lastCheck && lastCheck !== c) {
        const [a, b] = [all.indexOf(lastCheck), all.indexOf(c)].sort((x, y) => x - y);
        all.slice(a, b + 1).forEach(x => { x.checked = c.checked; });
      }
      lastCheck = c; syncSelection();
    });
    allBox?.addEventListener('change', () => { checks().forEach(c => { c.checked = allBox.checked; }); syncSelection(); });
    bulk.querySelector('[data-ol-clear]').addEventListener('click', clearSelection);
    // Runs before the confirm popup (admin.js listens on document), so the ids are in the form when it is sent.
    bulk.addEventListener('submit', e => {
      bulk.querySelectorAll('input[name=ids]').forEach(x => x.remove());
      const sel = picked();
      if (!sel.length) { e.preventDefault(); e.stopPropagation(); return; }
      sel.forEach(c => bulk.insertAdjacentHTML('beforeend', `<input type="hidden" name="ids" value="${+c.value}" />`));
      bulk.dataset.name = `${sel.length} ${ol.querySelector('.ol-nav a.on[href*="folder=sent"]') ? 'email' : 'message'}${sel.length === 1 ? '' : 's'}`;
    });
    syncSelection(); // the browser may restore ticked boxes after Back
  }

  // ── keyboard: J/K or ↑/↓ move through the list, R reply, F forward, N new, / search, Delete removes selected ──
  document.addEventListener('keydown', e => {
    if (e.ctrlKey || e.metaKey || e.altKey || e.target.closest('input:not([type=checkbox]),textarea,select,[contenteditable]')) return;
    if (document.querySelector('.modal:not([hidden]),[data-cmdk].open')) return;
    if (bulk && !bulk.hidden) {
      if (e.key === 'Delete') { e.preventDefault(); bulk.requestSubmit(); return; }
      if (e.key === 'Escape') { e.preventDefault(); clearSelection(); return; }
    }
    const items = [...ol.querySelectorAll('[data-ol-item]')];
    const cur = items.findIndex(a => a.classList.contains('sel'));
    const k = e.key;
    const go = i => { if (items[i]) location.href = items[i].href; };
    if (k === 'j' || k === 'ArrowDown') { e.preventDefault(); go(cur < 0 ? 0 : cur + 1); }
    else if (k === 'k' || k === 'ArrowUp') { e.preventDefault(); go(cur < 0 ? 0 : cur - 1); }
    else if (k === 'r') { const a = ol.querySelector('.ol-bar a[href*="compose=reply"]'); if (a) { e.preventDefault(); location.href = a.href; } }
    else if (k === 'f') { const a = ol.querySelector('.ol-bar a[href*="compose=forward"]'); if (a) { e.preventDefault(); location.href = a.href; } }
    else if (k === 'n') { e.preventDefault(); location.href = ol.querySelector('.ol-new').href; }
    else if (k === '/') { e.preventDefault(); ol.querySelector('[data-ol-search]')?.focus(); }
    else if (k === 'Escape' && ol.classList.contains('reading')) { const b = ol.querySelector('.ol-back'); if (b) location.href = b.href; }
  });
})();
