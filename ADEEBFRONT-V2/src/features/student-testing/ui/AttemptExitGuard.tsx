import { AlertTriangle } from 'lucide-react'
import { type RefObject, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useBeforeUnload, useBlocker } from 'react-router-dom'
import { TestingButton } from './TestingUi'

type AttemptExitGuardProps = {
  active: boolean
  allowExit: RefObject<boolean>
  finishing: boolean
  unanswered: number
  onFinish: () => void
}

export function AttemptExitGuard({ active, allowExit, finishing, unanswered, onFinish }: AttemptExitGuardProps) {
  const { t } = useTranslation()
  const blocker = useBlocker(({ currentLocation, nextLocation }) =>
    active && !allowExit.current && currentLocation.pathname !== nextLocation.pathname)

  useBeforeUnload(useCallback((event) => {
    if (!active || allowExit.current) return
    event.preventDefault()
    event.returnValue = ''
  }, [active, allowExit]))

  if (blocker.state !== 'blocked') return null

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/45 p-4" role="presentation">
      <div
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="exit-test-title"
        aria-describedby="exit-test-description"
        className="w-full max-w-md rounded-lg bg-[var(--student-surface)] p-5 shadow-2xl"
      >
        <span className="grid h-11 w-11 place-items-center rounded-lg bg-amber-50 text-amber-700">
          <AlertTriangle className="h-5 w-5" />
        </span>
        <h2 id="exit-test-title" className="mt-4 text-xl font-black">{t('student.testing.exitTitle')}</h2>
        <p id="exit-test-description" className="mt-2 text-sm leading-6 text-[var(--student-muted)]">
          {t('student.testing.exitDescription', { count: unanswered })}
        </p>
        <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <TestingButton variant="secondary" onClick={() => blocker.reset()} disabled={finishing}>
            {t('student.testing.stayInTest')}
          </TestingButton>
          <TestingButton onClick={() => { blocker.reset(); onFinish() }} disabled={finishing} autoFocus>
            {finishing ? t('student.testing.submitting') : t('student.testing.finishAndExit')}
          </TestingButton>
        </div>
      </div>
    </div>
  )
}
