export type LeagueCountdown = { days: number; time: string }

export function serverClockOffset(serverNowUtc: string | undefined, clientNowMs: number) {
  return serverNowUtc ? new Date(serverNowUtc).getTime() - clientNowMs : 0
}

export function leagueCountdown(endsAtUtc: string | null | undefined, clientNowMs: number, offsetMs: number): LeagueCountdown {
  const left = endsAtUtc ? Math.max(0, new Date(endsAtUtc).getTime() - (clientNowMs + offsetMs)) : 0
  const seconds = Math.floor(left / 1000)
  const days = Math.floor(seconds / 86400)
  const hours = Math.floor(seconds % 86400 / 3600)
  const minutes = Math.floor(seconds % 3600 / 60)
  const rest = seconds % 60
  return { days, time: `${pad(hours)}:${pad(minutes)}:${pad(rest)}` }
}

function pad(value: number) {
  return String(value).padStart(2, '0')
}

