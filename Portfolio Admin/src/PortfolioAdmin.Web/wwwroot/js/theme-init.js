// Runs in <head> before first paint so there is no light/dark flash.
// Preference: "light" | "dark" | "system" (default), stored per browser.
(function () {
  var root = document.documentElement, mq = window.matchMedia('(prefers-color-scheme: dark)');
  function pref() { try { return localStorage.getItem('pa.theme') || 'system'; } catch (e) { return 'system'; } }
  function apply() {
    var p = pref();
    root.setAttribute('data-theme', p === 'system' ? (mq.matches ? 'dark' : 'light') : p);
    root.setAttribute('data-theme-pref', p);
  }
  apply();
  mq.addEventListener('change', apply);
  window.__setTheme = function (p) { try { localStorage.setItem('pa.theme', p); } catch (e) {} apply(); document.dispatchEvent(new Event('themechange')); };
  try { if (localStorage.getItem('pa.sidebar') === 'collapsed') root.classList.add('sidebar-collapsed'); } catch (e) {}
})();
