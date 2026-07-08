export type ApiValidationError = {
  code: string
  message: string
}

export type ProblemDetails = {
  type?: string
  title: string
  status: number
  code: string
  traceId?: string
  errors?: Record<string, ApiValidationError[]>
}

export class ApiError extends Error {
  readonly problem: ProblemDetails | undefined
  readonly status: number | undefined

  constructor(message: string, problem?: ProblemDetails, status?: number) {
    super(message)
    this.name = 'ApiError'
    this.problem = problem
    this.status = status
  }
}

export function isProblemDetails(value: unknown): value is ProblemDetails {
  if (!value || typeof value !== 'object') return false
  const candidate = value as Record<string, unknown>
  return (
    typeof candidate.title === 'string' &&
    typeof candidate.status === 'number' &&
    typeof candidate.code === 'string'
  )
}
