import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import type { RatedTeam } from '../api/types'
import { slugify } from '../util/slugify'

type SortKey = 'rankOverall' | 'hensleyRating' | 'scheduleStrength' | 'wins'
type SortDir = 'asc' | 'desc'

export default function Ratings() {
  const [params, setParams] = useSearchParams()

  const { data: yearsData } = useQuery({
    queryKey: ['years'],
    queryFn: () => api.years(),
  })

  const latestAvailableYear = yearsData?.years[0] ?? new Date().getFullYear()
  const year = Number(params.get('year') ?? latestAvailableYear)
  const week = Number(params.get('week') ?? 0)
  const divisionId = params.get('divisionId') ? Number(params.get('divisionId')) : undefined
  const conferenceId = params.get('conferenceId') ? Number(params.get('conferenceId')) : undefined

  const [sort, setSort] = useState<{ key: SortKey; dir: SortDir }>({
    key: 'rankOverall',
    dir: 'asc',
  })

  const { data: weeksData } = useQuery({
    queryKey: ['weeks', year],
    queryFn: () => api.weeks(year),
    enabled: !!yearsData,
  })

  const { data: divisionsData } = useQuery({
    queryKey: ['divisions'],
    queryFn: () => api.divisions(),
  })

  const { data: conferencesData } = useQuery({
    queryKey: ['conferences', year, divisionId],
    queryFn: () => api.conferences(year, divisionId),
    enabled: divisionId !== undefined,
  })

  // Use the latest available week if none specified
  const availableWeeks = weeksData?.weeks ?? []
  const latestWeek = availableWeeks.at(-1)?.week ?? 0
  const activeWeek = week > 0 ? week : latestWeek

  const { data: ratings, isLoading, error } = useQuery({
    queryKey: ['ratings', year, activeWeek, divisionId, conferenceId],
    queryFn: () => api.ratings(year, activeWeek, divisionId, conferenceId),
    enabled: activeWeek > 0,
  })

  const navigate = (updates: Record<string, string | undefined>) => {
    const next = new URLSearchParams(params)
    for (const [k, v] of Object.entries(updates)) {
      if (v === undefined) next.delete(k)
      else next.set(k, v)
    }
    setParams(next)
  }

  const sortedRatings = (ratings ?? []).slice().sort((a, b) => {
    const mult = sort.dir === 'asc' ? 1 : -1
    return (a[sort.key] - b[sort.key]) * mult
  })

  const toggleSort = (key: SortKey) => {
    setSort((prev) =>
      prev.key === key
        ? { key, dir: prev.dir === 'asc' ? 'desc' : 'asc' }
        : { key, dir: key === 'rankOverall' ? 'asc' : 'desc' },
    )
  }

  const sortArrow = (key: SortKey) =>
    sort.key === key ? (sort.dir === 'asc' ? ' ↑' : ' ↓') : ''

  const divisions = divisionsData ?? []
  const conferences = conferencesData ?? []

  return (
    <main className="page">
      <h1 className="page-heading">
        {divisionId
          ? (divisions.find((d) => d.divisionId === divisionId)?.name ?? 'Ratings')
          : 'Ratings'}
        {activeWeek > 0 && ` — Week ${activeWeek}`}
      </h1>

      {/* Week navigation */}
      {availableWeeks.length > 0 && (
        <div className="week-nav" role="navigation" aria-label="Week">
          {availableWeeks.map((w) => (
            <button
              key={w.week}
              className={`week-pill${w.week === activeWeek ? ' active' : ''}`}
              onClick={() => navigate({ week: String(w.week) })}
              aria-current={w.week === activeWeek ? 'page' : undefined}
            >
              Wk {w.week}
            </button>
          ))}
        </div>
      )}

      {/* Division filter */}
      <div className="filter-bar" role="tablist">
        <button
          role="tab"
          aria-selected={!divisionId}
          className={`filter-tab${!divisionId ? ' active' : ''}`}
          onClick={() => navigate({ divisionId: undefined, conferenceId: undefined })}
        >
          All
        </button>
        {divisions.map((d) => (
          <button
            key={d.divisionId}
            role="tab"
            aria-selected={divisionId === d.divisionId}
            className={`filter-tab${divisionId === d.divisionId ? ' active' : ''}`}
            onClick={() =>
              navigate({ divisionId: String(d.divisionId), conferenceId: undefined })
            }
          >
            {d.name}
          </button>
        ))}
      </div>

      {/* Conference filter (only when a division is selected) */}
      {divisionId !== undefined && conferences.length > 0 && (
        <div className="filter-bar" style={{ marginTop: '-14px' }} role="tablist">
          <button
            role="tab"
            aria-selected={!conferenceId}
            className={`filter-tab${!conferenceId ? ' active' : ''}`}
            onClick={() => navigate({ conferenceId: undefined })}
          >
            All conferences
          </button>
          {conferences.map((c) => (
            <button
              key={c.conferenceId}
              role="tab"
              aria-selected={conferenceId === c.conferenceId}
              className={`filter-tab${conferenceId === c.conferenceId ? ' active' : ''}`}
              onClick={() => navigate({ conferenceId: String(c.conferenceId) })}
            >
              {c.name}
            </button>
          ))}
        </div>
      )}

      {isLoading && <div className="loading">Loading ratings…</div>}
      {error && <div className="error">Failed to load ratings. Is the API running?</div>}

      {!isLoading && !error && (
        <div className="table-wrap">
          <table className="ratings-table">
            <thead>
              <tr>
                <th
                  className={`left${sort.key === 'rankOverall' ? ' sorted' : ''}`}
                  onClick={() => toggleSort('rankOverall')}
                >
                  Rank{sortArrow('rankOverall')}
                </th>
                <th
                  className={`left${sort.key === 'hensleyRating' ? ' sorted' : ''}`}
                  onClick={() => toggleSort('hensleyRating')}
                >
                  Team{sortArrow('hensleyRating')}
                </th>
                <th
                  className={sort.key === 'wins' ? 'sorted' : ''}
                  onClick={() => toggleSort('wins')}
                >
                  Record{sortArrow('wins')}
                </th>
                <th
                  className={sort.key === 'hensleyRating' ? 'sorted' : ''}
                  onClick={() => toggleSort('hensleyRating')}
                >
                  Rating{sortArrow('hensleyRating')}
                </th>
                <th
                  className={sort.key === 'scheduleStrength' ? 'sorted' : ''}
                  onClick={() => toggleSort('scheduleStrength')}
                >
                  Sched Strength{sortArrow('scheduleStrength')}
                </th>
                <th>+/−</th>
              </tr>
            </thead>
            <tbody>
              {sortedRatings.map((team) => (
                <RatingRow key={team.teamId} team={team} year={year} />
              ))}
            </tbody>
          </table>
          {sortedRatings.length === 0 && (
            <div className="empty">No ratings available for this selection.</div>
          )}
        </div>
      )}
    </main>
  )
}

function RatingRow({ team, year }: { team: RatedTeam; year: number }) {
  const change = team.weekOverWeekChange
  let changeClass = 'same'
  let changeLabel = '—'
  if (change !== null && change !== 0) {
    changeClass = change > 0 ? 'up' : 'down'
    changeLabel = `${change > 0 ? '+' : ''}${change}`
  }

  return (
    <tr>
      <td>
        <span className="rank-num">#{team.rankOverall}</span>
      </td>
      <td>
        <Link
          to={`/teams/${team.teamId}?year=${year}&name=${slugify(team.name)}`}
          className="team-link"
        >
          {team.name}
        </Link>
        <span className="conf-badge">{team.conferenceName}</span>
      </td>
      <td>
        <span className="record">
          {team.wins}–{team.losses}
        </span>
      </td>
      <td>
        <span className="rating-val">{team.hensleyRating.toFixed(3)}</span>
      </td>
      <td>{team.scheduleStrength.toFixed(3)}</td>
      <td>
        <span className={`rank-change ${changeClass}`}>{changeLabel}</span>
      </td>
    </tr>
  )
}
