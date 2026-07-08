import { Bell, BookOpen, FileQuestion, Layers3, LogOut, PanelLeftClose, PanelLeftOpen, Search } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/features/auth/model/auth-context'
import { Button } from '@/shared/ui/Button'
import { cn } from '@/shared/lib/cn'
import { setStoredUiLanguage, type UiLanguage } from '@/shared/i18n/language'

export function AdminLayout() {
  const { user, logout } = useAuth()
  const { i18n, t } = useTranslation()
  const [sidebarOpen, setSidebarOpen] = useState(true)
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
    <div className={cn('min-h-screen bg-transparent transition-[grid-template-columns] duration-300 lg:grid', sidebarOpen ? 'lg:grid-cols-[258px_1fr]' : 'lg:grid-cols-[88px_1fr]')}>
      <aside className="border-b border-white/70 bg-white/78 backdrop-blur-xl lg:sticky lg:top-0 lg:min-h-screen lg:border-b-0 lg:border-r lg:border-white/70">
        <div className={cn('flex items-center gap-3 px-4 py-5', sidebarOpen ? 'justify-between' : 'justify-center')}>
          <NavLink to="/admin" className={cn('flex items-center text-[var(--text)] no-underline', sidebarOpen ? 'gap-3' : 'justify-center')}>
            <span className="grid h-11 w-11 shrink-0 place-items-center rounded-2xl bg-[linear-gradient(135deg,var(--primary),#8ab5ad)] text-lg font-black text-white shadow-[0_14px_30px_rgb(47_125_115/0.25)]">A</span>
            {sidebarOpen ? (
              <span>
                <strong className="adeeb-brand block text-2xl leading-none">{t('appName')}</strong>
              </span>
            ) : null}
          </NavLink>
          {sidebarOpen ? (
            <button
              type="button"
              className="hidden h-10 w-10 place-items-center rounded-2xl text-[var(--muted)] transition hover:bg-[var(--surface-muted)] hover:text-[var(--text)] lg:grid"
              onClick={() => setSidebarOpen(false)}
              aria-label="Collapse sidebar"
            >
              <PanelLeftClose className="h-5 w-5" aria-hidden />
            </button>
          ) : null}
        </div>
        {!sidebarOpen ? (
          <div className="hidden justify-center px-4 pb-3 lg:flex">
            <button
              type="button"
              className="grid h-10 w-10 place-items-center rounded-2xl text-[var(--muted)] transition hover:bg-[var(--surface-muted)] hover:text-[var(--text)]"
              onClick={() => setSidebarOpen(true)}
              aria-label="Open sidebar"
            >
              <PanelLeftOpen className="h-5 w-5" aria-hidden />
            </button>
          </div>
        ) : null}
        <nav className={cn('custom-scrollbar flex gap-2 overflow-x-auto px-3 pb-3 lg:grid lg:gap-1.5 lg:overflow-visible lg:px-3 lg:py-4', sidebarOpen ? '' : 'lg:px-4')}>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                cn(
                  'inline-flex min-h-11 items-center rounded-2xl text-sm font-bold text-[var(--muted)] no-underline transition',
                  sidebarOpen ? 'gap-3 px-3.5 py-2.5' : 'justify-center px-0 py-2.5 lg:w-14',
                  isActive ? 'bg-[var(--surface-muted)] text-[var(--text)] shadow-sm ring-1 ring-white' : 'hover:bg-[var(--surface-muted)] hover:text-[var(--text)]',
                )
              }
              title={item.label}
            >
              <item.icon className="h-4 w-4" aria-hidden />
              {sidebarOpen ? <span>{item.label}</span> : null}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="min-w-0">
        <header className="sticky top-0 z-10 flex items-center justify-between gap-4 border-b border-white/70 bg-white/76 px-4 py-3 backdrop-blur-xl md:px-8">
          <div className="hidden min-w-0 max-w-md flex-1 items-center gap-3 rounded-full bg-[var(--surface-muted)] px-4 py-2.5 text-sm text-[var(--muted)] md:flex">
            <Search className="h-4 w-4 shrink-0" aria-hidden />
            <span className="truncate">{t('search')}</span>
          </div>
          <div className="ml-auto flex items-center gap-2">
            <div className="hidden rounded-2xl border border-[var(--border)] bg-white p-1 shadow-sm sm:flex">
              <button
                type="button"
                className={cn('rounded-xl px-3 py-1.5 text-xs font-bold transition', i18n.language === 'tg-TJ' ? 'bg-[var(--primary-soft)] text-[var(--primary-strong)]' : 'text-[var(--muted)] hover:text-[var(--text)]')}
                onClick={() => void changeLanguage('tg-TJ')}
              >
                {t('languageTg')}
              </button>
              <button
                type="button"
                className={cn('rounded-xl px-3 py-1.5 text-xs font-bold transition', i18n.language === 'ru-RU' ? 'bg-[var(--primary-soft)] text-[var(--primary-strong)]' : 'text-[var(--muted)] hover:text-[var(--text)]')}
                onClick={() => void changeLanguage('ru-RU')}
              >
                {t('languageRu')}
              </button>
            </div>
            <button type="button" className="hidden h-11 w-11 place-items-center rounded-2xl border border-[var(--border)] bg-white text-[var(--muted)] shadow-sm transition hover:bg-[var(--surface-soft)] hover:text-[var(--text)] sm:grid">
              <Bell className="h-4 w-4" aria-hidden />
            </button>
            <div className="hidden items-center gap-3 rounded-2xl bg-white px-3 py-2 shadow-sm ring-1 ring-[var(--border)] md:flex">
              <span className="grid h-8 w-8 place-items-center rounded-xl bg-[var(--primary-soft)] text-xs font-black text-[var(--primary-strong)]">
                {(user?.firstName?.[0] ?? 'A').toUpperCase()}
              </span>
              <span className="leading-tight">
                <span className="block text-sm font-black">{user?.firstName} {user?.lastName}</span>
              </span>
            </div>
            <Button variant="secondary" onClick={() => void logout()} className="min-h-11 px-3 md:px-4">
              <LogOut className="h-4 w-4" aria-hidden />
              <span className="hidden sm:inline">{t('logout')}</span>
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
