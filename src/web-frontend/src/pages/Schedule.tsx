import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import type { ScheduleGame } from '../api/types'
import { slugify } from '../util/slugify'

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  })
}

function groupByDate(games: ScheduleGame[]) {
  const groups = new Map<string, ScheduleGame[]>()
  for (const g of games) {
    const key = g.date.slice(0, 10)
    if (!groups.has(key)) groups.set(key, [])
    groups.get(key)!.push(g)
  }
  return groups
}

export default function Schedule() {
  const [params, setParams] = useSearchParams()

  const { data: yearsData } = useQuery({
    queryKey: ['years'],
    queryFn: () => api.years(),
  })

  const latestAvailableYear = yearsData?.years[0] ?? new Date().getFullYear()
  const year = Number(params.get('year') ?? latestAvailableYear)
  const week = Number(params.get('week') ?? 0)
  const divisionId = params.get('divisionId') ? Number(params.get('divisionId')) : undefined

  const { data: weeksData } = useQuery({
    queryKey: ['weeks', year],
    queryFn: () => api.weeks(year),
    enabled: !!yearsData,
  })

  const { data: divisionsData } = useQuery({
    queryKey: ['divisions'],
    queryFn: () => api.divisions(),
  })

  const availableWeeks = weeksData?.weeks ?? []
  const latestWeek = availableWeeks.at(-1)?.week ?? 0
  const activeWeek = week > 0 ? week : latestWeek

  const { data: games, isLoading, error } = useQuery({
    queryKey: ['schedule', year, activeWeek, divisionId],
    queryFn: () => api.schedule(year, activeWeek, divisionId),
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

  const divisions = divisionsData ?? []
  const grouped = games ? groupByDate(games) : new Map()

  // "Game of the Week" = lowest sum of ranks (both teams highest-rated)
  const gotw = games?.filter((g) => g.isComplete).reduce<ScheduleGame | null>((best, g) => {
    if (!g.homeTeamRank || !g.awayTeamRank) return best
    const score = g.homeTeamRank + g.awayTeamRank
    if (!best) return g
    const bestScore = (best.homeTeamRank ?? 999) + (best.awayTeamRank ?? 999)
    return score < bestScore ? g : best
  }, null)

  return (
    <main className="page">
      <h1 className="page-heading">
        Schedule &amp; Results{activeWeek > 0 && ` — Week ${activeWeek}`}
      </h1>

      {/* Week nav */}
      {availableWeeks.length > 0 && (
        <div className="week-nav" role="navigation" aria-label="Week">
          {availableWeeks.map((w) => (
            <button
              key={w.week}
              className={`week-pill${w.week === activeWeek ? ' active' : ''}`}
              onClick={() => navigate({ week: String(w.week) })}
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
          onClick={() => navigate({ divisionId: undefined })}
        >
          All
        </button>
        {divisions.map((d) => (
          <button
            key={d.divisionId}
            role="tab"
            aria-selected={divisionId === d.divisionId}
            className={`filter-tab${divisionId === d.divisionId ? ' active' : ''}`}
            onClick={() => navigate({ divisionId: String(d.divisionId) })}
          >
            {d.name}
          </button>
        ))}
      </div>

      {isLoading && <div className="loading">Loading schedule…</div>}
      {error && <div className="error">Failed to load schedule. Is the API running?</div>}

      {!isLoading && !error && gotw && (
        <section style={{ marginBottom: 28 }}>
          <div className="schedule-date-label">⭐ Game of the Week</div>
          <GameCard game={gotw} year={year} isGotw />
        </section>
      )}

      {!isLoading && !error && (
        <>
          {Array.from(grouped.entries()).map(([date, dayGames]) => (
            <section key={date} className="schedule-date-group">
              <div className="schedule-date-label">{formatDate(date)}</div>
              {dayGames.map((g: ScheduleGame) => (
                <GameCard key={g.gameId} game={g} year={year} isGotw={g.gameId === gotw?.gameId} />
              ))}
            </section>
          ))}
          {games?.length === 0 && (
            <div className="empty">No games found for this selection.</div>
          )}
        </>
      )}
    </main>
  )
}

function TeamRef({ name, id, rank, year }: { name: string; id: number; rank: number | null; year: number }) {
  return (
    <Link to={`/teams/${id}?year=${year}&name=${slugify(name)}`} className="team-link">
      {rank && rank <= 25 ? <span style={{ color: 'var(--accent)', marginRight: 3 }}>#{rank}</span> : null}
      {name}
    </Link>
  )
}

function GameCard({ game, year, isGotw }: { game: ScheduleGame; year: number; isGotw: boolean }) {
  const isComplete = game.isComplete

  return (
    <div className={`game-card${isGotw ? ' gotw' : ''}`}>
      <div className="game-row">
        {/* Away */}
        <div className="game-team away">
          <TeamRef name={game.awayTeamName} id={game.awayTeamId} rank={game.awayTeamRank} year={year} />
          <div style={{ fontSize: 12, color: 'var(--muted)', marginTop: 2 }}>{game.awayTeamConference}</div>
        </div>

        {/* Score / prediction */}
        <div className="game-score-area">
          {isComplete ? (
            <div className="game-score">
              {game.awayScore! > game.homeScore! ? (
                <>
                  <span className="winner">{game.awayScore}</span>
                  <span className="sep">–</span>
                  <span className="loser">{game.homeScore}</span>
                </>
              ) : (
                <>
                  <span className="loser">{game.awayScore}</span>
                  <span className="sep">–</span>
                  <span className="winner">{game.homeScore}</span>
                </>
              )}
            </div>
          ) : (
            <>
              <div style={{ fontSize: 12, color: 'var(--muted)', fontStyle: 'italic' }}>
                {game.isNeutralSite ? 'Neutral' : 'vs'}
              </div>
              {game.predictedAwayScore !== null && game.predictedHomeScore !== null && (
                <div className="game-prediction">
                  Pred: {game.predictedAwayScore}–{game.predictedHomeScore}
                </div>
              )}
            </>
          )}
        </div>

        {/* Home */}
        <div className="game-team home">
          <TeamRef name={game.homeTeamName} id={game.homeTeamId} rank={game.homeTeamRank} year={year} />
          <div style={{ fontSize: 12, color: 'var(--muted)', marginTop: 2 }}>{game.homeTeamConference}</div>
        </div>
      </div>

      {/* Stats comparison row */}
      {(game.homeTeamRating !== null || game.awayTeamRating !== null) && (
        <div className="game-stats-row">
          <div className="stat-cell">
            <span className="stat-label">Rating</span>
            <span className={`stat-val${(game.awayTeamRating ?? 0) > (game.homeTeamRating ?? 0) ? ' stat-edge' : ''}`}>
              {game.awayTeamRating?.toFixed(3) ?? '—'}
            </span>
          </div>
          <div className="stat-cell">
            <span className="stat-label">Rating</span>
            <span className={`stat-val${(game.homeTeamRating ?? 0) > (game.awayTeamRating ?? 0) ? ' stat-edge' : ''}`}>
              {game.homeTeamRating?.toFixed(3) ?? '—'}
            </span>
          </div>
        </div>
      )}
    </div>
  )
}
