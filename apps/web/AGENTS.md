<INSTRUCTIONS>
# Codex-Specific Agent Instructions

> **Primary contract**: See root `/AGENTS.md` for the complete agent contract.
> This file contains Codex-specific supplements only.

---

## 1. Codex Configuration

- **Model**: gpt-5.2-codex
- **Approval policy**: on-request
- **Sandbox**: workspace-write

Configuration file: `.codex/config.toml`

---

## 2. Skill Invocation

Skills are discovered from `.codex/skills/`. Invoke with `/skill-name`.

### Installed Skills

**FPF Foundry Skills**:
- `/fpf-bridge` — Cross-context mapping
- `/fpf-context-card` — Context documentation
- `/fpf-drr` — Decision-Risk Records
- `/fpf-ids-discipline` — Identifier naming
- `/fpf-method-description` — Method documentation
- `/fpf-role-description` — Role definitions
- `/fpf-task-frame` — Task framing

**Vercel Skills**:
- `/react-best-practices` — React/Next.js performance (40+ rules)
- `/web-design-guidelines` — UI accessibility (100+ rules)

---

## 3. MCP Tools Available

| Tool            | Purpose                       |
|-----------------|-------------------------------|
| `playwright`    | Browser automation, UI debug  |
| `figma`         | Design context extraction     |
| `grafana-traces`| Distributed tracing (TraceQL) |
| `sentry`        | Error tracking and analysis   |
| `github`        | GitHub API access             |

Query via `/mcp` or use inline in tool calls.

---

## 4. Working Agreements

1. **Follow root AGENTS.md** for language, deploy, and testing policies.
2. **Use skills** for domain-specific guidance.
3. **Query MCP** for telemetry when debugging production issues.
4. **Prefer `rg`** for code search; show exact commands.
5. **Run tests** after changes when feasible.
6. **Node.js tooling**: use `npm install`, `npm run`, and `npx`.

---

## 5. Reference Links

Fetch detailed standards when needed:
- .NET + Async: https://gist.github.com/venikman/ddb78832d31ebb8997d6fefd4279fb1a
- DDD Architecture: https://gist.github.com/venikman/4ad7fb4572aa95e599007adcf2371810
- Minimal API: https://gist.github.com/venikman/1cf3b6f5eef81c72039a3c2de0b66e00
- Azure Pipelines: https://gist.github.com/venikman/77de170ba8a01333774e083b85d5fb26
- AI Model Selection: https://gist.github.com/venikman/536c6e97edcfac42734201be4c4dd3f4
- Azure Cost Optimize: https://gist.github.com/venikman/e2d3c7897bb0fcee1391b01fd8518249

