import { BookOpenCheck, CalendarCheck2, ChevronDown, HelpCircle, Home, Languages, ListChecks, LogOut, PanelLeftClose, PanelLeftOpen, Route, Settings, ShieldCheck, Swords, Trophy, UserRound, type LucideIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/features/auth/model/auth-context'
import { cn } from '@/shared/lib/cn'
import { AdeebBrand } from '@/shared/ui/AdeebBrand'
import { StudentPreferencesProvider } from '@/routes/student/StudentPreferences'
import { useStudentPreferences } from '@/routes/student/student-preferences-context'
import { useStudentActivityVisit } from '@/features/student-activity/model/useStudentActivity'

type StudentNavItem = { to: string; label: string; icon: LucideIcon; end?: boolean }
type StudentNavGroup = { id: string; label: string; icon: LucideIcon; items: StudentNavItem[] }

export function StudentLayout() {
  return <StudentPreferencesProvider><StudentLayoutContent /></StudentPreferencesProvider>
}

function StudentLayoutContent() {
  useStudentActivityVisit()
  const { user, logout } = useAuth()
  const { t } = useTranslation()
  const { theme } = useStudentPreferences()
  const location = useLocation()
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const homeItem: StudentNavItem = { to: '/student', label: t('student.home'), icon: Home, end: true }
  const mmtItem: StudentNavItem = { to: '/student/mmt', label: t('student.mmt'), icon: Route }
  const navGroups: StudentNavGroup[] = [
    { id: 'practice', label: t('student.practice'), icon: BookOpenCheck, items: [
      { to: '/student/tests', label: t('student.tests'), icon: ShieldCheck },
      { to: '/student/vocabulary', label: t('vocabulary.studentNav'), icon: Languages },
      { to: '/student/red-list', label: t('student.testing.redList.shortTitle'), icon: ListChecks },
      { to: '/student/daily-tasks', label: t('student.dailyTasks'), icon: CalendarCheck2 },
    ] },
    { id: 'competition', label: t('student.competition'), icon: Swords, items: [
      { to: '/student/duels', label: t('student.duel'), icon: Swords },
      { to: '/student/league', label: t('student.league'), icon: Trophy },
    ] },
    { id: 'account', label: t('student.account'), icon: UserRound, items: [
      { to: '/student/profile', label: t('student.profile'), icon: UserRound },
      { to: '/student/settings', label: t('student.settings'), icon: Settings },
      { to: '/student/support', label: t('student.support'), icon: HelpCircle },
    ] },
  ]
  const allItems = [homeItem, mmtItem, ...navGroups.flatMap((group) => group.items)]
  const current = allItems.find((item) => item.end ? location.pathname === item.to : location.pathname.startsWith(item.to)) ?? homeItem
  const activeGroup = navGroups.find((group) => group.items.some((item) => location.pathname.startsWith(item.to)))?.id
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>(() => ({ practice: activeGroup === 'practice', competition: activeGroup === 'competition', account: activeGroup === 'account' }))

  useEffect(() => {
    if (activeGroup) setOpenGroups((currentGroups) => ({ ...currentGroups, [activeGroup]: true }))
  }, [activeGroup])

  return (
    <div className={cn('student-shell min-h-screen bg-[var(--student-bg)] text-[var(--student-text)] transition-colors lg:p-2', theme === 'dark' && 'student-theme-dark')}>
      <div className={cn('mx-auto min-h-screen max-w-[1720px] overflow-hidden bg-[var(--student-canvas)] transition-[grid-template-columns] duration-300 lg:grid lg:min-h-[calc(100vh-1rem)] lg:rounded-lg lg:border lg:border-[var(--student-border)] lg:shadow-[0_22px_70px_rgb(20_31_70/0.08)]', sidebarOpen ? 'lg:grid-cols-[230px_minmax(0,1fr)]' : 'lg:grid-cols-[82px_minmax(0,1fr)]')}>
        <aside className="hidden border-r border-[var(--student-border)] bg-[var(--student-surface)] lg:flex lg:flex-col">
          <div className={cn('relative flex h-22 items-center', sidebarOpen ? 'px-6' : 'justify-center px-3')}>
            {sidebarOpen ? <AdeebBrand to="/student" /> : <Link to="/student" className="grid h-10 w-10 place-items-center rounded-lg bg-[#5146f0] font-black text-white no-underline shadow-[0_8px_20px_rgb(81_70_240/0.22)]">A</Link>}
            {sidebarOpen ? <button type="button" className="absolute right-2 grid h-9 w-9 place-items-center rounded-lg text-[var(--student-muted)] hover:bg-[var(--student-surface-soft)] hover:text-[#5146f0]" onClick={() => setSidebarOpen(false)} aria-label="Collapse sidebar"><PanelLeftClose className="h-4 w-4" /></button> : null}
          </div>

          {!sidebarOpen ? <div className="flex justify-center border-b border-[var(--student-border)] pb-3"><button type="button" className="grid h-9 w-9 place-items-center rounded-lg text-[var(--student-muted)] hover:bg-[var(--student-surface-soft)] hover:text-[#5146f0]" onClick={() => setSidebarOpen(true)} aria-label="Open sidebar"><PanelLeftOpen className="h-4 w-4" /></button></div> : null}

          <nav className={cn('custom-scrollbar flex-1 overflow-y-auto pb-5', sidebarOpen ? 'px-3 pt-0' : 'px-3 pt-3')} aria-label={t('student.navigation')}>
            <DesktopNavLink item={homeItem} sidebarOpen={sidebarOpen} />
            <DesktopNavLink item={mmtItem} sidebarOpen={sidebarOpen} />
            <div className="mt-2">
              {navGroups.map((group) => {
                const isOpen = openGroups[group.id] ?? false
                const isCurrent = activeGroup === group.id
                return (
                  <section key={group.id} className={cn(sidebarOpen ? 'border-t border-[var(--student-border)] py-2 first:border-t-0' : 'py-1')}>
                    <button type="button" className={cn('flex min-h-11 w-full items-center rounded-lg text-sm font-black transition', sidebarOpen ? 'gap-3 px-3' : 'justify-center px-0', isCurrent ? 'text-[#5146f0]' : 'text-[var(--student-muted)] hover:bg-[var(--student-surface-soft)] hover:text-[var(--student-text)]')} onClick={() => { if (!sidebarOpen) { setSidebarOpen(true); setOpenGroups((groups) => ({ ...groups, [group.id]: true })); return } setOpenGroups((groups) => ({ ...groups, [group.id]: !isOpen })) }} aria-expanded={sidebarOpen ? isOpen : false} title={group.label}>
                      <group.icon className="h-4 w-4 shrink-0" />
                      {sidebarOpen ? <><span className="min-w-0 flex-1 truncate text-left">{group.label}</span><ChevronDown className={cn('h-4 w-4 transition-transform', isOpen && 'rotate-180')} /></> : null}
                    </button>
                    {sidebarOpen && isOpen ? <div className="mt-1 grid gap-1 border-l border-[#d9d6ff] pl-3">{group.items.map((item) => <DesktopNavLink key={item.to} item={item} sidebarOpen />)}</div> : null}
                  </section>
                )
              })}
            </div>
          </nav>

          <button type="button" className={cn('mx-3 mb-4 inline-flex min-h-10 items-center justify-center gap-2 rounded-lg text-sm font-bold text-[var(--student-muted)] hover:bg-[var(--student-surface-soft)] hover:text-[#d94848]', !sidebarOpen && 'mx-auto w-10')} onClick={() => void logout()} title={t('logout')}><LogOut className="h-4 w-4" />{sidebarOpen ? t('logout') : null}</button>
        </aside>

        <div className="min-w-0">
          <header className="sticky top-0 z-40 flex h-18 items-center border-b border-[var(--student-border)] bg-[var(--student-surface)]/96 px-4 backdrop-blur-lg sm:px-6 lg:px-8">
            <div className="lg:hidden"><AdeebBrand to="/student" compact inverse={theme === 'dark'} /></div>
            <h1 className="hidden text-2xl font-black tracking-normal lg:block">{current.label}</h1>
            <div className="ml-auto flex items-center gap-2 sm:gap-4">
              <Link to="/student/support" className="hidden min-h-10 items-center gap-2 rounded-lg border border-[var(--student-border)] px-4 text-sm font-bold text-[var(--student-text)] no-underline hover:bg-[var(--student-surface-soft)] sm:inline-flex"><HelpCircle className="h-4 w-4" />{t('student.support')}</Link>
              <Link to="/student/profile" className="flex min-w-0 items-center gap-3 text-[var(--student-text)] no-underline"><span className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-[#e9e7ff] text-sm font-black text-[#5146f0]">{initials(user?.firstName, user?.lastName)}</span><span className="hidden max-w-40 truncate text-sm font-black xl:block">{user?.firstName} {user?.lastName}</span><ChevronDown className="hidden h-4 w-4 text-[var(--student-muted)] sm:block" /></Link>
            </div>
          </header>
          <main className="mx-auto w-full max-w-[1450px] px-4 py-5 pb-28 sm:px-6 md:pb-8 lg:px-8 lg:py-6"><Outlet /></main>
        </div>

        <nav className="fixed inset-x-0 bottom-0 z-40 border-t border-[var(--student-border)] bg-[var(--student-surface)]/96 px-1 pb-[max(0.5rem,env(safe-area-inset-bottom))] pt-2 backdrop-blur-lg lg:hidden" aria-label={t('student.navigation')}><div className="mx-auto grid max-w-lg grid-cols-5">{[homeItem, mmtItem, navGroups[0]!.items[0]!, navGroups[0]!.items[1]!, navGroups[2]!.items[0]!].map((item) => <MobileNavLink key={item.to} item={item} />)}</div></nav>
      </div>
    </div>
  )
}

function DesktopNavLink({ item, sidebarOpen }: { item: StudentNavItem; sidebarOpen: boolean }) {
  return <NavLink to={item.to} end={item.end ?? false} title={item.label} className={({ isActive }) => cn('flex min-h-11 items-center rounded-lg text-sm font-bold no-underline transition', sidebarOpen ? 'gap-3 px-3' : 'justify-center px-0', isActive ? 'bg-[#f0efff] text-[#5146f0]' : 'text-[var(--student-muted)] hover:bg-[var(--student-surface-soft)] hover:text-[var(--student-text)]')}><item.icon className="h-4 w-4 shrink-0" />{sidebarOpen ? <span className="truncate">{item.label}</span> : null}</NavLink>
}

function MobileNavLink({ item }: { item: StudentNavItem }) {
  return <NavLink to={item.to} end={item.end ?? false} className={({ isActive }) => cn('flex min-w-0 flex-col items-center gap-1 rounded-lg px-1 py-2 text-[0.68rem] font-bold no-underline', isActive ? 'bg-[#f0efff] text-[#5146f0]' : 'text-[var(--student-muted)]')}><item.icon className="h-4 w-4" /><span className="max-w-full truncate">{item.label}</span></NavLink>
}

function initials(firstName?: string, lastName?: string) {
  return `${firstName?.[0] ?? 'A'}${lastName?.[0] ?? ''}`.toUpperCase()
}
