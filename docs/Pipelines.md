# Pipelines and badges

`portable-checks.yml` runs on pushes, pull requests and manual dispatch on a Windows hosted runner. It installs .NET 10, runs portable calculation/XML/asset tests and parses every PowerShell script. Test results are retained for 14 days. It has read-only repository permissions and never deploys or publishes.

The README's **Portable checks** badge tracks push runs on `main`. It reports this workflow's status, not the status of an unmerged branch. It may show no status until the workflow is pushed and runs. Protect `main` by requiring pull requests and the **Portable tests and script syntax** status check, requiring the branch to be up to date before merging, and disallowing force pushes and branch deletion. These repository settings are not supplied by a YAML file; configure the status check after its first run.

The other badges identify the declared RimWorld 1.6 target, required Biotech DLC and MIT license. They are metadata, not test results. Do not add a release/download badge until there is a corresponding published artifact. Do not claim coverage percentages without collecting and publishing measured coverage.

## Game and package validation

Hosted CI has no installed RimWorld assemblies. The portable suite compiles selected production sources; it does not build the entire mod or run the conditional game API tests. Use the explicit game test command and release/package workflow in [RELEASE.md](RELEASE.md) on a machine with RimWorld installed. Those checks still do not render Unity or simulate a live colony.

No dormant self-hosted job is added: a runner would need provisioning and access controls, particularly before accepting pull-request code. If unattended full validation becomes necessary, supply a trusted Windows runner with legally installed game assemblies, restrict it to trusted revisions, run managed API tests, package and validate, and retain the validated artifact. Give that workflow a separate **Game and package validation** badge. Never make a missing game installation look like a successful skipped validation.

Publishing remains manual through the documented Workshop process. A green portable badge is not permission to publish.

Badge semantics follow [GitHub's workflow status badge documentation](https://docs.github.com/en/actions/how-tos/monitor-workflows/add-a-status-badge).
