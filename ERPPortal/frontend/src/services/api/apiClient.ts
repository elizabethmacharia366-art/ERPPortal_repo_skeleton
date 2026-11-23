import { authService } from '@/services/auth/authService'

const API_BASE = 'http://localhost:5000'

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = await authService.getAccessToken()

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      ...(options.body ? { 'Content-Type': 'application/json' } : {}),
      Authorization: token ? `Bearer ${token}` : '',
      ...options.headers,
    },
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new Error(body?.error ?? `Request failed with status ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json()
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body ? JSON.stringify(body) : undefined }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
