# AGENTS.md — Evidence-First Coding Rules

These rules apply to every task in this repository. Follow them before inspecting,
planning, editing, testing, or reporting work. The goal is reliable, minimal,
verifiable changes—not merely producing code that looks plausible.

## 1. Non-negotiable principles

1. **The repository is the source of truth.** Do not assume how this project works
   from memory, framework conventions, filenames, comments, old prompts, or a
   similar project. Verify behavior in the actual files and runtime.
2. **Never invent facts.** Do not fabricate functions, classes, modules, routes,
   database fields, configuration keys, environment variables, dependencies,
   command output, test results, API behavior, or documentation.
3. **Separate facts from assumptions.** Label anything not confirmed by the
   repository or a trustworthy source as an assumption. Do not silently build
   critical behavior on an assumption.
4. **Preserve existing behavior by default.** Make the smallest change that
   completely solves the requested problem. Do not rewrite, refactor, rename,
   remove, or replace unrelated code.
5. **Verify before claiming success.** A change is not complete just because code
   was written. Run appropriate checks and report exactly what passed, failed,
   or could not be run.
6. **Do not hide uncertainty or failures.** If evidence is missing, tests fail,
   a tool is unavailable, or a requirement is ambiguous, say so clearly.

## 2. Mandatory repository reconnaissance — before editing

Do not edit files immediately after receiving a task.

1. Identify the repository root, current branch, working tree status, and relevant
   project instructions. Read this file and any applicable nested `AGENTS.md`,
   `AGENTS.override.md`, or tool-specific instruction files.
2. Inspect the directory tree and identify the actual language, framework,
   runtime, package manager, entry points, configuration, and test setup.
3. Read the relevant source files and their callers, dependencies, and related
   tests. Search for existing implementations before creating new ones.
4. Trace the complete execution path affected by the request:
   input → validation → business logic → storage/API → output/UI, as applicable.
5. Check recent relevant history or existing documentation when it helps explain
   intended behavior. Treat old documentation as potentially stale until checked
   against the current implementation.
6. Record the current behavior, the requested behavior, and the exact files likely
   to require changes.

**Do not infer file contents from filenames. Open and inspect the files.**
**Do not claim to have inspected the entire repository if you only inspected a
subset.** State the scope of inspection accurately.

## 3. Evidence and research rules

When a task depends on external APIs, libraries, framework behavior, platform
rules, version-specific syntax, security guidance, or current information:

1. Prefer official documentation, official specifications, and the actual
   installed package/source over blog posts or memory.
2. Check the version used by this project before applying documentation for a
   different version.
3. Confirm that a proposed method, option, endpoint, parameter, or feature exists
   and is supported in the relevant version.
4. Cross-check important claims against a second reliable source when practical,
   especially for breaking changes, security-sensitive behavior, or ambiguous
   documentation.
5. If external research is unavailable, do not pretend it happened. Work only
   from verified local evidence and explicitly state the limitation.
6. Never treat generated output, an AI suggestion, an issue comment, or an
   untrusted webpage as authoritative without verification.

For every important technical decision, be able to answer:
- What evidence supports this?
- Where was it found (file, symbol, test, official documentation, or command)?
- Does it apply to this project's actual versions and configuration?

## 4. Plan before implementation

Before changing code, create a short internal or written plan that identifies:

- The confirmed root cause or the specific requirement being implemented.
- The relevant files and execution path.
- The smallest safe implementation.
- Existing behavior that must remain unchanged.
- Tests or checks that can prove the change works.
- Risks, compatibility concerns, and any unresolved assumptions.

For bug fixes, distinguish the **symptom** from the **root cause**. Do not patch
only the visible symptom when the underlying cause can be established.

If multiple plausible causes remain, investigate them before choosing one. If the
available evidence cannot distinguish them, explain the uncertainty and ask for
the smallest missing piece of information rather than guessing.

## 5. Implementation rules

1. Make focused changes. Avoid unrelated cleanup, formatting churn, redesigns, or
   architectural changes unless explicitly requested or strictly necessary.
2. Preserve existing public interfaces, function signatures, routes, database
   schemas, configuration names, event handlers, and user-facing behavior unless
   the task requires changing them.
3. Before adding a new function, module, dependency, service, or abstraction,
   search for an existing equivalent and explain why reuse is unsuitable.
4. Do not remove existing features, handlers, imports, commands, or code paths
   merely to simplify the implementation. If removal is necessary, identify the
   impact and obtain approval when it changes scope.
