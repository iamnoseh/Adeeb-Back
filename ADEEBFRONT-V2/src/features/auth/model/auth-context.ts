import { createContext, useContext } from 'react'
import type { AuthResponse, LoginRequest, UserResponse } from '@/features/auth/model/auth.types'

export type AuthContextValue = {
  user: UserResponse | null
  isAuthenticated: boolean
  isBootstrapping: boolean
  login: (request: LoginRequest) => Promise<AuthResponse>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider.')
  return context
}
