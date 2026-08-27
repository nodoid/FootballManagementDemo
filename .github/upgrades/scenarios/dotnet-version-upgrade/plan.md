# Keep net10.0 Plan

## Overview

**Target**: Keep all projects targeting net10.0.
**Scope**: 2 projects (FootballManagementDemo, FootballManagementEngine). Small solution with ~1.2k LOC. No package incompatibilities detected by the assessment.

## Tasks

### 01-package-updates: Apply non-breaking package updates

Apply recommended and non-breaking NuGet updates (minor/patch) and small compatibility fixes in class libraries. Verify package restoration and project references.

**Done when**: All updated packages restore successfully and the projects build without errors.

---

### 02-build-and-test: Full solution build and unit tests

Perform a full solution build and run unit tests (if present). Capture and fix any compile-time errors or test failures introduced by changes.

**Done when**: Solution builds cleanly and unit tests (if any) pass.

---

### 03-resolve-breaking-issues: Address any remaining compatibility issues

If build or runtime issues remain (API changes, MAUI adjustments), fix code or adjust package versions. This task covers MAUI-specific verifications (handlers, resources) where applicable.

**Done when**: All compile-time and MAUI runtime issues are resolved and projects build.

---

### 04-final-verification: Final validation and cleanup

Run final builds for supported platforms, perform smoke test of MAUI app, update tasks.md with statuses, and prepare progress report.

**Done when**: Solution builds and the MAUI app launches (basic smoke check on at least one platform). Tasks.md reflects completed tasks.
