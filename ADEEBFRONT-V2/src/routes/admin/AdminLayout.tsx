import { BookOpen, FileQuestion, Layers3, LogOut } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/features/auth/model/auth-context'
import { Button } from '@/shared/ui/Button'
import { cn } from '@/shared/lib/cn'
import { setStoredUiLanguage, type UiLanguage } from '@/shared/i18n/language'

export function AdminLayout() {
  const { user, logout } = useAuth()
  const { i18n, t } = useTranslation()
  const navItems = [
    { to: '/admin/subjects', label: t('navSubjects'), icon: BookOpen },
    { to: '/admin/topics', label: t('navTopics'), icon: Layers3 },
    { to: '/admin/questions', label: t('navQuestions'), icon: FileQuestion },
  ]

  async function changeLanguage(language: UiLanguage) {
    setStoredUiLanguage(language)
    await i18n.changeLanguage(language)
  }

  return (
    <div className="min-h-screen bg-[var(--background)] lg:grid lg:grid-cols-[240px_1fr]">
      <aside className="border-b border-[var(--border)] bg-[var(--surface)] lg:min-h-screen lg:border-b-0 lg:border-r">
        <div className="flex items-center justify-between gap-3 px-4 py-4 lg:block">
          <NavLink to="/admin" className="flex items-center gap-3 text-[var(--text)] no-underline">
            <span className="grid h-9 w-9 place-items-center rounded-md bg-[var(--primary)] font-black text-white">A</span>
            <span>
              <strong className="block text-sm">{t('appName')}</strong>
              <small className="text-xs text-[var(--muted)]">{t('brandSubtitle')}</small>
            </span>
          </NavLink>
        </div>
        <nav className="flex gap-2 overflow-x-auto px-3 pb-3 lg:grid lg:px-3 lg:py-4">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-semibold text-[var(--muted)] no-underline transition',
                  isActive ? 'bg-[var(--surface-muted)] text-[var(--text)] shadow-sm' : 'hover:bg-[var(--surface-muted)]',
                )
              }
            >
              <item.icon className="h-4 w-4" aria-hidden />
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="min-w-0">
        <header className="sticky top-0 z-10 flex items-center justify-between gap-4 border-b border-[var(--border)] bg-[color-mix(in_srgb,var(--surface)_92%,transparent)] px-4 py-3 backdrop-blur md:px-8">
          <div>
            <p className="text-sm font-bold">{user?.firstName} {user?.lastName}</p>
            <p className="text-xs text-[var(--muted)]">{user?.role}</p>
          </div>
          <div className="flex items-center gap-2">
            <div className="hidden rounded-lg border border-[var(--border)] bg-white p-1 sm:flex">
              <button
                type="button"
                className={cn('rounded-md px-3 py-1.5 text-xs font-bold', i18n.language === 'tg-TJ' ? 'bg-[var(--surface-muted)] text-[var(--text)]' : 'text-[var(--muted)]')}
                onClick={() => void changeLanguage('tg-TJ')}
              >
                {t('languageTg')}
              </button>
              <button
                type="button"
                className={cn('rounded-md px-3 py-1.5 text-xs font-bold', i18n.language === 'ru-RU' ? 'bg-[var(--surface-muted)] text-[var(--text)]' : 'text-[var(--muted)]')}
                onClick={() => void changeLanguage('ru-RU')}
              >
                {t('languageRu')}
              </button>
            </div>
            <Button variant="secondary" onClick={() => void logout()}>
              <LogOut className="h-4 w-4" aria-hidden />
              {t('logout')}
            </Button>
          </div>
        </header>
        <main className="mx-auto w-full max-w-7xl px-4 py-6 md:px-8 lg:py-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
