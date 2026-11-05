# GeneFoundry

[![Portable checks](https://github.com/GuySymonds/Rimworld_GeneFoundry/actions/workflows/portable-checks.yml/badge.svg?branch=main&event=push)](https://github.com/GuySymonds/Rimworld_GeneFoundry/actions/workflows/portable-checks.yml)
[![CodeQL](https://github.com/GuySymonds/Rimworld_GeneFoundry/actions/workflows/codeql.yml/badge.svg?branch=main&event=push)](https://github.com/GuySymonds/Rimworld_GeneFoundry/actions/workflows/codeql.yml)
[![RimWorld 1.6 / 1.7](https://img.shields.io/badge/RimWorld-1.6%20%2F%201.7-blue)](Mod/About/About.xml)
[![Requires Biotech](https://img.shields.io/badge/DLC-Biotech_required-blue)](Mod/About/About.xml)
[![License MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

GeneFoundry **v1.1.0** turns organic matter into custom genepacks in **RimWorld 1.6 or 1.7 with Biotech**. See the [release notes](CHANGELOG.md#110--in-development).

1. **Extract** raw biomass from meat, plants or organic corpses at the Gene Vat, with optional enzymes and artificial-part salvage.
2. **Refine** hopper-fed biomass in an automated Biomass Refinery.
3. **Fabricate** researched single- or multi-gene genepacks at the Gene Fabricator. No gene samples required.

## Getting started

Copy the packaged `GeneFoundry` folder into RimWorld's local `Mods` directory, then enable **Core → Biotech → GeneFoundry**. Avoid enabling multiple copies of the mod. To build the package from source, follow the [build and release guide](docs/RELEASE.md#build-and-package).

## Documentation

| Topic | Guide |
|---|---|
| Extraction, storage, degradation, refinery, building costs and power | [Biomass production](docs/Biomass.md) |
| Research progression, gene categories and archite gates | [Research](docs/Research.md) |
| Gene selection, costs, skills, bills and unfinished work | [Gene fabrication](docs/Fabrication.md) |
| Dirt, exposure and lighting penalties | [Processing environment](docs/ProcessingEnvironment.md) |
| Gene Vat sprites, animation and corpse presentation | [Gene Vat visuals](docs/GeneVatVisuals.md) |
| Refinery overlay alignment, frames and timing | [Refinery artwork](docs/BiomassRefineryOverlay.md) |
| Test commands, coverage and in-game acceptance | [Testing](docs/Testing.md) |
| Build, package, deploy, version and publish | [Release guide](docs/RELEASE.md) |
| CI checks and what the badges mean | [Pipelines](docs/Pipelines.md) |
| Review findings, verification evidence and remaining release work | [Solution review](docs/Review.md) |

## Development & AI Collaboration

GeneFoundry was created and is maintained by Guy Symonds, working collaboratively with ChatGPT by OpenAI, with additional development assistance from OpenAI Codex and GitHub Copilot.

The development process combines:

- Human-led gameplay design, creative direction, balancing and engineering decisions.
- Collaborative design discussions, technical problem-solving and implementation guidance using ChatGPT.
- AI-assisted programming, debugging and refactoring using Codex and GitHub Copilot.
- AI-assisted visual asset creation and animation design under human direction.
- Human-led testing, validation and final approval of changes.

The project demonstrates how a human developer can collaborate with AI tools to build a functioning RimWorld mod. Guy Symonds retains creative direction and responsibility for the finished project.

GeneFoundry is an independent community project and is not affiliated with, sponsored by or endorsed by OpenAI, GitHub or Ludeon Studios.

## Contributing

Read [AGENTS.md](AGENTS.md) for shared contributor rules, repository structure and naming conventions. Codex uses that file; [Copilot's instructions](.github/copilot-instructions.md) point to the same guidance.

[Release notes](CHANGELOG.md) · [Workshop listing text](docs/workshop-description.txt) · [Report a bug](https://github.com/GuySymonds/Rimworld_GeneFoundry/issues) · [MIT license](LICENSE)
