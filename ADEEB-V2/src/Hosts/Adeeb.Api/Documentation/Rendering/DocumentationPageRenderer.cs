using System.Net;
using System.Text;
using System.Text.Json;
using Adeeb.Api.Documentation.Models;

namespace Adeeb.Api.Documentation.Rendering;

public sealed class DocumentationPageRenderer
{
    public string RenderHome(IReadOnlyList<DocumentationItem> items) =>
        RenderShell("ADEEB API Documentation", items, null, HomeContent(items));

    public string RenderItem(IReadOnlyList<DocumentationItem> items, DocumentationItem item) =>
        RenderShell(item.Title, items, item.Slug, ItemContent(item));

    public string RenderNotFound(IReadOnlyList<DocumentationItem> items, string slug) =>
        RenderShell("Documentation page not found", items, null, $"""
        <section class="hero small">
          <p class="eyebrow">404</p>
          <h1>Documentation page not found</h1>
          <p>No documentation page exists for <code>{WebUtility.HtmlEncode(slug)}</code>.</p>
          <a class="button" href="/docs">Back to documentation home</a>
        </section>
        """);

    private static string HomeContent(IReadOnlyList<DocumentationItem> items)
    {
        var routeCount = items.Count(x => x.Category == "api");
        return $"""
        <section class="hero">
          <p class="eyebrow">ADEEB V2</p>
          <h1>Internal API Documentation</h1>
          <p>Frontend-focused documentation for implemented ADEEB V2 routes. Use stable error codes for logic and localized titles/messages for display.</p>
          <div class="hero-actions">
            <a class="button" href="/docs/api/identity/login">Start with Login</a>
            <a class="button secondary" href="/openapi/v2.json">View OpenAPI JSON</a>
            <a class="button secondary" href="/swagger">Open Swagger UI</a>
          </div>
          <div class="stats">
            <span><strong>{routeCount}</strong> documented API routes</span>
            <span><strong>tg-TJ</strong> default language</span>
            <span><strong>Bearer</strong> auth</span>
          </div>
        </section>
        <section class="cards">
          <a class="card" href="/docs/flows/authentication-flow"><span>Flow</span><strong>Authentication Flow</strong><p>Register/login, token storage, authenticated requests, and refresh behavior.</p></a>
          <a class="card" href="/docs/flows/refresh-token-flow"><span>Flow</span><strong>Refresh Token Flow</strong><p>Rotation, single-flight refresh, retry once, and failure handling.</p></a>
          <a class="card" href="/docs/frontend/error-handling"><span>Frontend</span><strong>Error Handling</strong><p>Branch on stable <code>code</code>; display localized title/message.</p></a>
        </section>
        """;
    }

    private static string ItemContent(DocumentationItem item)
    {
        var method = item.Method is null ? "" : $"<span class=\"method {item.Method.ToLowerInvariant()}\">{item.Method}</span>";
        var route = item.Route is null ? "" : $"<button class=\"copy-route\" type=\"button\" data-copy=\"{WebUtility.HtmlEncode(item.Route)}\">{WebUtility.HtmlEncode(item.Route)}</button>";
        return $"""
        <article class="doc-article">
          <header class="doc-header">
            <div class="badges">{method}<span class="status">{WebUtility.HtmlEncode(item.Status)}</span><span class="auth">{WebUtility.HtmlEncode(item.Auth)}</span></div>
            <h1>{WebUtility.HtmlEncode(item.Title)}</h1>
            <div class="route-line">{route}</div>
          </header>
          {item.Html}
        </article>
        """;
    }

