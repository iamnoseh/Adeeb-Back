let accessToken: string | null = null

const refreshTokenKey = 'adeeb.refreshToken'

export const tokenStore = {
  getAccessToken() {
    return accessToken
  },
  setAccessToken(token: string | null) {
    accessToken = token
  },
  getRefreshToken() {
    return window.sessionStorage.getItem(refreshTokenKey)
  },
  setRefreshToken(token: string | null) {
    if (token) {
      window.sessionStorage.setItem(refreshTokenKey, token)
      return
    }

    window.sessionStorage.removeItem(refreshTokenKey)
  },
  clear() {
    accessToken = null
    window.sessionStorage.removeItem(refreshTokenKey)
  },
}
