/*
 * Accessibility sweep: drives the running demo through headless Edge and runs axe-core over every
 * page, in both palettes, and then over the states that only exist after an interaction.
 *
 *     cd samples/ZenithUI.Demo && dotnet run          # in one terminal
 *     cd tools/accessibility && npm install && npm run sweep
 *
 * Why this is a script and not a test
 * ----------------------------------
 * It needs a real rendering engine and a running application. The bUnit suite asserts the ARIA a
 * component emits; this asserts what a browser builds out of it, which is where the M6 findings
 * came from - an attribute on a role that does not support it, and a wrapper element that broke
 * three id references at once. Neither was visible to a renderer without layout and a cascade.
 *
 * Interactive states are driven before auditing, because axe only ever sees the DOM in front of
 * it: a modal that has never been opened is a modal that has never been checked.
 *
 * Exits non-zero when anything is flagged, so it can gate a release even though it does not run
 * on every push.
 */

import { spawn } from "node:child_process";
import { readFileSync, writeFileSync } from "node:fs";
import { mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const base = process.env.ZENITH_DEMO_URL ?? "http://localhost:5070";
const port = Number(process.env.ZENITH_CDP_PORT ?? 9336);

const EDGE = process.env.ZENITH_BROWSER ??
  "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe";

/** Every routable page in the demo. */
const pages = [
  "/", "/tokens", "/primitives", "/typography", "/layout", "/forms", "/theming",
  "/overlays", "/data", "/shell", "/render-modes", "/reference",
];

const states = JSON.parse(readFileSync(join(here, "states.json"), "utf8"));

const axe = readFileSync(
  join(here, "node_modules", "axe-core", "axe.min.js"),
  "utf8",
);

/*
 * best-practice is included deliberately. It is not WCAG, and it is where heading-order lives -
 * one of the M6 findings, and the kind of thing that is trivially fixable while a page is being
 * written and awkward afterwards.
 */
const TAGS = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa", "best-practice"];

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

const browser = spawn(EDGE, [
  "--headless=new", "--disable-gpu", `--remote-debugging-port=${port}`,
  `--user-data-dir=${mkdtempSync(join(tmpdir(), "zenith-axe-"))}`,
  "--window-size=1440,900", "about:blank",
], { stdio: "ignore" });

let socketUrl;
for (let attempt = 0; attempt < 40 && !socketUrl; attempt++) {
  await sleep(250);
  try {
    const targets = await (await fetch(`http://127.0.0.1:${port}/json`)).json();
    socketUrl = targets.find((t) => t.type === "page")?.webSocketDebuggerUrl;
  } catch {
    // devtools not listening yet
  }
}

if (!socketUrl) {
  browser.kill();
  throw new Error(`No devtools endpoint on ${port}. Is ${EDGE} installed?`);
}

const socket = new WebSocket(socketUrl);
await new Promise((resolve) => socket.addEventListener("open", resolve));

let nextId = 0;
const pending = new Map();
socket.addEventListener("message", (event) => {
  const message = JSON.parse(event.data);
  if (message.id && pending.has(message.id)) {
    pending.get(message.id)(message);
    pending.delete(message.id);
  }
});

const send = (method, params = {}) =>
  new Promise((resolve) => {
    const id = ++nextId;
    pending.set(id, resolve);
    socket.send(JSON.stringify({ id, method, params }));
  });

const evaluate = async (expression) => {
  const response = await send("Runtime.evaluate",
    { expression, returnByValue: true, awaitPromise: true });

  return response.result?.exceptionDetails
    ? { error: response.result.exceptionDetails.exception?.description?.slice(0, 300) }
    : response.result?.result?.value;
};

const RUN = `axe.run(document, { resultTypes: ['violations'], runOnly: { type: 'tag', values: ${JSON.stringify(TAGS)} } })`
  + ".then(r => JSON.stringify({ violations: r.violations.map(v => ({"
  + " id: v.id, impact: v.impact, help: v.help,"
  + " nodes: v.nodes.slice(0, 6).map(n => ({ target: n.target,"
  + " summary: (n.failureSummary || String()).split(String.fromCharCode(10)).slice(0, 3).join(' | ') }))"
  + " })) }))";

const audit = async (label) => {
  // Injected per navigation: a page load discards the previous copy.
  await evaluate(`${axe}\ntypeof axe`);
  const raw = await evaluate(RUN);

  const parsed = typeof raw === "string"
    ? JSON.parse(raw)
    : { violations: [{ id: "axe did not run", impact: "unknown", help: JSON.stringify(raw), nodes: [] }] };

  const nodes = parsed.violations.reduce((total, v) => total + Math.max(v.nodes.length, 1), 0);
  console.log(`${parsed.violations.length === 0 ? "  ok  " : " FAIL "} ${label.padEnd(34)} ${parsed.violations.length} rules, ${nodes} nodes`);

  for (const violation of parsed.violations) {
    console.log(`         ${violation.id} [${violation.impact}] ${violation.help}`);
    for (const node of violation.nodes) {
      console.log(`           ${JSON.stringify(node.target)} :: ${node.summary.slice(0, 200)}`);
    }
  }

  return { label, violations: parsed.violations };
};

await send("Page.enable");

const report = [];

for (const theme of ["light", "dark"]) {
  await send("Emulation.setEmulatedMedia",
    { features: [{ name: "prefers-color-scheme", value: theme }] });

  for (const path of pages) {
    await send("Emulation.setDeviceMetricsOverride",
      { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false });
    await send("Page.navigate", { url: base + path });
    await sleep(2200);
    report.push(await audit(`${theme} ${path}`));
  }
}

await send("Emulation.setEmulatedMedia", { features: [{ name: "prefers-color-scheme", value: "light" }] });

for (const state of states) {
  await send("Emulation.setDeviceMetricsOverride", {
    width: state.width ?? 1440, height: state.height ?? 900, deviceScaleFactor: 1, mobile: false,
  });
  await send("Page.navigate", { url: base + state.path });
  await sleep(2400);

  const reached = await evaluate(state.drive);
  await sleep(state.settle ?? 900);

  const result = await audit(`state: ${state.name}`);
  result.reached = reached;

  // A state that was never reached is a state that was never audited, and a silent pass here is
  // worse than a failure.
  if (typeof reached !== "string" || /^no /.test(reached)) {
    console.log(`         NOT REACHED: ${JSON.stringify(reached)}`);
    result.violations.push({ id: "state not reached", impact: "unknown", help: String(reached), nodes: [] });
  }

  report.push(result);
}

writeFileSync(join(here, "report.json"), JSON.stringify(report, null, 2));

socket.close();
browser.kill();

const failed = report.filter((r) => r.violations.length > 0);
console.log(`\n${report.length - failed.length}/${report.length} clean.`);

if (failed.length > 0) {
  console.log(`Violations in: ${failed.map((f) => f.label).join(", ")}`);
  process.exitCode = 1;
}
