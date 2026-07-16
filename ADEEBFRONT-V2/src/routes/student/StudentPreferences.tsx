import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { StudentPreferencesContext, type StudentTheme } from '@/routes/student/student-preferences-context'

const storageKey = 'adeeb.student.theme'
export function StudentPreferencesProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<StudentTheme>(() => {
    const stored = window.localStorage.getItem(storageKey)
    if (stored === 'light' || stored === 'dark') return stored
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  })

  useEffect(() => {
    window.localStorage.setItem(storageKey, theme)
  }, [theme])

  const value = useMemo(() => ({ theme, setTheme }), [theme])
  return <StudentPreferencesContext.Provider value={value}>{children}</StudentPreferencesContext.Provider>
}
