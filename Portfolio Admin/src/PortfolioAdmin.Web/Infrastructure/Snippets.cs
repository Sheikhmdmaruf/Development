namespace PortfolioAdmin.Web.Infrastructure;

/// <summary>Copy-paste integration code shown on a website's page.</summary>
public static class Snippets
{
    public static string HtmlForm(string apiBase, string publicKey) => $$"""
<form id="contact-form">
  <input name="name" placeholder="Your name" required>
  <input name="email" type="email" placeholder="Email" required>
  <input name="phone" placeholder="Phone (optional)">
  <select name="inquiryType"><option>Website design</option><option>Something else</option></select>
  <textarea name="message" required></textarea>
  <!-- Spam trap: hidden from people, bots fill it -->
  <input name="gotcha" tabindex="-1" autocomplete="off" style="position:absolute;left:-9999px" aria-hidden="true">
  <button>Send</button>
  <p id="form-status" role="status"></p>
</form>
<script>
document.getElementById('contact-form').addEventListener('submit', async (e) => {
  e.preventDefault();
  const form = e.target, status = document.getElementById('form-status');
  const data = Object.fromEntries(new FormData(form));
  data.pageUrl = location.href;
  const res = await fetch('{{apiBase}}/api/v1/submissions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Site-Key': '{{publicKey}}' },
    body: JSON.stringify(data)
  });
  status.textContent = res.ok ? 'Thanks! I will reply soon.' : 'Sorry, something went wrong. Please email me instead.';
  if (res.ok) form.reset();
});
</script>
""";

    public static string Server(string apiBase) => $$$"""
curl -X POST {{{apiBase}}}/api/v1/submissions \
  -H "Authorization: Bearer $PORTFOLIO_ADMIN_SECRET" \
  -H "Content-Type: application/json" \
  -d '{"name":"Jane Doe","email":"jane@example.com","subject":"New project","message":"Hello!","extra":{"budget":"$2k"}}'
""";

    public static string Content(string apiBase, string publicKey) => $"GET {apiBase}/api/v1/content?key={publicKey}";
}
