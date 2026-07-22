const apiBaseUrl =
  (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ??
  'http://localhost:5053/api'

interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
}

export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail: string

  constructor(
    status: number,
    title: string,
    detail: string,
  ) {
    super(detail)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
  }
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  token?: string
  signal?: AbortSignal
}

export async function requestJson<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  let response: Response

  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method: options.method ?? 'GET',
      signal: options.signal,
      headers: {
        Accept: 'application/json',
        ...(options.body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
      },
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    })
  } catch {
    throw new ApiError(
      0,
      'Connection failed',
      'Could not reach the server. Please try again.',
    )
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new ApiError(
      problem.status ?? response.status,
      problem.title ?? 'Request failed',
      problem.detail ?? `The server returned ${response.status}.`,
    )
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
