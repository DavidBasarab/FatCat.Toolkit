---
name: code-security-review
description: Full-application security review. Always scans the ENTIRE source tree — never just a diff, project, or commit — and flags the top vulnerability classes using OWASP (Top 10, ASVS), Microsoft Secure Coding Guidance, the CWE Top 25, and GitHub CodeQL query coverage as the knowledge base. Use when the user says "security review", "check for vulnerabilities", "scan for security issues", or similar. Writes a dated, branch-stamped report to vulnerabilities/unresolved/ — unless screen-only output is requested (e.g. by another Claude instance), in which case the report is printed in the session and no file is written. When a session later fixes a report's findings, the report moves to vulnerabilities/resolved/ — that move happens in the fixing session, never in this skill. This skill NEVER edits source code.
---

# Code Security Review

Review the **entire library** for security vulnerabilities and produce an actionable report. This skill **only reads and reports** — it does not fix code, does not commit anything, does not delete reports, and does not move reports between folders.

Unlike a style-focused code review, security review is never diff-scoped: a vulnerability can live in code nobody touched this week, and a change in one file can make old code in another file exploitable. **Every run covers the full source tree.** If the user names a narrower target ("security review the crypto"), still review everything — you may organize the report so their named area appears first, but do not skip the rest.

## What makes this codebase different

`FatCat.Toolkit` and `FatCat.Toolkit.WebServer` are **published NuGet libraries**, not an application. That changes the threat model in three ways, and every finding should be weighed through them:

1. **A weak default ships to every consumer.** An insecure default (a weak RNG, a disabled certificate check, a permissive CORS helper) is not one vulnerability — it is one vulnerability per application that takes the package reference, in code those teams never wrote and will not audit.
2. **The library cannot see its caller's context.** It cannot assume input is trusted, that a value was validated upstream, or that a consumer reads the docs. Anything a caller can pass is attacker-controlled until proven otherwise.
3. **Consumers cannot patch it.** They can only upgrade. A vulnerability here has a slow, distributed fix cycle — which raises the severity of anything reachable through the public API.

Test doubles under `ToolKit/Testing/` and `Toolkit.WebServer/Testing/` ship in the package too. A fake that is trivially reachable from production code (or a weak generator exposed as a public API) is in scope.

## Report folders

Reports live in two committed folders under `src/`:

- `vulnerabilities/unresolved/` — reports whose findings have not all been fixed yet. New reports always land here.
- `vulnerabilities/resolved/` — reports whose findings have all been fixed. Reports are moved here **only by the session that fixed them** (see "Resolution workflow" below), never by this skill.

Create either folder if it does not exist. These folders are intentionally **not** gitignored — reports are committed alongside the code they describe.

## Step 0 — Choose the output mode

Two output modes:

- **File mode (default):** write the report to `vulnerabilities/unresolved/`.
- **Screen mode:** print the full report as markdown in the session and write **nothing** to disk.

Use screen mode when any of the following is true:
- The skill is invoked with an argument such as `screen`, `no-file`, `no-write`, or `stdout` (e.g. `/code-security-review screen`).
- The user's phrasing asks for it: "don't write a file", "just show me", "output to the screen", "inline".
- The skill is being run by another Claude instance / automated session as part of a larger task, where the findings will be consumed directly from the conversation rather than from disk.

If none of those apply, use file mode.

## Step 1 — Gather the scope and run context

The scope is always the whole repository. Working directory is `src/`; the git root is `C:/Code/FatCat.Toolkit`.

