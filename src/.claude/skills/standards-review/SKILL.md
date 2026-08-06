---
name: standards-review
description: Review code against the FatCat.Toolkit coding standards in .claude/rules. Use when the user says "review all uncommitted changes", "review the ToolKit project", "review this directory", "review the last commit", "review commit <hash>", or similar. Loads the relevant language rule files, finds every violation (especially the C# one-class-per-file rule), and — only when violations exist — writes a human-readable, session-actionable markdown report to .reviews/ (gitignored). This skill NEVER edits source code and NEVER deletes report files.
---

# Standards Review

Review code in a requested scope against the FatCat.Toolkit coding standards and produce a report another session can act on. This skill **only reads and reports** — it does not fix code and does not delete report files.

Paths in this skill are relative to `src/` (the working directory and the home of the solution, `.claude/`, and all tooling config). The git root is one level up at `C:/Code/FatCat.Toolkit`; `git` commands work from anywhere in the tree.

## Step 1 — Resolve the scope

Map the user's phrasing to a concrete set of files to review.

| User says | Scope |
|---|---|
| "review all uncommitted changes" / "review my changes" | Staged + unstaged + untracked files: `git status --porcelain` (each entry is in scope). Review the **current working-tree content** of each file. |
| "review the ToolKit project" / "review the WebServer" / "review the tests" | All source files under that project directory (`ToolKit/`, `Toolkit.WebServer/`, `Tests.ToolKit/`). |
| "review this directory" / "review `<path>`" | All source files under the given directory (or the current working directory if none named). |
| "review the last commit" | Files changed in `HEAD`: `git show --stat --name-only HEAD`. Review the **content as of that commit** via `git show HEAD:<path>`. |
| "review commit `<hash>`" | Files changed in `<hash>`: `git show --stat --name-only <hash>`. Review content via `git show <hash>:<path>`. |

Rules:
- Only review source files: `*.cs`, `*.razor`, `*.ps1`. Skip `bin/`, `obj/`, `*.csproj`, `*.json`, `*.slnx`, `*.DotSettings`, and other non-source files.
- The shipped projects are `ToolKit` and `Toolkit.WebServer`; the test project is `Tests.ToolKit`. `OneOff`, `OneOffLib`, `OneOffToolkitOnly`, `OneOffBlazor`, `ProxySpike`, and `SampleDocker` are hand-run sample hosts — review them at a lighter bar (naming, formatting, obvious correctness) and never raise TDD findings against them. Say in the report that you did so.
- For commit-scoped reviews, read the file content **at that commit**, not the working tree.
- If the resolved scope is empty (e.g. no uncommitted changes), say so and stop — do not write a report.
- If the phrasing is ambiguous about which scope, pick the most likely one and state your assumption before proceeding.

## Step 2 — Load the relevant rules

Read only the rule files for the languages present in the scope. The rules live in `.claude/rules/`.

- **C# (`*.cs`, `*.razor`)** — read all of: `csharp/naming-and-structure.md`, `csharp/types-and-di.md`, `csharp/toolchain.md`, `csharp/async.md`, `csharp/errors-and-logging.md`, `csharp/testing.md`, `csharp/not-allowed.md`.
- **PowerShell (`*.ps1`)** — read `powershell/powershell.md`.

These rule files are the source of truth. The standard is "indistinguishable from code written by a senior member of this team." Treat the rules as a checklist — do not rely on memory.

## Step 3 — Review each file

Go file by file. For every file, check it against every applicable rule. Pay special attention to these high-signal violations:

### C# — emphasized checks
- **One class per file.** A `.cs` file must contain exactly one class. The *only* acceptable second type in the file is the single interface that the class directly implements (per `naming-and-structure.md`: interface + class in the same file, file named after the class). Two classes, two unrelated interfaces, or an enum + class in one file are all violations. Flag every extra top-level type and name it.
- File named after the class, never the interface.
- File-scoped namespaces only; namespace matches folder path; production namespaces start with `FatCat.Toolkit.*`, test namespaces with `Tests.FatCat.Toolkit.*`.
- No expression-bodied members (any access level, including tests).
- No records. No nullable annotations or `null!` in files that are not `#nullable enable` — the shipped projects have nullable disabled.
- Constructor (primary-constructor) injection only; no `new` for dependencies; no `ISystemScope`/`SystemScope.Container` used as a service locator.
- No `DateTime.UtcNow`/`DateTime.Now`, `Task.Delay`/`Thread.Sleep`/`new Thread(...)`, `.Result`/`.Wait()`, `async void`, `ConfigureAwait(false)` in production code.
- Collection expressions (`[]`) not `new List<T>()`; Humanizer durations (`3.Seconds()`) not `TimeSpan.From*`; switch expressions with a discard arm over if/else chains.
- **String interpolation, never `+` concatenation.** Require `$"Some string with data {theData}"`; flag any `"..." + value`. No analyzer catches this — it is a review-only rule, so check it carefully.
- Endpoints inherit `Endpoint` and return `WebResult`, with an explicit `"api/..."` route on the HTTP attribute.
- Logging goes through `ISimpleLogger` / `IToolkitLogger` / `ConsoleLog` — flag any Serilog or `Microsoft.Extensions.Logging.ILogger` injection, and any leftover scratch trace.
- **Public API surface.** This is a published NuGet library. Flag any renamed or re-signed public member in a change that did not set out to change the API, anything made public that only the toolkit uses, and any new `PackageReference` (it becomes a transitive dependency for every consumer).
- TDD: a production class in `ToolKit`/`Toolkit.WebServer` has matching tests in `Tests.ToolKit` — a `<Class>Specs` folder with an abstract base plus one concrete class per method, or a flat `<Class>Tests.cs` for a small stateless utility. Verb-first `[Fact]` names. Assertions from `FatCat.Testing` (`.Should().Not.X`), never FluentAssertions. For depth here, use `/unit-test-review` instead.

### PowerShell — emphasized checks
- `Verb-Noun` names with approved verbs; one function per file; no aliases; typed params; `[switch]` not `[bool]`; `pwsh` not `powershell.exe`.

Be precise. For each violation capture: the file, the line (or line range), what rule it breaks, and the concrete change required to satisfy the rule. Reference the rule file by name. Do not invent rules that are not in `.claude/rules/`.

## Step 4 — Report

**If there are no violations:** report a clean pass inline in the session (briefly list what was reviewed). **Do not write a file.**

**If there are violations:** write one markdown report to `.reviews/`.

- Ensure the folder exists (`src/.reviews/` — it is gitignored).
- Filename: `.reviews/<YYYY-MM-DD-HHmmss>-<scope-slug>.md`, where the scope slug describes the target (`uncommitted`, `toolkit`, `webserver`, `last-commit`, `commit-<shorthash>`, a directory name, etc.). Generate the timestamp with `pwsh -Command '. $PROFILE; (Get-Date).ToString("yyyy-MM-dd-HHmmss")'`.
- Use the template below. It must be **actionable by another session** (precise file/line/change) and **readable by a human** (grouped, plain language, no raw tool dumps).
- After writing, tell the user the report path and give a one-line summary of how many violations were found.

### Report template

```markdown
# Standards Review — <scope description>

- **Reviewed:** <what was in scope, e.g. "uncommitted changes (7 files)">
- **Generated:** <timestamp>
- **Result:** <N> violation(s) across <M> file(s)

> This report was generated by `/standards-review`. To resolve it, point a session at this
> file and ask it to fix the listed violations. **Do not delete this file** — use
> `/clean-reviews` when you want to remove reports.

---

## <relative/path/to/File.cs>

### 1. <short title of the violation>
- **Lines:** <line or range>
- **Rule:** <rule file>, <which rule>
- **Problem:** <plain-language description of what is wrong>
- **Required change:** <the concrete edit needed to comply>

### 2. <next violation in this file>
...

---

## <relative/path/to/Next.cs>
...

---

## Summary checklist
- [ ] <File.cs> — <one-line of what to fix>
- [ ] <Next.cs> — <one-line of what to fix>
```

## Hard rules for this skill
- **Never edit source files.** This skill reviews and reports only. Fixing is a separate, explicit action a session does when pointed at the report.
- **Never delete a report.** If the user asks you to "review the markdown" (i.e. read a report and resolve its issues), fix the *source code* the report points to but leave the report file in place. Removing reports is exclusively `/clean-reviews`.
- **Do not commit anything.** `.reviews/` is gitignored by design.
- Only flag violations that trace to a rule in `.claude/rules/`. If something looks off but no rule covers it, you may add it under an "Observations (not rule violations)" section, clearly separated.
