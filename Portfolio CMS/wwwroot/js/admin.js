// Portfolio CMS — small helpers for the dashboard forms.
(function () {
  const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value;

  // ── Repeaters: lists of rows (metrics, steps, article blocks…) ──
  // <div class="rep" data-rep> <template>row html using [__i__]</template> rows… <button data-rep-add> </div>
  function reindex(rep) {
    rep.querySelectorAll(':scope > .rep-row').forEach((row, i) => {
      row.querySelectorAll('[name]').forEach(el => { el.name = el.name.replace(/\[(\d+|__i__)\]/, `[${i}]`); });
      row.querySelectorAll('[data-rep-num]').forEach(el => { el.textContent = i + 1; });
    });
  }
  document.addEventListener('click', e => {
    const add = e.target.closest('[data-rep-add]');
    if (add) {
      const rep = add.closest('[data-rep]');
      const tpl = rep.querySelector(':scope > template');
      add.insertAdjacentHTML('beforebegin', tpl.innerHTML);
      reindex(rep);
      add.previousElementSibling.querySelector('input,textarea,select')?.focus();
      return;
    }
    const tool = e.target.closest('[data-rep-act]');
    if (tool) {
      const row = tool.closest('.rep-row'), rep = row.parentElement, act = tool.dataset.repAct;
      if (act === 'del') row.remove();
      if (act === 'up' && row.previousElementSibling?.classList.contains('rep-row')) row.parentElement.insertBefore(row, row.previousElementSibling);
      if (act === 'down' && row.nextElementSibling?.classList.contains('rep-row')) row.parentElement.insertBefore(row.nextElementSibling, row);
      reindex(rep);
    }
  });
  document.querySelectorAll('[data-rep]').forEach(reindex);
  document.addEventListener('submit', e => e.target.querySelectorAll('[data-rep]').forEach(reindex), true);

  // ── Colour fields: text box + colour picker stay in sync ──
  document.querySelectorAll('.color').forEach(box => {
    const text = box.querySelector('input[type=text]'), pick = box.querySelector('input[type=color]');
    const valid = v => /^#[0-9a-f]{6}$/i.test(v);
    if (valid(text.value)) pick.value = text.value;
    pick.addEventListener('input', () => { text.value = pick.value; });
    text.addEventListener('input', () => { if (valid(text.value)) pick.value = text.value; });
  });

  // ── Image fields: upload button + preview ──
  const previewSrc = v => (v.startsWith('/uploads/') || v.startsWith('http')) ? v : '';
  function bindImage(box) {
    if (box.dataset.bound) return; box.dataset.bound = '1';
    const input = box.querySelector('input[type=text]'), file = box.querySelector('input[type=file]');
    const btn = box.querySelector('[data-upload]'), img = box.parentElement.querySelector('.img-preview');
    const show = () => { if (!img) return; const s = previewSrc(input.value.trim()); img.src = s; img.classList.toggle('show', !!s); };
    show(); input.addEventListener('change', show);
    btn.addEventListener('click', () => file.click());
    file.addEventListener('change', async () => {
      if (!file.files[0]) return;
      const fd = new FormData(); fd.append('file', file.files[0]); fd.append('__RequestVerificationToken', token());
      btn.disabled = true; btn.textContent = 'Uploading…';
      try {
        const res = await fetch('/media/upload?json=true', { method: 'POST', body: fd });
        const data = await res.json();
        if (!res.ok) throw new Error(data.error || 'Upload failed');
        input.value = data.url; show();
      } catch (err) { alert(err.message); }
      finally { btn.disabled = false; btn.textContent = 'Upload'; file.value = ''; }
    });
  }
  document.querySelectorAll('.img-field').forEach(bindImage);
  new MutationObserver(() => document.querySelectorAll('.img-field').forEach(bindImage)).observe(document.body, { childList: true, subtree: true });

  // ── Confirm before deleting ──
  document.addEventListener('submit', e => {
    const msg = e.target.dataset.confirm;
    if (msg && !confirm(msg)) e.preventDefault();
  });

  // ── Slug preview from title ──
  const title = document.querySelector('[data-slug-from]'), slug = document.querySelector('[data-slug]');
  if (title && slug) {
    const make = s => s.toLowerCase().replace(/&/g, 'and').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
    let auto = !slug.value || slug.value === make(title.value);
    title.addEventListener('input', () => { if (auto) slug.value = make(title.value); });
    slug.addEventListener('input', () => { auto = false; });
  }

  // ── Copy buttons ──
  document.addEventListener('click', e => {
    const b = e.target.closest('[data-copy]');
    if (!b) return;
    navigator.clipboard.writeText(b.dataset.copy).then(() => { const t = b.textContent; b.textContent = 'Copied'; setTimeout(() => b.textContent = t, 1200); });
  });

  // ── List / Grid view switch (remembered per page) ──
  document.querySelectorAll('[data-views]').forEach(box => {
    const key = 'cms-view-' + box.dataset.views;
    const buttons = document.querySelectorAll('[data-view-set]');
    const set = (view, save) => {
      box.dataset.view = view;
      buttons.forEach(b => b.setAttribute('aria-pressed', String(b.dataset.viewSet === view)));
      if (save) try { localStorage.setItem(key, view); } catch { }
    };
    let saved = null;
    try { saved = localStorage.getItem(key); } catch { }
    set(saved === 'grid' ? 'grid' : 'list', false);
    buttons.forEach(b => b.addEventListener('click', () => set(b.dataset.viewSet, true)));
  });

  // ── Time zone dropdown: keep the "shown as …" hint in step with the choice ──
  document.querySelectorAll('select[data-tz]').forEach(sel => {
    const hint = sel.closest('.f')?.querySelector('.hint');
    sel.addEventListener('change', () => { if (hint) hint.textContent = `— shown as “${sel.value}”`; });
  });

  // ── Toasts: close on ×, success toasts fade out after 4 seconds ──
  const closeToast = t => { t.classList.add('hide'); setTimeout(() => t.remove(), 300); };
  document.querySelectorAll('.toast').forEach(t => {
    t.querySelector('.toast-close')?.addEventListener('click', () => closeToast(t));
    if (t.classList.contains('ok')) setTimeout(() => closeToast(t), 4000);
  });

  // ── Mobile menu ──
  document.querySelector('[data-menu]')?.addEventListener('click', () => document.querySelector('.side').classList.toggle('open'));
})();
