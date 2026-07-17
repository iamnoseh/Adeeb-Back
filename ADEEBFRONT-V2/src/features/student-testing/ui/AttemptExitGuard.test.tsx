// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { useRef } from 'react'
import { createMemoryRouter, Link, RouterProvider, useNavigate } from 'react-router-dom'
import { AttemptExitGuard } from './AttemptExitGuard'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

afterEach(cleanup)

function createGuardedRouter(active = true) {
  function Attempt() {
    const allowExit = useRef(false)
    const navigate = useNavigate()
    return <><Link to="/tests">Leave</Link><AttemptExitGuard active={active} allowExit={allowExit} finishing={false} unanswered={3} onFinish={() => { allowExit.current = true; navigate('/result') }} /></>
  }

  return createMemoryRouter([
    { path: '/attempt', element: <Attempt /> },
    { path: '/tests', element: <h1>Tests</h1> },
    { path: '/result', element: <h1>Result</h1> },
  ], { initialEntries: ['/attempt'] })
}

describe('AttemptExitGuard', () => {
  it('blocks navigation and keeps the student in the active test', async () => {
    const router = createGuardedRouter()
    const user = userEvent.setup()
    render(<RouterProvider router={router} />)

    await user.click(screen.getByRole('link', { name: 'Leave' }))
    expect(await screen.findByRole('alertdialog')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/attempt')

    await user.click(screen.getByRole('button', { name: 'student.testing.stayInTest' }))
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/attempt')
  })

  it('submits the attempt instead of continuing the abandoned navigation', async () => {
    const router = createGuardedRouter()
    const user = userEvent.setup()
    render(<RouterProvider router={router} />)

    await user.click(screen.getByRole('link', { name: 'Leave' }))
    await user.click(await screen.findByRole('button', { name: 'student.testing.finishAndExit' }))

    expect(await screen.findByRole('heading', { name: 'Result' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/result')
  })

  it('does not block navigation when the attempt is no longer active', async () => {
    const router = createGuardedRouter(false)
    const user = userEvent.setup()
    render(<RouterProvider router={router} />)

    await user.click(screen.getByRole('link', { name: 'Leave' }))

    expect(await screen.findByRole('heading', { name: 'Tests' })).toBeInTheDocument()
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
  })

  it('requests browser confirmation before unloading an active attempt', () => {
    const router = createGuardedRouter()
    render(<RouterProvider router={router} />)
    const event = new Event('beforeunload', { cancelable: true })

    window.dispatchEvent(event)

    expect(event.defaultPrevented).toBe(true)
  })
})
