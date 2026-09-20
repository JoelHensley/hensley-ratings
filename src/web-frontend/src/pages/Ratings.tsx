import { useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { RatedTeam } from '../api/types'
import { buildRatingsPath, buildTeamPath } from '../util/paths'
import { parseRatingsPath } from '../util/parseRatingsPath'
import { slugify } from '../util/slugify'
import Team from './Team'

const DIV_ABBREV: Record<string, string> = {
  'Division-II': 'D-II',
  'Division-III': 'D-III',
}
const shortDivName = (name: string) => DIV_ABBREV[name] ?? name

type SortKey = 'rankOverall' | 'hensleyRating' | 'scheduleStrength' | 'wins' | 'weekOverWeekChange'
type SortDir = 'asc' | 'desc'

export default function Ratings() {
  const { '*': splat } = useParams()
  const parsed = parseRatingsPath(splat)
  const nav = useNavigate()

  // If the path encodes a team page, delegate to Team
  if (parsed.teamId) {
    return <Team teamId={parsed.teamId} year={parsed.year} />
  }

  return <RatingsView parsed={parsed} nav={nav} />
}

function RatingsView({
  parsed,
  nav,
}: {
  parsed: ReturnType<typeof parseRatingsPath>
  nav: ReturnType<typeof useNavigate>
}) {
  const { data: yearsData } = useQuery({
    queryKey: ['years'],
    queryFn: () => api.years(),
  })

  const latestAvailableYear = yearsData?.years[0] ?? new Date().getFullYear()
  const year = parsed.year ?? latestAvailableYear
  const divisionId = parsed.divId
  const conferenceId = parsed.confId
  const weekFromUrl = parsed.week ?? 0

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

  const availableWeeks = weeksData?.weeks ?? []
  const latestWeek = availableWeeks.at(-1)?.week ?? 0
  const activeWeek = weekFromUrl > 0 ? weekFromUrl : latestWeek

  const { data: ratings, isLoading, error } = useQuery({
    queryKey: ['ratings', year, activeWeek, divisionId, conferenceId],
    queryFn: () => api.ratings(year, activeWeek, divisionId, conferenceId),
    enabled: activeWeek > 0,
  })

  const divisions = divisionsData ?? []
  const conferences = conferencesData ?? []

  const activeDivision = divisions.find((d) => d.divisionId === divisionId)
  const activeConference = conferences.find((c) => c.conferenceId === conferenceId)
  const divisionName = shortDivName(activeDivision?.name ?? 'All')

  const goTo = (
    toYear: number,
    toWeek: number | undefined,
    toDiv: { id: number; name: string } | undefined,
    toConf?: { id: number; name: string },
  ) => nav(buildRatingsPath(toYear, toWeek, toDiv, toConf))

  // Default to FBS when no division is explicitly set (and not "All")
  useEffect(() => {
    if (divisionId === undefined && !parsed.isAll && activeWeek > 0 && divisions.length > 0) {
      const fbs = divisions.find((d) => d.divisionId === 1) ?? divisions[0]
      nav(buildRatingsPath(year, activeWeek, { id: fbs.divisionId, name: fbs.name }), { replace: true })
    }
  }, [divisionId, parsed.isAll, activeWeek, year, divisions.length])

  const sortedRatings = (ratings ?? []).slice().sort((a, b) => {
    const mult = sort.dir === 'asc' ? 1 : -1
    const aVal = a[sort.key] ?? (sort.dir === 'asc' ? Infinity : -Infinity)
    const bVal = b[sort.key] ?? (sort.dir === 'asc' ? Infinity : -Infinity)
    return ((aVal as number) - (bVal as number)) * mult
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

  useEffect(() => {
    const pageTitle = `${divisionName} Ratings | Week ${activeWeek} | ${year} | Hensley Ratings`
    document.title = pageTitle
    const setMeta = (sel: string, attrName: string, attrVal: string, content: string) => {
      let el = document.querySelector(sel) as HTMLMetaElement | null
      if (!el) {
        el = document.createElement('meta')
        el.setAttribute(attrName, attrVal)
        document.head.appendChild(el)
      }
      el.setAttribute('content', content)
    }
    const top5 = (ratings ?? []).slice(0, 5)
      .map((t, i) => `#${i + 1} ${t.name} (${t.wins}-${t.losses})`)
      .join(', ')
    const desc = top5
      ? `Top 5 for this week: ${top5}`
      : `The Hensley Ratings for ${divisionName} football for week ${activeWeek} in ${year}.`
    setMeta('meta[name="description"]', 'name', 'description', desc)
    setMeta('meta[property="og:title"]', 'property', 'og:title', `Week ${activeWeek}`)
    setMeta('meta[property="og:description"]', 'property', 'og:description', desc)
    setMeta('meta[property="og:url"]', 'property', 'og:url', window.location.href)
    setMeta('meta[name="twitter:title"]', 'name', 'twitter:title', `Hensley Ratings for week ${activeWeek}`)
    setMeta('meta[name="twitter:description"]', 'name', 'twitter:description', desc)
  }, [divisionName, activeWeek, year, ratings])

  return (
    <main className="page">
      <h1 className="page-heading">
        {divisionName}{activeWeek > 0 && ` — Week ${activeWeek}`}
      </h1>

      {/* Year picker */}
      {yearsData && yearsData.years.length > 1 && (
        <div className="year-nav">
          {yearsData.years.slice().reverse().map((y) => (
            <span key={y}>
              <Link
                to={buildRatingsPath(y, undefined, activeDivision ? { id: activeDivision.divisionId, name: activeDivision.name } : undefined)}
                className={`year-link${y === year ? ' active' : ''}`}
              >
                {y}
              </Link>
            </span>
          ))}
        </div>
      )}

      {/* Week navigation */}
      {availableWeeks.length > 0 && (
        <div className="week-nav" role="navigation" aria-label="Week">
          {availableWeeks.map((w) => (
            <button
              key={w.week}
              className={`week-pill${w.week === activeWeek ? ' active' : ''}`}
              onClick={() => goTo(
                year, w.week,
                activeDivision ? { id: activeDivision.divisionId, name: activeDivision.name } : undefined,
                activeConference ? { id: activeConference.conferenceId, name: activeConference.name } : undefined,
              )}
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
          onClick={() => goTo(year, activeWeek, undefined)}
        >
          All
        </button>
        {divisions.map((d) => (
          <button
            key={d.divisionId}
            role="tab"
            aria-selected={divisionId === d.divisionId}
            className={`filter-tab${divisionId === d.divisionId ? ' active' : ''}`}
            onClick={() => goTo(year, activeWeek, { id: d.divisionId, name: d.name })}
          >
            {shortDivName(d.name)}
          </button>
        ))}
      </div>

      {/* Conference filter (only when a division is selected) */}
      {divisionId !== undefined && conferences.length > 0 && (
        <div className="filter-bar conf-bar" role="tablist">
          <button
            role="tab"
            aria-selected={!conferenceId}
            className={`filter-tab${!conferenceId ? ' active' : ''}`}
            onClick={() => goTo(year, activeWeek, activeDivision ? { id: activeDivision.divisionId, name: activeDivision.name } : undefined)}
          >
            All conferences
          </button>
          {conferences.map((c) => (
            <button
              key={c.conferenceId}
              role="tab"
              aria-selected={conferenceId === c.conferenceId}
              className={`filter-tab${conferenceId === c.conferenceId ? ' active' : ''}`}
              onClick={() => goTo(
                year, activeWeek,
                activeDivision ? { id: activeDivision.divisionId, name: activeDivision.name } : undefined,
                { id: c.conferenceId, name: c.name },
              )}
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
                <th
                  className={sort.key === 'weekOverWeekChange' ? 'sorted' : ''}
                  onClick={() => toggleSort('weekOverWeekChange')}
                >
                  +/−{sortArrow('weekOverWeekChange')}
                </th>
              </tr>
            </thead>
            <tbody>
              {sortedRatings.map((team) => (
                <RatingRow key={team.teamId} team={team} year={year} activeConferenceId={conferenceId} />
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

function RatingRow({ team, year, activeConferenceId }: { team: RatedTeam; year: number; activeConferenceId?: number }) {
  const change = team.weekOverWeekChange
  let changeClass = 'same'
  let changeLabel = '—'
  if (change !== null && change !== 0) {
    changeClass = change > 0 ? 'up' : 'down'
    changeLabel = `${change > 0 ? '+' : ''}${change}`
  }

  const teamPath = team.divisionId && team.conferenceId
    ? buildTeamPath(year, team.teamId, team.name, { id: team.divisionId, name: team.divisionName }, { id: team.conferenceId, name: team.conferenceName })
    : `/teams/${team.teamId}?year=${year}&name=${slugify(team.name)}`

  return (
    <tr>
      <td>
        {activeConferenceId ? (
          <span className="rank-num">
            #{team.rankConference}
            <span style={{ fontSize: '0.75em', fontWeight: 400, color: 'var(--muted)', marginLeft: 3 }}>
              (#{team.rankDivision})
            </span>
          </span>
        ) : (
          <span className="rank-num">#{team.rankOverall}</span>
        )}
      </td>
      <td className="left">
        <Link to={teamPath} className="team-link">
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
