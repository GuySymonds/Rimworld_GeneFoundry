# Pipelines and badges

`portable-checks.yml` runs on pushes, pull requests and manual dispatch on a Windows hosted runner. It installs .NET 10, runs portable calculation/XML/asset tests and parses every PowerShell script. Test results are retained for 14 days. It has read-only repository permissions and never deploys or publishes.

The README's **Portable checks** badge tracks push runs on `main`. It reports this workflow's status, not the status of an unmerged branch. These repository settings are not supplied by a YAML file; the required protection configuration is listed below.

`codeql.yml` uses CodeQL Action v4 for C# and workflow analysis on pushes and pull requests to `main` and weekly. Its **CodeQL** badge tracks `main` push runs and certifies only that analysis ran, not that the mod is secure or builds. No coverage is collected, so there is no coverage badge or coverage rule. GitHub Code Quality is separate from CodeQL and is not a prerequisite for these checks.

## Main branch protection

The active **Protect main** ruleset targets the default branch and requires:

- Pull requests, resolved review conversations and linear history, with **squash** as the allowed merge method. The repository's enabled merge method must also be squash; requiring merge commits here would conflict with both repository settings and linear history.
- Successful **Portable tests and script syntax**, **Analyze (csharp)** and **Analyze (actions)** checks, originating from the GitHub Actions app (integration ID `15368`). The branch must be up to date before merging.
- CodeQL code-scanning results, blocking security alerts of high or critical severity and other alerts of error severity. A successful analysis job alone does not establish that its findings meet this policy.
- No force pushes or branch deletion, and no bypass actors.

These settings were applied and read back on 4 November 2026. Keep the required check names synchronized with workflow job names. Repository settings are live state; verify them again if merge behavior changes.

## C# analysis limitations

The hosted C# job uses `build-mode: none` because hosted runners do not have the installed RimWorld/Unity assemblies needed by the production project. This is source analysis with inferred dependencies, not a complete production build. Unresolved game API types and calls can reduce dataflow accuracy and lead to missed findings or false positives; zero alerts does not establish that the mod is secure.

The [4 November 2026 baseline scan](https://github.com/GuySymonds/Rimworld_GeneFoundry/actions/runs/41851127467/job/126647409397) reported **57% of calls with resolved targets** and **73% of expressions with known types**, below CodeQL's 85% diagnostic thresholds. These are dependency-resolution metrics, not test-coverage percentages, and are a dated baseline rather than a guarantee about future scans. Review the extraction diagnostics after workflow/dependency changes, even when the job and badge are green. Upgrading the action does not supply the missing game assemblies.

Keep the lightweight hosted scan alongside the explicit managed-game tests and package checks below. Full build-backed CodeQL analysis would require a trusted environment with the game references; it is not provided by this workflow. See [GitHub's C# build-mode documentation](https://docs.github.com/en/code-security/reference/code-scanning/codeql/build-options-for-compiled-languages).

The other badges identify the declared RimWorld 1.6 target, required Biotech DLC and MIT license. They are metadata, not test results. Do not add a release/download badge until there is a corresponding published artifact. Do not claim coverage percentages without collecting and publishing measured coverage.

## Game and package validation

Hosted CI has no installed RimWorld assemblies. The portable suite compiles selected production sources; it does not build the entire mod or run the conditional game API tests. Use the explicit [managed game test command](Testing.md#automated-commands) and [release/package workflow](RELEASE.md#build-and-package) on a machine with RimWorld installed. Those checks still do not render Unity or simulate a live colony.

No dormant self-hosted job is added: a runner would need provisioning and access controls, particularly before accepting pull-request code. If unattended full validation becomes necessary, supply a trusted Windows runner with legally installed game assemblies, restrict it to trusted revisions, run managed API tests, package and validate, and retain the validated artifact. Give that workflow a separate **Game and package validation** badge. Never make a missing game installation look like a successful skipped validation.

Publishing remains manual through the documented Workshop process. A green portable badge is not permission to publish.

Badge semantics follow [GitHub's workflow status badge documentation](https://docs.github.com/en/actions/how-tos/monitor-workflows/add-a-status-badge).