    private static string RenderShell(string title, IReadOnlyList<DocumentationItem> items, string? activeSlug, string content)
    {
        var searchIndex = JsonSerializer.Serialize(items.Select(x => new
        {
            x.Title,
            x.Slug,
            x.Module,
            x.Method,
            x.Route,
            x.Id,
            x.Status,
            x.Auth
        }));

        return $$"""
        <!doctype html>
        <html lang="tg">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>{{WebUtility.HtmlEncode(title)}} - ADEEB API Docs</title>
          <style>{{Css}}</style>
        </head>
        <body>
          <div class="app">
            <aside class="sidebar">
              <a class="brand" href="/docs"><span class="brand-mark">A</span><span><strong>ADEEB API</strong><small>V2 Docs</small></span></a>
              <label class="search-label" for="doc-search">Search routes</label>
              <input id="doc-search" placeholder="login, POST, auth.invalid_credentials" autocomplete="off" />
              <div id="search-results" class="search-results"></div>
              <nav>{{BuildNav(items, activeSlug)}}</nav>
            </aside>
            <main class="main">
              <div class="topbar">
                <span>Internal Documentation Portal</span>
                <div>
                  <a href="/openapi/v2.json">OpenAPI</a>
                  <a href="/swagger">Swagger</a>
                </div>
              </div>
              {{content}}
            </main>
          </div>
          <script>window.__DOCS_INDEX__ = {{searchIndex}}; {{Js}}</script>
        </body>
        </html>
        """;
    }

    private static string BuildNav(IReadOnlyList<DocumentationItem> items, string? activeSlug)
    {
        var groups = items.GroupBy(x => x.Category).OrderBy(x => x.Key switch { "api" => 10, "flows" => 20, "frontend" => 30, _ => 100 });
        var html = new StringBuilder();
        foreach (var group in groups)
        {
            html.Append("<details open><summary>");
            html.Append(group.Key switch { "api" => "API Routes", "flows" => "Flows", "frontend" => "Frontend Guides", _ => WebUtility.HtmlEncode(group.Key) });
            html.AppendLine("</summary>");

            foreach (var item in group.OrderBy(x => x.Module).ThenBy(x => x.Order))
            {
                var active = item.Slug == activeSlug ? " active" : "";
                html.Append($"<a class=\"nav-link{active}\" href=\"/docs/{WebUtility.HtmlEncode(item.Slug)}\">");
                if (item.Method is not null)
                {
                    html.Append($"<span class=\"mini-method {item.Method.ToLowerInvariant()}\">{item.Method}</span>");
                }
                html.Append(WebUtility.HtmlEncode(item.Title));
                html.AppendLine("</a>");
            }

            html.AppendLine("</details>");
        }
        return html.ToString();
    }

