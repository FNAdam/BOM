// HTTP client tipis untuk memanggil BOM API.
// Base URL diambil dari VITE_API_URL, fallback ke localhost:5099.

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5099';

/**
 * Ekstrak pesan error dari body respons (JSON { message } / ProblemDetails / ValidationProblem),
 * sehingga UI dapat menampilkan pesan yang jelas.
 */
async function extractErrorMessage(res: Response): Promise<string> {
  const fallback = `${res.status} ${res.statusText}`;
  try {
    const text = await res.text();
    if (!text) return fallback;

    const body = JSON.parse(text) as Record<string, unknown>;

    if (typeof body.message === 'string') return body.message;
    if (typeof body.detail === 'string') return body.detail;

    // ValidationProblemDetails: { errors: { Field: [messages] } }
    if (body.errors && typeof body.errors === 'object') {
      const messages = Object.values(body.errors as Record<string, string[]>)
        .flat()
        .filter(Boolean);
      if (messages.length > 0) return messages.join(' ');
    }

    if (typeof body.title === 'string') return body.title;
    return fallback;
  } catch {
    return fallback;
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method,
    headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!res.ok) {
    throw new Error(await extractErrorMessage(res));
  }

  // 204 No Content atau body kosong.
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body: unknown) => request<T>('PUT', path, body),
  del: <T = void>(path: string) => request<T>('DELETE', path),
};
