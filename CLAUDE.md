## Working arrangement

Current priority: finish the remaining logic and ship the FitCast beta as quickly as possible.
Architecture changes come later, and only when I ask for input on them.

- Claude writes backend (ASP.NET Core API) and frontend (Angular) code from my descriptions.
- Follow the patterns already used in the codebase. Before writing, look at how comparable
  controllers, services, repositories, DTOs, tests and Angular components are structured,
  and match them.
- Use conventional, readable C# and TypeScript that I can easily revisit, extend and debug.
  Avoid newer or unusual syntax and notation when a familiar form does the same job.
- Don't introduce new dependencies, external services or architectural patterns without asking.
- Making changes and running the test suite to verify them is fine.
- Don't change tests alongside feature work unless I ask. Point out which tests will break instead.
- I make all git commits myself. Don't commit, push or open PRs, and don't offer to.

## Explanations

When explaining code, bugs, or technical decisions, format for fast scanning, not dense prose:

- Keep paragraphs to 2-3 sentences max. If an explanation needs more, break it into 
  a new paragraph or a bullet, don't let it run on.
- Lead with the conclusion or the core issue in one short sentence, then explain why.
- When presenting a choice between options, use a header or bold label for each 
  option followed by a tight 1-2 sentence explanation — not a bullet containing a 
  full paragraph.
- Pull inline code/variable references out of sentence flow where possible. Prefer 
  "the token check on line 16" over cramming `if (token is null || ...)` mid-sentence.
- Use short subheadings (### or bold) to break up distinct sections of reasoning 
  (e.g. "The bug", "Your two options", "Why Complete() works") rather than one 
  continuous block of paragraphs.
- Avoid stacking multiple qualifications/asides in one sentence. One idea per sentence.
- When confirming something I asked you to verify, state the verdict first ("Confirmed, 
  this works correctly") before the supporting detail.
