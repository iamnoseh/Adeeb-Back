import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router-dom'
import { authenticatedHome, isAdminRole } from '@/features/auth/lib/auth-role'
import { useAuth } from '@/features/auth/model/auth-context'

type AuthRouteProps = {
  children: ReactNode
  audience: 'admin' | 'student'
}

export function AuthRoute({ children, audience }: AuthRouteProps) {
  const { isAuthenticated, isBootstrapping, user } = useAuth()
  const { t } = useTranslation()

  if (isBootstrapping) {
    return <div className="grid min-h-screen place-items-center text-sm font-semibold text-[var(--muted)]">{t('sessionChecking')}</div>
  }

  if (!isAuthenticated) return <Navigate to="/login" replace />

  const admin = isAdminRole(user?.role)
  if (audience === 'admin' && !admin) return <Navigate to="/student" replace />
  if (audience === 'student' && admin) return <Navigate to="/admin" replace />

  return <>{children}</>
}

export function AuthenticatedHomeRedirect() {
  const { isAuthenticated, isBootstrapping, user } = useAuth()
  const { t } = useTranslation()

  if (isBootstrapping) {
    return <div className="grid min-h-screen place-items-center text-sm font-semibold text-[var(--muted)]">{t('sessionChecking')}</div>
  }

  return <Navigate to={isAuthenticated ? authenticatedHome(user?.role) : '/login'} replace />
}
