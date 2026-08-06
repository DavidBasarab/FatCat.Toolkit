# Toolchain

## Where things live
The git repository root is `C:/Code/FatCat.Toolkit`; all code and tooling config live one level down in `src/`. The solution is `src/ToolKit.slnx`. Run `dotnet` commands against it:

```bash
dotnet build src/ToolKit.slnx
dotnet test  src/ToolKit.slnx
```

## CSharpier — Final Formatting Authority
CSharpier owns **all** C# layout — braces, spacing, new lines, wrapping, single-line blocks. It is the single source of truth for formatting, and it is fully opinionated: it has no per-rule formatting switches.
- Configuration: `src/.csharpierrc` — `printWidth: 128`, `useTabs: true`, `tabWidth: 4`, plus `preprocessorSymbolSets` so conditional-compilation branches are formatted too.
- `CSharpier.MsBuild` is referenced by `ToolKit.csproj`, so formatting runs on build. Rider/VS format on save.
- CSharpier reads a small whitespace set from `.editorconfig` — `indent_style`, `tab_width`, `end_of_line`, `insert_final_newline`, `charset`, `trim_trailing_whitespace`, `dotnet_sort_system_directives_first`, `dotnet_separate_import_directive_groups`. It **ignores** every `csharp_*` formatting key.
- **Never fight CSharpier.** If it reformats something, that is correct. Do not manually reformat to avoid it.
- Write readable code — CSharpier handles the rest. Do not pre-format to match what you think CSharpier will do.

> Note: `src/` currently contains both `.csharpierrc` and `.csharpierrc.json`. CSharpier reads one of them; the two are not kept in sync automatically. If you change formatting config, change it in `.csharpierrc` and make sure the other file does not contradict it.

## dotnet format — Style & Analyzer Enforcement (NOT formatting)
`dotnet format` applies code-**style** and **analyzer** fixes only. Never run its whitespace formatter — it will fight CSharpier. Style and analyzer rules come from `src/.editorconfig`:
- Remove redundant code and unnecessary qualifiers
- `var` everywhere
- Fields made `readonly` where possible
- Block bodies only — expression-bodied members (`=>`) are banned
- String interpolation over concatenation

Run it before committing — style and analyzers only, never `whitespace`:
```bash
dotnet format style src/ToolKit.slnx                      # apply code-style fixes
dotnet format analyzers src/ToolKit.slnx                  # apply analyzer fixes
dotnet format style src/ToolKit.slnx --verify-no-changes  # gate
```

If `dotnet format` changes something, that change is correct — do not revert it. Do not suppress an analyzer rule without a comment explaining why:
```csharp
#pragma warning disable <RuleId> // <reason>
```

## Warnings Are Errors
Both shipped projects set `TreatWarningsAsErrors` in Debug and Release, with a narrow `NoWarn` list. A new warning breaks the build — fix it rather than widening `NoWarn`. Adding an ID to `NoWarn` is a deliberate, explained decision, not a way to get a build green.

## Rider / ReSharper
The team uses Rider. Solution-wide inspection settings live in `src/ToolKit.sln.DotSettings`. If ReSharper flags something, address it rather than suppressing it. When a suppression is genuinely necessary:
```csharp
// ReSharper disable once <RuleName> — <reason>
```

## .editorconfig
- `src/.editorconfig` holds the whitespace keys CSharpier reads, plus the code-style, naming, and analyzer severity rules that `dotnet format` and Rider apply.
- It also declares `csharp_*` formatting keys. CSharpier ignores those entirely — they only influence what Rider suggests as you type. **CSharpier's output is still the correct final layout.** If a `csharp_*` key ever disagrees with CSharpier, CSharpier wins; do not reformat code to satisfy the editor.
- Naming conventions are enforced as warnings. Namespace must match folder structure — enforced.
- All files should be green (no unresolved warnings) unless suppressed with a reason.

## Expression-Bodied Members — BANNED
This applies to ALL members regardless of access modifier: public, private, protected, internal.
**This ban also applies to the test project** — test methods and constructors must use block bodies too.
Do not write:
```csharp
public string Name => name;                                     // banned
public void Reset() => Execute();                               // banned
private LogLevel CurrentLevel => GetLevel();                    // banned
public DateTime UtcNow() => DateTime.UtcNow;                    // banned
public void WillReturnOk() => result.Should().BeOk();           // banned — even in tests
public MyTests() => sut = new MyClass();                        // banned — even in test constructors
```
Always use block bodies:
```csharp
public string Name { get { return name; } }                     // correct
public void Reset() { Execute(); }                              // correct
private LogLevel CurrentLevel { get { return GetLevel(); } }    // correct
public DateTime UtcNow() { return DateTime.UtcNow; }            // correct
public void WillReturnOk() { result.Should().BeOk(); }          // correct — test method
public MyTests() { sut = new MyClass(); }                       // correct — test constructor
```

## Publishing
`ToolKit` and `Toolkit.WebServer` are NuGet packages. `VersionPrefix` in the `.csproj` is the shipped version and `src/PushNugetPackages.ps1` pushes the build output. Do not bump the version as a side effect of an unrelated change — versioning is a deliberate release step.
