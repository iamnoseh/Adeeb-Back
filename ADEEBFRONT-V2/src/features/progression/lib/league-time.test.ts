import { describe, expect, it } from 'vitest'
import { leagueCountdown, serverClockOffset } from './league-time'

describe('league countdown', () => {
  it('uses server time instead of the browser clock', () => {
    const clientNow = Date.parse('2026-07-21T10:00:00Z')
    const offset = serverClockOffset('2026-07-21T09:55:00Z', clientNow)

    expect(leagueCountdown('2026-07-22T10:55:01Z', clientNow, offset))
      .toEqual({ days: 1, time: '01:00:01' })
  })

  it('never returns a negative countdown after season end', () => {
    expect(leagueCountdown('2026-07-21T09:00:00Z', Date.parse('2026-07-21T10:00:00Z'), 0))
      .toEqual({ days: 0, time: '00:00:00' })
  })
})

