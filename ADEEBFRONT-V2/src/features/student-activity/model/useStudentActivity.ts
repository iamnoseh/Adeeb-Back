import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useRef } from 'react'
import { studentActivityApi, studentActivityKeys } from '@/features/student-activity/api/student-activity.api'
import { browserTimeZone, millisecondsUntilNextLocalMidnight } from '@/features/student-activity/lib/student-activity'

export function useCurrentStudentActivity() {
  return useQuery({
    queryKey: studentActivityKeys.current(),
    queryFn: () => studentActivityApi.calendar(),
    staleTime: 60_000,
  })
}

export function useStudentActivityVisit() {
  const queryClient = useQueryClient()
  const pending = useRef(false)
  const mutation = useMutation({
    mutationFn: () => studentActivityApi.recordVisit(browserTimeZone()),
    retry: 3,
    retryDelay: (attempt) => Math.min(1_000 * 2 ** attempt, 10_000),
    onMutate: () => { pending.current = true },
    onSuccess: (calendar) => {
      queryClient.setQueryData(studentActivityKeys.current(), calendar)
      queryClient.setQueryData(studentActivityKeys.month(calendar.year, calendar.month), calendar)
    },
    onSettled: () => { pending.current = false },
  })
  const mutateRef = useRef(mutation.mutate)
  mutateRef.current = mutation.mutate
  const recordVisit = useCallback(() => {
    if (!pending.current) mutateRef.current()
  }, [])

  useEffect(() => {
    recordVisit()
    let midnightTimer = window.setTimeout(function markNextDay() {
      recordVisit()
      midnightTimer = window.setTimeout(markNextDay, millisecondsUntilNextLocalMidnight())
    }, millisecondsUntilNextLocalMidnight())
    const onVisibilityChange = () => {
      if (document.visibilityState === 'visible') recordVisit()
    }
    document.addEventListener('visibilitychange', onVisibilityChange)
    return () => {
      window.clearTimeout(midnightTimer)
      document.removeEventListener('visibilitychange', onVisibilityChange)
    }
  }, [recordVisit])
}
