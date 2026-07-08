import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/model/auth-context'

type GuestRouteProps = {
  children: ReactNode
}

export function GuestRoute({ children }: GuestRouteProps) {
  const { isAuthenticated, isBootstrapping } = useAuth()

  if (isBootstrapping) {
    return <div className="grid min-h-screen place-items-center text-sm font-semibold text-[var(--muted)]">Санҷиши сессия...</div>
  }

  return isAuthenticated ? <Navigate to="/admin" replace /> : <>{children}</>
}