- Review source and configuration that can carry vulnerabilities: `*.cs`, `*.razor`, `*.ps1`, `*.csproj` (package references), `appsettings*.json`, `Dockerfile` and compose files, and any JavaScript under a Blazor project's `wwwroot/`.
- Skip `bin/`, `obj/`, `node_modules/`, lock files, and prior reports in `vulnerabilities/`.
- `Tests.ToolKit` and the sample hosts (`OneOff`, `OneOffLib`, `OneOffToolkitOnly`, `OneOffBlazor`, `ProxySpike`, `SampleDocker`) are in scope **primarily** for hard-coded secrets, credentials, and committed certificates — application vulnerability classes carry less weight in code that does not ship. But note where a sample demonstrates an insecure pattern that consumers will copy: samples are documentation, and a bad example propagates.
- Review the **current working-tree content** of each file (uncommitted changes included — they are part of the library as it stands).

Record the run context up front — it goes in the report header:
- **Datetime:** `pwsh -Command '. $PROFILE; (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")'` (and `yyyy-MM-dd-HHmmss` for the filename).
- **Branch:** `git branch --show-current` (if empty — detached HEAD — use `git rev-parse --short HEAD`).
- **Commit:** `git rev-parse --short HEAD`.

## Step 2 — Know what to look for

The knowledge base for this review is the union of these four sources. Do not invent exotic findings — anchor every finding to at least one of them:

1. **OWASP** — Top 10 (A01 Broken Access Control … A10 SSRF) and the relevant ASVS controls.
2. **Microsoft Secure Coding Guidance** — .NET-specific guidance: secure use of cryptography APIs, `System.Random` vs `RandomNumberGenerator`, deserialization (`BinaryFormatter` is banned), path handling, secrets in configuration, ASP.NET Core authentication/authorization patterns, CORS, anti-forgery.
3. **CWE Top 25** — the current Top 25 Most Dangerous Software Weaknesses. Tag every finding with its CWE ID (e.g. CWE-89, CWE-79, CWE-798, CWE-22, CWE-918).
4. **GitHub CodeQL** — the classes of issues CodeQL's C# query packs flag: tainted data flows (source → sink), log forging, regex injection, weak randomness, hard-coded credentials, missing certificate validation, XML external entities.

### High-signal checks for this codebase

Check these deliberately, not just generically:

