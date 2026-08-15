# THREAT MODEL

## Authorization Threats
- Privilege escalation by submitting an unauthorized role value during user creation/edit
- Cross-company or cross-farm access through direct URL manipulation
- Financial access leakage to operational-only roles
- Historical `Viewer` role reactivation through stale seed logic or old selectors

## Current Mitigations
- Server-side role validation uses approved assignable-role lists
- Controllers rely on centralized policy constants
- Company/farm isolation remains enforced independently of navigation
- Viewer is retired from current runtime constants, policies, seed data, and selectors
- Existing-database Viewer rows are handled by an explicit retirement service rather than being left active
