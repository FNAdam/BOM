// Login single-admin: token JWT disimpan di localStorage (bukan cookie - frontend & backend
// beda domain saat deploy, cookie cross-site butuh SameSite=None yang lebih rumit tanpa manfaat
// tambahan di sini).

const TOKEN_KEY = 'bom_token';
const USERNAME_KEY = 'bom_username';

export interface LoginResponse {
  token: string;
  username: string;
  expiresAtUtc: string;
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getUsername(): string | null {
  return localStorage.getItem(USERNAME_KEY);
}

export function setSession(session: LoginResponse): void {
  localStorage.setItem(TOKEN_KEY, session.token);
  localStorage.setItem(USERNAME_KEY, session.username);
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USERNAME_KEY);
}

export function isAuthenticated(): boolean {
  return getToken() !== null;
}