- **Weak randomness in security values (CWE-338, CWE-330) — the known trap here.** `IGenerator` has both a `Faker`/`System.Random`-backed path and a `RandomNumberGenerator` path. Anything with security meaning — AES keys, IVs/nonces, tokens, salts, session or access codes — must come from `RandomNumberGenerator`. Trace `AesKeyGenerator`, `FatCatAesEncryption`, `HashTools`, and every caller of `IGenerator.Bytes`/`RandomString`/`RandomNumber`. See `FIX_AES_KEY_CSPRNG.md` at `src/` for the documented instance of exactly this class of bug — verify whether it is actually fixed in the current tree rather than assuming.
- **Crypto correctness (CWE-327, CWE-323).** AES-GCM/CBC nonce or IV reuse under the same key is catastrophic — check that a fresh IV is generated per encryption and is never derived deterministically from the plaintext or key. Check key sizes (`AesKeySize`), mode selection, and that Argon2 (`Konscious.Security.Cryptography.Argon2`) parameters are not below current guidance. MD5/SHA1 used for anything security-bearing in `HashTools`.
- **Token handling and authentication (A07, CWE-287, CWE-347).** `FatCatTokenHandler`, `ToolkitTokenParameterGenerator`, and `OAuthExtensions`: signing key strength and origin, algorithm confusion (`none`/HMAC-vs-RSA), issuer/audience/lifetime validation actually enabled, clock skew, and whether any validation parameter defaults to permissive. A library that hands consumers a token validator with a check disabled by default is a High finding at minimum.
- **Transport security (CWE-295).** `CertificationSettings`, `HttpClientFactory`, `WebCaller`, `FatHttpCaller`: any certificate-validation callback that returns `true` unconditionally, any `ServerCertificateCustomValidationCallback` bypass, any HTTP fallback where HTTPS was intended. Flag it even when it is gated behind a flag — the flag ships too.
- **SignalR hubs (`Toolkit.WebServer/SignalR`, `ToolKit/Web/Api/SignalR`).** Hub methods trusting client-supplied identity, messages routed to connections the sender should not reach, and unauthenticated hub endpoints. Consumers inherit whatever the toolkit's hub base class permits.
- **Injection into Mongo (CWE-943).** Filters built by string interpolation or `BsonDocument.Parse` on caller-supplied data instead of typed expressions in `MongoRepository<T>` / `IMongoRepository<T>`.
- **Path traversal (CWE-22).** `FileSystemTools`, `FileSystemRepository`, and `EmbeddedResourceRepository`: paths composed from caller input without normalization or containment checks. This library hands file access to consumers — an unguarded `Path.Combine` on caller input is a real finding.
- **SSRF (A10, CWE-918).** `WebCaller` / `FatHttpCaller` building request URLs from caller-supplied values, and any redirect-following that crosses hosts.
- **Deserialization (CWE-502).** `JsonOperations` and any converter: `BinaryFormatter`, `TypeNameHandling`, or polymorphic deserialization of untrusted data. Check what `JsonSerializerOptions` the library defaults to.
- **Secrets (CWE-798).** Connection strings, API keys, signing keys, or passwords hard-coded in source or committed in `appsettings*.json` (`ProxySpike/`, `SampleDocker/`). Committed certificates or key files anywhere in the tree. Name the key, redact the value.
- **Logging leaks (CWE-532).** Keys, tokens, connection strings, or request bodies reaching `ISimpleLogger`, `ToolkitLogger`, `ConsoleLog`, or exception messages. `SimpleLogger` writes a file next to the consumer's executable — anything logged there persists on their disk.
- **Unsafe reflection (CWE-470).** `ReflectionTools`, `ObjectTools`, and the Autofac activation paths: type resolution or member invocation driven by caller-supplied names.
- **Transport/config in the samples:** missing HTTPS redirection/HSTS, permissive CORS (`GetCorsDebugEndpoint`), Docker images running as root with secrets baked in.
- **Vulnerable dependencies (A06):** run `dotnet list package --vulnerable` when feasible. Note that both shipped projects set `NoWarn` on `NU1902`/`NU1903` — NuGet audit warnings are suppressed at build time, so this check is not redundant, it is the only thing catching them.

## Step 3 — Review each file

Go file by file, tracing caller-controlled data from where it enters (public API parameters, endpoint request bodies, hub messages, query strings, file paths, deserialized JSON) to where it is used (Mongo queries, logs, file/blob paths, outbound HTTP, crypto operations, reflection).

For every finding capture:
- **File and line (or range).**
- **Severity:** `Critical` (remotely exploitable, or a cryptographic/authentication weakness that every consumer inherits by default), `High` (exploitable with conditions, or an insecure default reachable through the public API), `Medium` (defense-in-depth gap, hardening), `Low` (best-practice deviation with limited impact).
- **Classification:** CWE ID + OWASP category (and CodeQL query family or Microsoft guidance reference where it applies).
- **Problem:** what an attacker can actually do, in plain language — and for a library, *whose* application they can do it to.
- **Suggested fix:** the concrete change — named APIs, patterns, or code direction that complies with the repo's coding standards (constructor injection, `IThread`, `IDateTimeUtilities`, `WebResult` returns, TDD). Call out explicitly when a fix is a **breaking public API change**, since consumers will feel it.

Before writing a finding, check `vulnerabilities/unresolved/` for existing reports: if the same issue is already reported there, still include it (each report stands alone as a full picture of the branch at its run time), but note that it also appears in the earlier report.

Be precise and honest. Do not pad the report with theoretical findings that have no path to exploitation here — if something is only an observation, label it as one. If the codebase is clean, say so.

## Step 4 — Report

Order findings by severity (Critical first). Use the template below in both modes.

**Screen mode:** print the full report in the session. State clearly that no file was written.

