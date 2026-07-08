import { tokenStore } from '@/shared/auth/token-store'

type RefreshResult = {
  accessToken: string
  refreshToken: string
}

let refreshPromise: Promise<RefreshResult> | null = null
let refreshFn: ((refreshToken: string) => Promise<RefreshResult>) | null = null
let failureFn: (() => void) | null = null

export function configureRefreshManager(options: {
  refresh: (refreshToken: string) => Promise<RefreshResult>
  onRefreshFailed: () => void
}) {
  refreshFn = options.refresh
  failureFn = options.onRefreshFailed
}

export async function refreshOnce() {
  const refreshToken = tokenStore.getRefreshToken()
  if (!refreshToken || !refreshFn) {
    failureFn?.()
    throw new Error('Refresh token is not available.')
  }

  refreshPromise ??= refreshFn(refreshToken)
    .then((tokens) => {
      tokenStore.setAccessToken(tokens.accessToken)
      tokenStore.setRefreshToken(tokens.refreshToken)
      return tokens
    })
    .catch((error: unknown) => {
      failureFn?.()
      throw error
    })
    .finally(() => {
      refreshPromise = null
    })

  return refreshPromise
}
