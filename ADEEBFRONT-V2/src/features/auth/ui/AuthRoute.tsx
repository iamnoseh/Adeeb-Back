import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/model/auth-context'

type AuthRouteProps = {
  children: ReactNode
}

export function AuthRoute({ children }: AuthRouteProps) {
  const { isAuthenticated, isBootstrapping, user } = useAuth()
  const { t } = useTranslation()

  if (isBootstrapping) {
    return <div className="grid min-h-screen place-items-center text-sm font-semibold text-[var(--muted)]">{t('sessionChecking')}</div>
  }

  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (user?.role !== 'SuperAdmin' && user?.role !== 'Admin') return <Navigate to="/login" replace />

  return <>{children}</>
}
