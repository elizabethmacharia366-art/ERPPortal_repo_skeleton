import { authService } from '@/services/auth/authService'

const API_BASE = 'http://localhost:5000'

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

function messageFrom(problem: ProblemDetails | null, status: number): string {
  if (problem?.errors) {
    const messages = Object.values(problem.errors).flat()
    if (messages.length) return messages.join(' ')
  }
  if (problem?.detail) return problem.detail
  if (problem?.title) return problem.title
  if (status === 403) return 'You do not have permission to do that.'
  return `Request failed with status ${status}`
}

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
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null
    throw new Error(messageFrom(problem, response.status))
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
