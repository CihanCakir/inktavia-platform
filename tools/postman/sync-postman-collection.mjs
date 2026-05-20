#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";

const ROOT = process.cwd();
// Primary scan root per spec (src/MDYKE) + fallback for actual project layout (Modules/)
const SCAN_ROOTS = [
  path.join(ROOT, "src", "MDYKE"),
  path.join(ROOT, "Modules"),
];
const COLLECTION_PATH = path.join(ROOT, "infrastructure", "postman", "inktavia-keycloak-api-tests.postman_collection.json");
const GENERATED_FOLDER = "04 - Auto-Discovered Endpoints";
const GENERATOR_ID = "tools/postman/sync-postman-collection.mjs";
const GENERATED_MARKER = "Generated from Controller";

function walk(dir, result = []) {
  if (!fs.existsSync(dir)) return result;
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const f = path.join(dir, e.name);
    if (e.isDirectory()) walk(f, result);
    else if (e.isFile() && e.name.endsWith("Controller.cs")) result.push(f);
  }
  return result;
}

function isTarget(file) {
  const normalized = file.replaceAll("\\", "/");
  return /\/src\/MDYKE\/.*\/Controllers?\/V1\/.*Controller\.cs$/i.test(normalized)
    || /\/Modules\/.*\/Controllers?\/V1\/.*Controller\.cs$/i.test(normalized);
}

function moduleOf(file, controller) {
  const s = `${file} ${controller}`.toLowerCase();
  if (s.includes("identity")) return "Identity";
  if (s.includes("profile")) return "Profile";
  if (s.includes("payment")) return "Payment";
  return "Unknown";
}

function baseOf(module) {
  if (module === "Identity") return "{{identity_api_base_url}}";
  if (module === "Profile") return "{{profile_api_base_url}}";
  if (module === "Payment") return "{{payment_api_base_url}}";
  return "{{active_api_base_url}}";
}

function tokenOf(module, policy, allowAnonymous) {
  if (allowAnonymous) return null;
  if (/AdminOnly|IdentityWrite|PaymentWrite/i.test(policy)) return "admin_access_token";
  if (/PaymentRead/i.test(policy)) return "customer_access_token";
  if (/ProfileRead|ProfileWrite/i.test(policy)) return "mobile_access_token";
  if (/IdentityRead/i.test(policy)) return "admin_access_token";
  if (module === "Payment") return "customer_access_token";
  if (module === "Profile") return "mobile_access_token";
  if (module === "Identity") return "admin_access_token";
  return "active_access_token";
}

function cleanRoute(x) {
  return (x || "")
    .replace(/^~/, "")
    .replace(/^\/+|\/+$/g, "")
    .replace(/\{([A-Za-z0-9_]+)(:[^}]+)?\}/g, "{{$1}}");
}

function parse(file) {
  const text = fs.readFileSync(file, "utf8");
  const cls = text.match(/class\s+([A-Za-z0-9_]+Controller)\b/);
  if (!cls) return [];
  const controller = cls[1];
  const classPrefix = text.slice(Math.max(0, cls.index - 2000), cls.index);
  const routes = [...classPrefix.matchAll(/\[Route\s*\(\s*"([^"]*)"\s*\)\s*\]/g)];
  let controllerRoute = routes.length ? routes[routes.length - 1][1] : "";
  controllerRoute = controllerRoute.replace("[controller]", controller.replace(/Controller$/, "").toLowerCase());

  const module = moduleOf(file, controller);
  const rel = path.relative(ROOT, file).replaceAll("\\", "/");
  const httpMap = { HttpGet:"GET", HttpPost:"POST", HttpPut:"PUT", HttpPatch:"PATCH", HttpDelete:"DELETE" };
  const actionRe = /((?:\s*\[[^\]]+\]\s*)+)\s*(?:public|private|protected|internal)\s+(?:async\s+)?(?:Task<[^>]+>|Task|IActionResult|ActionResult(?:<[^>]+>)?|[A-Za-z0-9_<>,\s]+)\s+([A-Za-z0-9_]+)\s*\(([^)]*)\)/g;
  const endpoints = [];

  for (const m of text.matchAll(actionRe)) {
    const attrs = m[1], action = m[2], args = m[3] || "";
    const allowAnonymous = /\[AllowAnonymous\b[^\]]*\]/.test(attrs);
    const policy = (attrs.match(/Policy\s*=\s*"([^"]+)"/) || attrs.match(/Roles\s*=\s*"([^"]+)"/) || [,""])[1] || "";
    for (const [attr, method] of Object.entries(httpMap)) {
      const re = new RegExp(`\\[${attr}(?:\\s*\\(\\s*"([^"]*)"\\s*\\))?[^\\]]*\\]`, "g");
      for (const hm of attrs.matchAll(re)) {
        const route = [cleanRoute(controllerRoute), cleanRoute(hm[1] || "")].filter(Boolean).join("/").replace(/\/+/g, "/");
        const hasBody = ["POST", "PUT", "PATCH"].includes(method) && /\[FromBody\]|Request|Command|Dto|Model/i.test(args);
        endpoints.push({ method, route, url: `${baseOf(module)}/${route}`, module, controller, action, source: rel, policy: policy || "Unknown", allowAnonymous, token: tokenOf(module, policy, allowAnonymous), hasBody });
      }
    }
  }
  return endpoints;
}

