using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers;

/// <summary>
/// Public deep-link bridge for OWNER email links. There is no owner web app — owners are mobile-only — so an https
/// link in an email cannot open a custom scheme directly (webmail strips <c>inktavia-marine://</c>). This anonymous,
/// data-free endpoint returns a tiny self-contained page that immediately attempts the app scheme and shows a branded
/// "open in app" fallback if the app isn't installed.
///
/// Open-redirect safe: the scheme is HARDCODED and <c>{path}</c> is validated against a strict allowlist of the exact
/// link shapes the notification system emits — anything else 404s. No arbitrary input is ever reflected into a redirect.
/// </summary>
[AllowAnonymous]
public sealed class LinkBridgeController : ControllerBase
{
    private const string MobileScheme = "inktavia-marine";

    // Allowlist of owner deep-link shapes. Covers exactly what Wave 4A emits for owners (service-request detail/list,
    // kit by code, catalog, maintenance) — a deliberate superset of the 3 documented examples (see report flag).
    private static readonly Regex AllowedPath = new(
        @"^(service-requests(/\d+)?|cargodry/kits/[A-Za-z0-9\-]+|cargodry/catalog|maintenance)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [HttpGet("/link/{*path}")]
    public IActionResult Bridge([FromRoute] string? path)
    {
        var clean = (path ?? string.Empty).Trim().Trim('/');
        if (!AllowedPath.IsMatch(clean))
            return NotFound();

        // Scheme hardcoded; path already allowlist-validated (only [a-z0-9/-]). JSON-encode defensively for JS context.
        var deepLink = $"{MobileScheme}://{clean}";
        var deepLinkJs = JsonSerializer.Serialize(deepLink); // yields a safe double-quoted JS string literal

        var html = BuildHtml(deepLinkJs);
        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = 200 };
    }

    private static string BuildHtml(string deepLinkJsLiteral)
    {
        // App Store / Play links are placeholders for a later wave (mobile team registers the scheme in 4D).
        return
"<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\">" +
"<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
"<title>Inktavia Marine</title>" +
"<style>" +
"body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;" +
"font-family:-apple-system,Segoe UI,Roboto,Arial,sans-serif;background:#0b2942;color:#fff}" +
".card{max-width:360px;padding:32px 24px;text-align:center}" +
".brand{font-size:20px;font-weight:700;letter-spacing:.3px;margin-bottom:16px}" +
".msg{font-size:16px;line-height:1.5;opacity:.92;margin-bottom:24px}" +
".btn{display:inline-block;padding:12px 22px;border-radius:10px;background:#1e88e5;color:#fff;" +
"text-decoration:none;font-weight:600;border:none;font-size:15px;cursor:pointer}" +
".sub{margin-top:16px;font-size:13px;opacity:.7}.sub a{color:#bcd8f0}" +
"#fallback{display:none}" +
"</style></head><body><div class=\"card\">" +
"<div class=\"brand\">Inktavia Marine</div>" +
"<div id=\"fallback\">" +
"<div class=\"msg\">Inktavia Marine uygulamasında açılır.</div>" +
"<button class=\"btn\" onclick=\"openApp()\">Yeniden dene</button>" +
"<div class=\"sub\">Uygulama yüklü değil mi? <a href=\"#\" id=\"store\">Mağazadan indir</a></div>" +
"</div>" +
"<div id=\"loading\" class=\"msg\">Uygulama açılıyor…</div>" +
"<script>" +
"var target=" + deepLinkJsLiteral + ";" +
"function openApp(){window.location.href=target;}" +
"openApp();" +
"setTimeout(function(){" +
"document.getElementById('loading').style.display='none';" +
"document.getElementById('fallback').style.display='block';" +
"},1500);" +
"</script>" +
"</div></body></html>";
    }
}