**File mode:**
- Ensure `vulnerabilities/unresolved/` exists.
- Filename: `vulnerabilities/unresolved/<YYYY-MM-DD-HHmmss>-<branch-slug>.md` — branch slug is the branch name with `/` replaced by `-`.
- Write the report even when there are **no findings** — a dated clean pass on a branch is itself a useful record. A clean report may be written directly to `vulnerabilities/resolved/` since there is nothing to resolve.
- After writing, tell the user the report path and a one-line summary (counts by severity).
- Do not commit the report or anything else — committing is the user's decision.

### Report template

```markdown
# Security Review — FatCat.Toolkit (full source tree)

- **Run:** <yyyy-MM-dd HH:mm:ss>
- **Branch:** <branch name>
- **Commit:** <short hash of HEAD>
- **Reviewed:** entire source tree (<N> files)
- **Result:** <N> finding(s) — <c> Critical, <h> High, <m> Medium, <l> Low
- **Sources:** OWASP Top 10 / ASVS, Microsoft Secure Coding Guidance, CWE Top 25, CodeQL query coverage
- **Status:** UNRESOLVED

> Generated by `/code-security-review`. This skill never edits source code itself.
>
> **To the session asked to fix this report:** fix the findings below in the source code,
> run `dotnet build src/ToolKit.slnx` and `dotnet test src/ToolKit.slnx` to confirm green, and
> check off each finding in the summary checklist as you resolve it. Fixes are written test-first
> like any other production change. When — and only when — every finding is fixed and verified,
> change **Status** above to `RESOLVED (<date>)` and move this file to `vulnerabilities/resolved/`
> (same filename). If any finding remains unfixed, the file stays in `vulnerabilities/unresolved/`
> with the checklist showing what is left.

---

## 1. <short title> — <Severity>
- **File:** <relative/path/to/File.cs>, lines <range>
- **Classification:** CWE-<id> — <name>; OWASP <category>
- **Problem:** <what an attacker can do and why this code allows it>
- **Consumer impact:** <what a downstream application inherits by default; "breaking API change to fix" when that is true>
- **Suggested fix:** <the concrete change>

## 2. <next finding>
...

---

## Observations (not vulnerabilities)
- <hardening suggestions or notes that are not exploitable findings — omit section if none>

## Summary checklist
- [ ] 1. <File.cs> — <one-line of what to fix> (<Severity>)
```

## Resolution workflow (for the fixing session — not this skill)

When the user says "fix this security report", "run a fix on <report>", or points a session at a file in `vulnerabilities/unresolved/`:

1. Fix each finding in the source code, following the repo's coding standards (TDD included — security fixes get tests like any other production change).
2. Run `dotnet build src/ToolKit.slnx` and `dotnet test src/ToolKit.slnx`; everything must be green.
3. Check off each resolved finding in the report's summary checklist.
4. **All findings fixed:** update the report's **Status** line to `RESOLVED (<yyyy-MM-dd>)` and move the file to `vulnerabilities/resolved/`, keeping the same filename (use `git mv` if the report is tracked).
5. **Some findings remain:** leave the file in `vulnerabilities/unresolved/` with the checklist reflecting what is done and what is left. Never move a partially resolved report.

This move is the **only** sanctioned way a report leaves `unresolved/` — the review skill itself never moves, renames, or deletes reports.

## Hard rules for this skill
- **Always review the entire source tree** — never narrow to a diff, commit, project, or directory, even if asked; the full library is the point.
- **Never edit source files.** Review and report only; fixing is a separate, explicit action.
- **Never move or delete reports.** New reports go to `unresolved/`; moving to `resolved/` belongs exclusively to the session that fixed the findings.
- **Do not commit anything.**
- Every finding must trace to OWASP, Microsoft Secure Coding Guidance, the CWE Top 25, or a CodeQL query class. Anything else goes under Observations.
- The report references files, lines, and identifiers — it never reproduces secret values (name the key, redact the value), key material, or certificate contents.
