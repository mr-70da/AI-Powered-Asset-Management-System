# Kinana Assets Client

Angular (v21, standalone components) client for the KINANA asset management API.

## Development server

```bash
npm install
npm start
```

Run `ng serve` (port 4200). The API base URL defaults to
`http://localhost:5258` (see `src/environments/`); the API's CORS policy
allows only `http://localhost:4200`.

## Key commands

- `npm start` — dev server
- `npm run build` — production build (outputs to `dist/`)

## Structure

- `src/app/core/models` — typed interfaces mirroring the C# DTOs
- `src/app/core/services` — `AuthService`, `AssetService`, `LookupService`
- `src/app/core/interceptors` — bearer-token + 401/403 handling
- `src/app/core/guards` — auth (`canActivate`), admin (`canMatch`), unsaved changes
- `src/app/core/directives` — `*appAdminOnly` structural directive
- `src/app/features` — login, not-permitted, home, and the asset screens

See the repository root `README.md` for the full write-up, including the token
storage rationale and the UI security boundary.
