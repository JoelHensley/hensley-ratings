import type {
  Conference,
  Division,
  MetaWeeks,
  MetaYears,
  RatedTeam,
  ScheduleGame,
  TeamDetail,
} from './types'

async function get<T>(path: string): Promise<T> {
  const res = await fetch(path)
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  return res.json() as Promise<T>
}

const qs = (params: Record<string, string | number | undefined>) =>
  Object.entries(params)
    .filter(([, v]) => v !== undefined)
    .map(([k, v]) => `${k}=${encodeURIComponent(v!)}`)
    .join('&')

export const api = {
  ratings(year: number, week: number, divisionId?: number, conferenceId?: number) {
    return get<RatedTeam[]>(
      `/api/ratings?${qs({ year, week, divisionId, conferenceId })}`,
    )
  },

  schedule(year: number, week: number, divisionId?: number, conferenceId?: number) {
    return get<ScheduleGame[]>(
      `/api/schedule?${qs({ year, week, divisionId, conferenceId })}`,
    )
  },

  team(id: number, year: number) {
    return get<TeamDetail>(`/api/teams/${id}?year=${year}`)
  },

  years() {
    return get<MetaYears>('/api/meta/years')
  },

  weeks(year: number) {
    return get<MetaWeeks>(`/api/meta/weeks?year=${year}`)
  },

  divisions() {
    return get<Division[]>('/api/meta/divisions')
  },

  conferences(year: number, divisionId?: number) {
    return get<Conference[]>(
      `/api/meta/conferences?${qs({ year, divisionId })}`,
    )
  },
}
