# ERPPortal

Enterprise ERP & Web Portal platform — C#/ASP.NET Core backend, Vue.js + Tailwind frontend,
Business Central On-Premises integration via AL and REST/OData.

## Structure
- `backend/` — ASP.NET Core solution (API, Application, Domain, Infrastructure, Integration, Tests)
- `frontend/` — Vue.js + TypeScript + Tailwind portal
- `business-central/AL/` — Custom AL codeunits/API pages (only where standard BC APIs fall short)
- `database/` — EF Core migrations and seed data
- `docs/` — Architecture, API, database, security, deployment documentation
- `devops/` — CI/CD and environment configuration
- `scripts/` — Operational scripts

## Getting started
See `docs/architecture/` for the system blueprint and `docs/` subfolders as specs are filled in
during Phase 0.

## Status
Phase 0 — Discovery, Requirements & Architecture in progress.
