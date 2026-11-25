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
    form.addEventListener('submit', e => {
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

  // ── keyboard: J/K or ↑/↓ move through the list, R reply, F forward, N new, / search ──
  document.addEventListener('keydown', e => {
    if (e.ctrlKey || e.metaKey || e.altKey || e.target.closest('input,textarea,select,[contenteditable]')) return;
    if (document.querySelector('.modal:not([hidden]),[data-cmdk].open')) return;
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
