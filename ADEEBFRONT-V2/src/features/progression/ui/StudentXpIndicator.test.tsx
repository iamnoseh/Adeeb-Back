// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { progressionApi } from '@/features/progression/api/progression.api'
import { StudentXpIndicator } from './StudentXpIndicator'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'ru-RU' } }),
}))

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

describe('StudentXpIndicator', () => {
  it('shows the server balance and opens the XP explanation', async () => {
    vi.spyOn(progressionApi, 'getXpSummary').mockResolvedValue({ totalXp: 22, updatedAtUtc: '2026-07-21T10:00:00Z' })
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    render(<QueryClientProvider client={client}><StudentXpIndicator /></QueryClientProvider>)

    const trigger = await screen.findByRole('button', { name: 'student.xp.title' })
    await waitFor(() => expect(trigger).toHaveTextContent('22 XP'))
    await userEvent.click(trigger)

    expect(screen.getByRole('dialog', { name: 'student.xp.title' })).toBeInTheDocument()
    expect(screen.getByText('student.xp.whatIsXp')).toBeInTheDocument()
    expect(screen.getByText('student.xp.earnTests')).toBeInTheDocument()
  })
})
