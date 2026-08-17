// In-memory only — never persisted. The httpOnly auth cookie can't be read by JS anyway, and
// this value is re-primed by AuthContext's boot getMe() call on every fresh page load.
let csrfToken: string | null = null;

export function setCsrfToken(token: string | null) {
  csrfToken = token;
}

export function getCsrfToken(): string | null {
  return csrfToken;
}