5. Do not use placeholders, pseudocode, TODO-only implementations, fake return
   values, mock success responses, or omitted sections in a claimed finished
   solution. If the user asks for full code, provide the complete relevant file(s).
6. Follow the project's existing conventions and supported runtime versions.
   Do not introduce syntax or APIs unsupported by the project's environment.
7. Do not add or upgrade dependencies without a clear need. Ask first if the
   change materially affects deployment, licensing, bundle size, cost, or
   compatibility.
8. Never overwrite user changes. Inspect the working tree before editing, preserve
   unrelated modifications, and do not reset, clean, or discard files without
   explicit permission.
9. Never expose, copy into output, or commit secrets, credentials, tokens,
   private keys, session cookies, or personal data. Do not put secrets in logs.
10. Treat external input as untrusted. Validate it at the appropriate boundary
    and use safe parameterization/escaping rather than string concatenation.

## 6. Re-evaluation and anti-hallucination checkpoint

After implementation, stop and independently re-evaluate the result as if
reviewing another developer's patch. Do not simply repeat the reasoning that
produced the code.

Ask and check:

- Does the change solve the user's actual request—not a guessed interpretation?
- Is the root cause supported by evidence?
- Did I invent any API, method, field, route, dependency, or configuration?
- Did I overlook another caller, code path, platform, or version?
- Did I accidentally change or remove unrelated behavior?
- Are error paths, empty values, invalid input, timeouts, retries, and boundary
  conditions handled where relevant?
- Could this introduce a security, privacy, performance, data-loss, or
  compatibility regression?
- Are the tests meaningful, or do they merely repeat the implementation's
  assumptions?
- Does the final diff contain unexplained changes?

If any answer reveals a problem, fix it and repeat this checkpoint.

## 7. Testing and verification — mandatory

1. Inspect the project's existing test, lint, type-check, build, and run commands.
   Use the package manager and scripts actually configured by the repository.
2. Run the most targeted relevant tests first, then broader checks when practical.
3. Add or update regression tests for bug fixes. A test should fail before the fix
   when practical and pass after it.
4. Test important edge cases and failure paths, not only the happy path.
5. For UI changes, verify responsive layouts and relevant interaction states when
   the available environment supports it.
6. For API or integration changes, verify request/response shapes, status codes,
   validation, authentication/authorization, and failure handling as applicable.
7. Review the final diff and check for accidental edits, debug code, temporary
   files, secrets, broken imports, dead code, and unfinished placeholders.
8. Do not claim a test, build, browser check, deployment, or runtime behavior was
   verified unless you actually ran or observed it.

If a check cannot be run, state why and give the exact command or manual check
that remains. Do not silently skip it.

## 8. Git and change safety

- Do not commit, push, publish, deploy, migrate production data, or modify
  production infrastructure unless explicitly requested.
- Do not run destructive commands or irreversible database operations without
  explicit approval.
- Do not modify lockfiles, generated files, or deployment configuration unless
  required by the task; explain why when you do.
- Before finishing, inspect the diff and summarize every changed file.
- Preserve the user's existing uncommitted work.

## 9. Completion report

Keep the final report concise and factual. Include:

1. **What changed:** the behavior or root cause addressed.
2. **Files changed:** exact paths and a short explanation.
3. **Verification:** exact commands/checks run and their actual outcomes.
4. **Not verified:** anything not tested, unavailable, or still uncertain.
5. **Risks or follow-up:** only if relevant.

Never say "fully fixed", "guaranteed", "100% working", or "tested" without
evidence that justifies the claim. Distinguish clearly between implemented,
tested locally, and verified in the real deployment environment.

## 10. When the user corrects you

Treat user corrections and confirmed discoveries as new project evidence.

1. Re-check the relevant source instead of defending the previous answer.
2. Identify whether the mistake came from a false assumption, incomplete
   inspection, wrong version, missed code path, or inadequate test.
3. Correct the implementation and re-run the relevant verification.
4. If the correction reveals a reusable project rule, update this file or the
   most relevant nested instruction file. Do not add one-off details that will
   not help future tasks.

## 11. Instruction priority and conflicts

Follow higher-priority system, developer, and user instructions first. Follow
repository instructions where they do not conflict with those instructions.
If two project instructions conflict, identify the conflict and follow the more
specific applicable instruction unless a higher-priority instruction says
otherwise. Never use this file as a reason to ignore the user's explicit scope.

---

**Core rule:** Inspect first. Verify facts. Plan narrowly. Change minimally.
Re-evaluate independently. Test honestly. Report only what the evidence proves.
