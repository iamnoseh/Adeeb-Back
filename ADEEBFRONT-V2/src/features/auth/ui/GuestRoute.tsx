import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/model/auth-context'
import { authenticatedHome } from '@/features/auth/lib/auth-role'

type GuestRouteProps = {
  children: ReactNode
}

export function GuestRoute({ children }: GuestRouteProps) {
  const { isAuthenticated, isBootstrapping, user } = useAuth()
  const { t } = useTranslation()

  if (isBootstrapping) {
    return <div className="grid min-h-screen place-items-center text-sm font-semibold text-[var(--muted)]">{t('sessionChecking')}</div>
  }

  return isAuthenticated ? <Navigate to={authenticatedHome(user?.role)} replace /> : <>{children}</>
}
