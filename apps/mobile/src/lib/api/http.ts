import { API_URL } from "@/lib/env";
import { authStore } from "@/lib/store/auth-store";
import type { ApiErrorResponse, AuthResponse } from "@/lib/api/types";

export class ApiError extends Error {
  constructor(
    public readonly code: string,
    message: string,
    public readonly status: number,
    public readonly details?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

interface RequestOptions {
  method?: string;
  body?: unknown;
  auth?: boolean;
  query?: Record<string, string | number | boolean | null | undefined>;
  signal?: AbortSignal;
}

let refreshInFlight: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const { refreshToken, setSession, clear } = authStore.getState();
  if (!refreshToken) return null;

  try {
    const res = await fetch(`${API_URL}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });
    if (!res.ok) {
      clear();
      return null;
    }
    const auth = (await res.json()) as AuthResponse;
    setSession(auth);
    return auth.accessToken;
  } catch {
    clear();
    return null;
  }
}

function buildUrl(path: string, query?: RequestOptions["query"]): string {
  let url = `${API_URL}${path}`;
  if (query) {
    const parts: string[] = [];
    for (const [key, value] of Object.entries(query)) {
      if (value !== null && value !== undefined) {
        parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`);
      }
    }
    if (parts.length) url += `?${parts.join("&")}`;
  }
  return url;
}

async function parseError(res: Response): Promise<ApiError> {
  let payload: Partial<ApiErrorResponse> = {};
  try {
    payload = (await res.json()) as ApiErrorResponse;
  } catch {
    // non-JSON error body
  }
  return new ApiError(
    payload.code ?? `http.${res.status}`,
    payload.message ?? "Request failed",
    res.status,
    payload.details,
  );
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = "GET", body, auth = true, query, signal } = options;

  const doFetch = async (token: string | null): Promise<Response> => {
    const headers: Record<string, string> = {};
    if (body !== undefined) headers["Content-Type"] = "application/json";
    if (auth && token) headers["Authorization"] = `Bearer ${token}`;
    return fetch(buildUrl(path, query), {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal,
    });
  };

  let token = auth ? authStore.getState().accessToken : null;
  let res = await doFetch(token);

  if (res.status === 401 && auth) {
    refreshInFlight ??= refreshAccessToken().finally(() => {
      refreshInFlight = null;
    });
    token = await refreshInFlight;
    if (token) res = await doFetch(token);
  }

  if (!res.ok) {
    throw await parseError(res);
  }

  if (res.status === 204) {
    return undefined as T;
  }

  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}