function findFolder(collection, name) {
  collection.item ??= [];
  let f = collection.item.find(x => x.name === name);
  if (!f) { f = { name, item: [] }; collection.item.push(f); }
  f.item ??= [];
  return f;
}

function findSub(folder, name) {
  let f = folder.item.find(x => x.name === name && Array.isArray(x.item));
  if (!f) { f = { name, item: [] }; folder.item.push(f); }
  return f;
}

function makeItem(e) {
  const headers = [{ key: "x-generated-by", value: GENERATOR_ID, type: "text" }];
  if (e.token) headers.push({ key: "Authorization", value: `Bearer {{${e.token}}}`, type: "text" });
  if (e.hasBody) headers.push({ key: "Content-Type", value: "application/json", type: "text" });
  const request = {
    method: e.method,
    header: headers,
    url: { raw: e.url, host: [e.url] },
    description: `${GENERATED_MARKER}\nSource: ${e.source}\nAction: ${e.action}\nController: ${e.controller}\nDetected Policy: ${e.policy}\nDetected Module: ${e.module}`
  };
  if (e.hasBody) request.body = { mode: "raw", raw: "{}", options: { raw: { language: "json" } } };
  return {
    name: `[${e.method}] /${e.route} - ${e.controller}.${e.action}`,
    request,
    event: [{ listen: "test", script: { type: "text/javascript", exec: ['pm.test("Status is not 500", function () {', '  pm.expect(pm.response.code).to.not.eql(500);', '});'] } }]
  };
}

function key(item) {
  return `${item?.request?.method || ""} ${item?.request?.url?.raw || ""}`;
}

const files = SCAN_ROOTS.flatMap(root => walk(root)).filter(isTarget);
const endpoints = files.flatMap(parse);
if (!fs.existsSync(COLLECTION_PATH)) throw new Error(`Collection not found: ${COLLECTION_PATH}`);
const collection = JSON.parse(fs.readFileSync(COLLECTION_PATH, "utf8"));
const folder = findFolder(collection, GENERATED_FOLDER);

const existing = new Map();
for (const sub of folder.item) {
  if (!Array.isArray(sub.item)) continue;
  for (const it of sub.item) {
    const desc = it?.request?.description || "";
    const generated = desc.includes(GENERATED_MARKER) || (it?.request?.header || []).some(h => h.key === "x-generated-by" && h.value === GENERATOR_ID);
    if (generated) existing.set(key(it), { sub, it });
  }
}

let created = 0, updated = 0;
for (const e of endpoints) {
  const sub = findSub(folder, e.module);
  const it = makeItem(e);
  const k = key(it);
  if (existing.has(k)) {
    const ex = existing.get(k);
    ex.sub.item[ex.sub.item.indexOf(ex.it)] = it;
    updated++;
  } else {
    sub.item.push(it);
    created++;
  }
}

fs.writeFileSync(COLLECTION_PATH, JSON.stringify(collection, null, 2));
console.log(`Scanned controller count: ${files.length}`);
console.log(`Discovered endpoint count: ${endpoints.length}`);
console.log(`Created request count: ${created}`);
console.log(`Updated request count: ${updated}`);
console.log(`Skipped request count: 0`);
console.log(`Output collection path: ${path.relative(ROOT, COLLECTION_PATH)}`);