## Skills
A skill is a set of local instructions to follow that is stored in a `SKILL.md` file. Below is the list of skills that can be used. Each entry includes a name, description, and file path so you can open the source for full instructions when using a specific skill.
### Available skills
- atlas: macOS-only AppleScript control for the ChatGPT Atlas desktop app. Use only when the user explicitly asks to control Atlas tabs/bookmarks/history on macOS and the "ChatGPT Atlas" app is installed; do not trigger for general browser tasks or non-macOS environments. (file: /Users/stas-studio/.codex/skills/atlas/SKILL.md)
- develop-web-game: Use when Codex is building or iterating on a web game (HTML/JS) and needs a reliable development + testing loop: implement small changes, run a Playwright-based test script with short input bursts and intentional pauses, inspect screenshots/text, and review console errors with render_game_to_text. (file: /Users/stas-studio/.codex/skills/develop-web-game/SKILL.md)
- doc: Use when the task involves reading, creating, or editing `.docx` documents, especially when formatting or layout fidelity matters; prefer `python-docx` plus the bundled `scripts/render_docx.py` for visual checks. (file: /Users/stas-studio/.codex/skills/doc/SKILL.md)
- figma: Use the Figma MCP server to fetch design context, screenshots, variables, and assets from Figma, and to translate Figma nodes into production code. Trigger when a task involves Figma URLs, node IDs, design-to-code implementation, or Figma MCP setup and troubleshooting. (file: /Users/stas-studio/.codex/skills/figma/SKILL.md)
- figma-implement-design: Translate Figma nodes into production-ready code with 1:1 visual fidelity using the Figma MCP workflow (design context, screenshots, assets, and project-convention translation). Trigger when the user provides Figma URLs or node IDs, or asks to implement designs or components that must match Figma specs. Requires a working Figma MCP server connection. (file: /Users/stas-studio/.codex/skills/figma-implement-design/SKILL.md)
- fpf-bridge: Declare an explicit FPF Bridge between two contexts (SenseCells): kind, direction, congruence level (CL), and loss notes. Use when mapping terms/models across domains, reusing a role/method across standards, or whenever cross-context equivalence/substitution is implied. (file: /Users/stas-studio/.codex/skills/fpf-bridge/SKILL.md)
- fpf-context-card: Draft or revise an FPF Context Card (bounded context): scope boundary, time stance, primary sources, key terms, and adjacent contexts. Use when starting a new project/domain, doing terminology work, or when the user asks to define context boundaries/invariants. (file: /Users/stas-studio/.codex/skills/fpf-context-card/SKILL.md)
- fpf-drr: Write an FPF Design-Rationale Record (DRR): Problem frame, Decision, Rationale, Consequences. Use when making or changing a normative rule, interface, policy, architecture choice, or when the user asks to record a decision with trade-offs. (file: /Users/stas-studio/.codex/skills/fpf-drr/SKILL.md)
- fpf-ids-discipline: Enforce FPF Intension–Description–Specification discipline: separate the thing itself (intension) from its context-bound description and only call it a spec when invariants + an acceptance harness exist. Use when writing requirements, specs, docs, or when the user asks 'is this a spec?'. (file: /Users/stas-studio/.codex/skills/fpf-ids-discipline/SKILL.md)
- fpf-method-description: Draft an FPF MethodDescription (recipe): inputs, preconditions, steps, outputs, failure modes, and an acceptance harness outline. Use when defining a repeatable workflow, SOP, runbook, or when the user asks to specify how a task should be done. (file: /Users/stas-studio/.codex/skills/fpf-method-description/SKILL.md)
- fpf-role-description: Create or revise an FPF Role Description Card (Role or Status template): Tech/Plain labels, SenseCell, applicability, minimal invariants, and misread trip-wires. Use when defining responsibilities, permissions vs behavior, or when the user asks to define a role/status. (file: /Users/stas-studio/.codex/skills/fpf-role-description/SKILL.md)
- fpf-task-frame: Frame ambiguous tasks using FPF: identify BoundedContext, Role, Capability, MethodDescription, Work outputs, and acceptance tests. Use when the user asks to frame/scope/define the problem, or when requirements feel underspecified or cross-context. (file: /Users/stas-studio/.codex/skills/fpf-task-frame/SKILL.md)
- gh-address-comments: Help address review/issue comments on the open GitHub PR for the current branch using gh CLI; verify gh auth first and prompt the user to authenticate if not logged in. (file: /Users/stas-studio/.codex/skills/gh-address-comments/SKILL.md)
- gh-fix-ci: Use when a user asks to debug or fix failing GitHub PR checks that run in GitHub Actions; use `gh` to inspect checks and logs, summarize failure context, draft a fix plan, and implement only after explicit approval. Treat external providers (for example Buildkite) as out of scope and report only the details URL. (file: /Users/stas-studio/.codex/skills/gh-fix-ci/SKILL.md)
- openai-docs: Use when the user asks how to build with OpenAI products or APIs and needs up-to-date official documentation with citations (for example: Codex, Responses API, Chat Completions, Apps SDK, Agents SDK, Realtime, model capabilities or limits); prioritize OpenAI docs MCP tools and restrict any fallback browsing to official OpenAI domains. (file: /Users/stas-studio/.codex/skills/openai-docs/SKILL.md)
- openrouter-typescript-sdk: Complete reference for integrating with 300+ AI models through the OpenRouter TypeScript SDK using the callModel pattern (file: /Users/stas-studio/.codex/skills/openrouter-typescript-sdk/SKILL.md)
- pdf: Use when tasks involve reading, creating, or reviewing PDF files where rendering and layout matter; prefer visual checks by rendering pages (Poppler) and use Python tools such as `reportlab`, `pdfplumber`, and `pypdf` for generation and extraction. (file: /Users/stas-studio/.codex/skills/pdf/SKILL.md)
- playwright: Use when the task requires automating a real browser from the terminal (navigation, form filling, snapshots, screenshots, data extraction, UI-flow debugging) via `playwright-cli` or the bundled wrapper script. (file: /Users/stas-studio/.codex/skills/playwright/SKILL.md)
- react-best-practices: React and Next.js performance best practices. 40+ rules across 8 categories, prioritized by impact. Source: vercel-labs/agent-skills (file: /Users/stas-studio/.codex/skills/react-best-practices/SKILL.md)
- screenshot: Use when the user explicitly asks for a desktop or system screenshot (full screen, specific app or window, or a pixel region), or when tool-specific capture capabilities are unavailable and an OS-level capture is needed. (file: /Users/stas-studio/.codex/skills/screenshot/SKILL.md)
- security-best-practices: Perform language and framework specific security best-practice reviews and suggest improvements. Trigger only when the user explicitly requests security best practices guidance, a security review/report, or secure-by-default coding help. Trigger only for supported languages (python, javascript/typescript, go). Do not trigger for general code review, debugging, or non-security tasks. (file: /Users/stas-studio/.codex/skills/security-best-practices/SKILL.md)
- security-ownership-map: Analyze git repositories to build a security ownership topology (people-to-file), compute bus factor and sensitive-code ownership, and export CSV/JSON for graph databases and visualization. Trigger only when the user explicitly wants a security-oriented ownership or bus-factor analysis grounded in git history (for example: orphaned sensitive code, security maintainers, CODEOWNERS reality checks for risk, sensitive hotspots, or ownership clusters). Do not trigger for general maintainer lists or non-security ownership questions. (file: /Users/stas-studio/.codex/skills/security-ownership-map/SKILL.md)
- security-threat-model: Repository-grounded threat modeling that enumerates trust boundaries, assets, attacker capabilities, abuse paths, and mitigations, and writes a concise Markdown threat model. Trigger only when the user explicitly asks to threat model a codebase or path, enumerate threats/abuse paths, or perform AppSec threat modeling. Do not trigger for general architecture summaries, code review, or non-security design work. (file: /Users/stas-studio/.codex/skills/security-threat-model/SKILL.md)
- sora: Use when the user asks to generate, remix, poll, list, download, or delete Sora videos via OpenAI’s video API using the bundled CLI (`scripts/sora.py`), including requests like “generate AI video,” “Sora,” “video remix,” “download video/thumbnail/spritesheet,” and batch video generation; requires `OPENAI_API_KEY` and Sora API access. (file: /Users/stas-studio/.codex/skills/sora/SKILL.md)
- speech: Use when the user asks for text-to-speech narration or voiceover, accessibility reads, audio prompts, or batch speech generation via the OpenAI Audio API; run the bundled CLI (`scripts/text_to_speech.py`) with built-in voices and require `OPENAI_API_KEY` for live calls. Custom voice creation is out of scope. (file: /Users/stas-studio/.codex/skills/speech/SKILL.md)
- spreadsheet: Use when tasks involve creating, editing, analyzing, or formatting spreadsheets (`.xlsx`, `.csv`, `.tsv`) using Python (`openpyxl`, `pandas`), especially when formulas, references, and formatting need to be preserved and verified. (file: /Users/stas-studio/.codex/skills/spreadsheet/SKILL.md)
- transcribe: Transcribe audio files to text with optional diarization and known-speaker hints. Use when a user asks to transcribe speech from audio/video, extract text from recordings, or label speakers in interviews or meetings. (file: /Users/stas-studio/.codex/skills/transcribe/SKILL.md)
- web-design-guidelines: Web interface design guidelines. 100+ rules for accessibility, forms, navigation, performance, and UX. Source: vercel-labs/web-interface-guidelines (file: /Users/stas-studio/.codex/skills/web-design-guidelines/SKILL.md)
- skill-creator: Guide for creating effective skills. This skill should be used when users want to create a new skill (or update an existing skill) that extends Codex's capabilities with specialized knowledge, workflows, or tool integrations. (file: /Users/stas-studio/.codex/skills/.system/skill-creator/SKILL.md)
- skill-installer: Install Codex skills into $CODEX_HOME/skills from a curated list or a GitHub repo path. Use when a user asks to list installable skills, install a curated skill, or install a skill from another repo (including private repos). (file: /Users/stas-studio/.codex/skills/.system/skill-installer/SKILL.md)
### How to use skills
- Discovery: The list above is the skills available in this session (name + description + file path). Skill bodies live on disk at the listed paths.
- Trigger rules: If the user names a skill (with `$SkillName` or plain text) OR the task clearly matches a skill's description shown above, you must use that skill for that turn. Multiple mentions mean use them all. Do not carry skills across turns unless re-mentioned.
- Missing/blocked: If a named skill isn't in the list or the path can't be read, say so briefly and continue with the best fallback.
- How to use a skill (progressive disclosure):
  1) After deciding to use a skill, open its `SKILL.md`. Read only enough to follow the workflow.
  2) When `SKILL.md` references relative paths (e.g., `scripts/foo.py`), resolve them relative to the skill directory listed above first, and only consider other paths if needed.
  3) If `SKILL.md` points to extra folders such as `references/`, load only the specific files needed for the request; don't bulk-load everything.
  4) If `scripts/` exist, prefer running or patching them instead of retyping large code blocks.
  5) If `assets/` or templates exist, reuse them instead of recreating from scratch.
- Coordination and sequencing:
  - If multiple skills apply, choose the minimal set that covers the request and state the order you'll use them.
  - Announce which skill(s) you're using and why (one short line). If you skip an obvious skill, say why.
- Context hygiene:
  - Keep context small: summarize long sections instead of pasting them; only load extra files when needed.
  - Avoid deep reference-chasing: prefer opening only files directly linked from `SKILL.md` unless you're blocked.
- Safety and fallback: If a skill can't be applied cleanly (missing files, unclear instructions), state the issue, pick the next-best approach, and continue.
</INSTRUCTIONS>
