let cachedCsrfToken: string | null = null;

export async function getCsrfToken(forceRefresh = false): Promise<string | null> {
  if (!forceRefresh && cachedCsrfToken) {
    return cachedCsrfToken;
  }

  const response = await fetch('/admin/csrf-token', {
    method: 'GET',
    credentials: 'include',
    cache: 'no-store'
  });

  if (!response.ok) {
    return null;
  }

  const data = await response.json().catch(() => null) as { token?: unknown } | null;
  if (typeof data?.token === 'string' && data.token.length > 0) {
    cachedCsrfToken = data.token;
    return cachedCsrfToken;
  }

  return null;
}

export function clearCsrfToken(): void {
  cachedCsrfToken = null;
}

export async function withCsrfHeaders(baseHeaders: HeadersInit = {}, forceRefresh = false): Promise<Headers> {
  const headers = new Headers(baseHeaders);
  const token = await getCsrfToken(forceRefresh);
  if (token) {
    headers.set('X-CSRF-TOKEN', token);
  }

  return headers;
}
