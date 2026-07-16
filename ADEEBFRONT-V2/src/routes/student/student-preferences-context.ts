import { createContext, useContext } from 'react'

export type StudentTheme = 'light' | 'dark'

export type StudentPreferencesValue = {
  theme: StudentTheme
  setTheme: (theme: StudentTheme) => void
}

export const StudentPreferencesContext = createContext<StudentPreferencesValue | null>(null)

export function useStudentPreferences() {
  const context = useContext(StudentPreferencesContext)
  if (!context) throw new Error('useStudentPreferences must be used inside StudentPreferencesProvider.')
  return context
}