    private const string Css = """
    :root{color-scheme:light dark;--bg:#f6f4ef;--panel:#fffdf8;--text:#1d1d1f;--muted:#6f6b61;--line:#ded7c8;--gold:#b88716;--dark:#181714;--code:#111827;--green:#12805c;--blue:#1f6feb;--red:#c93c37}
    @media (prefers-color-scheme: dark){:root{--bg:#151515;--panel:#20201d;--text:#f5f1e8;--muted:#b5ad9f;--line:#38342c;--dark:#0f0f0f;--code:#0c1018}}
    *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:15px/1.6 Inter,Segoe UI,Arial,sans-serif}.app{display:grid;grid-template-columns:310px 1fr;min-height:100vh}.sidebar{position:sticky;top:0;height:100vh;overflow:auto;background:var(--panel);border-right:1px solid var(--line);padding:20px}.brand{display:flex;gap:12px;align-items:center;color:var(--text);text-decoration:none;margin-bottom:24px}.brand-mark{width:38px;height:38px;border-radius:8px;background:var(--gold);color:white;display:grid;place-items:center;font-weight:800}.brand small{display:block;color:var(--muted)}.search-label{font-size:12px;color:var(--muted);font-weight:700;text-transform:uppercase}#doc-search{width:100%;margin:6px 0 12px;padding:11px 12px;border:1px solid var(--line);border-radius:8px;background:transparent;color:var(--text)}.search-results{display:none;margin-bottom:16px;border:1px solid var(--line);border-radius:8px;overflow:hidden}.search-results a{display:block;padding:10px;color:var(--text);text-decoration:none;border-bottom:1px solid var(--line)}.search-results a:last-child{border-bottom:0}.search-results small{display:block;color:var(--muted)}details{margin-bottom:12px}summary{cursor:pointer;font-weight:800;color:var(--muted);padding:8px 0}.nav-link{display:flex;align-items:center;gap:8px;padding:8px 10px;border-radius:8px;color:var(--text);text-decoration:none}.nav-link:hover,.nav-link.active{background:rgba(184,135,22,.13)}.mini-method,.method{font-weight:800;border-radius:6px;padding:2px 6px;color:white;font-size:11px}.post{background:var(--green)}.get{background:var(--blue)}.delete{background:var(--red)}.main{min-width:0}.topbar{position:sticky;top:0;z-index:2;display:flex;justify-content:space-between;gap:16px;background:rgba(255,253,248,.86);backdrop-filter:blur(12px);border-bottom:1px solid var(--line);padding:13px 28px;color:var(--muted)}@media (prefers-color-scheme: dark){.topbar{background:rgba(32,32,29,.86)}}.topbar a{color:var(--gold);font-weight:700;margin-left:14px}.hero,.doc-article{max-width:980px;margin:30px auto;padding:34px;background:var(--panel);border:1px solid var(--line);border-radius:8px}.hero.small{max-width:760px}.eyebrow{color:var(--gold);font-weight:800;text-transform:uppercase;letter-spacing:.08em}h1{font-size:36px;line-height:1.15;margin:0 0 14px}h2{margin-top:34px;border-top:1px solid var(--line);padding-top:22px}h3{margin-top:24px}.anchor{opacity:0;margin-left:8px;color:var(--gold);text-decoration:none}h1:hover .anchor,h2:hover .anchor,h3:hover .anchor{opacity:1}.hero-actions{display:flex;flex-wrap:wrap;gap:10px;margin-top:22px}.button{display:inline-flex;padding:10px 14px;border-radius:8px;background:var(--gold);color:white;text-decoration:none;font-weight:800}.button.secondary{background:transparent;color:var(--gold);border:1px solid var(--line)}.stats{display:flex;flex-wrap:wrap;gap:10px;margin-top:20px}.stats span,.status,.auth{border:1px solid var(--line);border-radius:8px;padding:5px 8px;color:var(--muted)}.cards{max-width:980px;margin:0 auto 40px;display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px}.card{background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:20px;color:var(--text);text-decoration:none}.card span{color:var(--gold);font-weight:800}.badges{display:flex;gap:8px;align-items:center;margin-bottom:14px}.route-line{margin:12px 0}.copy-route{border:1px solid var(--line);background:transparent;color:var(--text);border-radius:8px;padding:10px 12px;font-family:Consolas,monospace;cursor:pointer}code{background:rgba(184,135,22,.12);padding:2px 5px;border-radius:5px}.code-block{position:relative}pre{background:var(--code);color:#e5e7eb;border-radius:8px;padding:18px;overflow:auto}pre code{background:transparent;padding:0}.copy-code{position:absolute;right:10px;top:10px;border:0;border-radius:6px;padding:6px 9px;background:#374151;color:white;cursor:pointer}blockquote{border-left:4px solid var(--gold);padding:8px 14px;background:rgba(184,135,22,.1);margin:16px 0}table{width:100%;border-collapse:collapse}td,th{border:1px solid var(--line);padding:8px}@media (max-width:900px){.app{grid-template-columns:1fr}.sidebar{position:relative;height:auto}.cards{grid-template-columns:1fr;margin:16px}.hero,.doc-article{margin:16px;padding:22px}.topbar{position:relative}.topbar div{display:none}}
    """;

    private const string Js = """
    document.querySelectorAll('.copy-code').forEach(btn=>btn.addEventListener('click',()=>navigator.clipboard.writeText(btn.nextElementSibling.innerText).then(()=>{btn.textContent='Copied';setTimeout(()=>btn.textContent='Copy',900)})));
    document.querySelectorAll('.copy-route').forEach(btn=>btn.addEventListener('click',()=>navigator.clipboard.writeText(btn.dataset.copy).then(()=>{btn.textContent='Copied';setTimeout(()=>btn.textContent=btn.dataset.copy,900)})));
    const input=document.getElementById('doc-search'),box=document.getElementById('search-results');
    input?.addEventListener('input',()=>{const q=input.value.trim().toLowerCase();if(!q){box.style.display='none';box.innerHTML='';return}const hits=window.__DOCS_INDEX__.filter(x=>[x.title,x.slug,x.module,x.method,x.route,x.id,x.status,x.auth].some(v=>(v||'').toLowerCase().includes(q))).slice(0,8);box.innerHTML=hits.map(x=>`<a href="/docs/${x.slug}"><strong>${x.method?x.method+' ':''}${x.title}</strong><small>${x.route||x.slug}</small></a>`).join('')||'<a>No results</a>';box.style.display='block'});
    """;
}
