import { httpClient } from '@/shared/api/http-client'
import type { AuthResponse, LoginRequest, RefreshTokenRequest, UserResponse } from '@/features/auth/model/auth.types'

export const authApi = {
  async login(request: LoginRequest) {
    const response = await httpClient.post<AuthResponse>('/api/v2/auth/login', request)
    return response.data
  },
  async refresh(refreshToken: string) {
    const response = await httpClient.post<AuthResponse>('/api/v2/auth/refresh', {
      refreshToken,
    } satisfies RefreshTokenRequest)
    return response.data
  },
  async me() {
    const response = await httpClient.get<UserResponse>('/api/v2/auth/me')
    return response.data
  },
  async logout() {
    await httpClient.post('/api/v2/auth/logout')
  },
  async logoutAll() {
    await httpClient.post('/api/v2/auth/logout-all')
  },
}
