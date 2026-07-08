# ADEEBFRONT V2 Frontend Rules

This is the ADEEB Admin Web frontend. It is intentionally separate from `ADEEB-V2` backend.

## Architecture

- Use pragmatic feature-first structure: `app`, `routes`, `features`, `shared`, `styles`.
- `shared` must not import `features`.
- Features must not import other feature internals.
- Routes compose features and read route params; they do not own transport or business validation.
- API modules return data, not Axios responses.

## State

- TanStack Query owns server state.
- React Hook Form owns form state.
- URL search params own list filters where useful.
- Do not store subjects, topics, questions, paginated responses, or forms in global client state.

## Auth

- Access token stays in memory.
- Refresh token is stored in `sessionStorage` for the current admin session.
- Use single-flight refresh and retry an authenticated request once.
- Never log access tokens, refresh tokens, authorization headers, or passwords.

## API And Errors

- Use ADEEB V2 `/api/v2` contracts.
- Branch on stable `ProblemDetails.code`, not translated `title`.
- Display server localized `title` and validation messages.
- Keep `Image`/`Icon` as file uploads; backend creates URLs.

## UI

- Admin UI is work-focused: dense, clear, minimal, black/white with restrained gold.
- No marketing hero, oversized dashboard, or empty future menu items.
- All controls need labels and keyboard access.
