import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import type { ByeWeek, TeamGame } from '../api/types'
import { XLogo } from '../components/XLogo'
import { YearPicker } from '../components/YearPicker'
import { buildRatingsPath, buildTeamPath } from '../util/paths'
import { slugify } from '../util/slugify'

type LogEntry = { kind: 'game'; game: TeamGame } | { kind: 'bye'; bye: ByeWeek }

function buildMergedLog(games: TeamGame[], byeWeeks: ByeWeek[]): LogEntry[] {
  const result: LogEntry[] = []
  const sortedByes = [...byeWeeks].sort((a, b) => a.week - b.week)
  let byeIdx = 0
  for (const game of games) {
    while (byeIdx < sortedByes.length && (game.weekNumber == null || sortedByes[byeIdx].week < game.weekNumber)) {
      result.push({ kind: 'bye', bye: sortedByes[byeIdx++] })
    }
    result.push({ kind: 'game', game })
  }
  while (byeIdx < sortedByes.length) result.push({ kind: 'bye', bye: sortedByes[byeIdx++] })
  return result
}

const DIV_ABBREV: Record<string, string> = {
  'Division-II': 'D-II',
  'Division-III': 'D-III',
  'NCAA Division II': 'D-II',
  'NCAA Division III': 'D-III',
}
const shortDivName = (name: string) => DIV_ABBREV[name] ?? name

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

function PredictionBadge({ team, opp }: { team: number; opp: number }) {
  const [visible, setVisible] = useState(false)
  const teamWins = team > opp

  return (
    <div
      className="prediction-badge prediction-badge--inline"
      onMouseEnter={() => setVisible(true)}
      onMouseLeave={() => setVisible(false)}
      onFocus={() => setVisible(true)}
      onBlur={() => setVisible(false)}
      tabIndex={0}
      role="button"
      aria-label={`Predicted score: ${team}–${opp}`}
    >
      <div className="prediction-eyebrow">
        Prediction
        <svg className="prediction-info" aria-hidden="true" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <circle cx="8" cy="8" r="7" stroke="currentColor" strokeWidth="1.25"/>
          <rect x="7.25" y="7" width="1.5" height="5" rx="0.75" fill="currentColor"/>
          <circle cx="8" cy="5" r="0.85" fill="currentColor"/>
        </svg>
      </div>
      <div className="prediction-scores">
        <span className={teamWins ? 'pred-winner' : 'pred-loser'}>{team}</span>
        <span className="pred-sep">–</span>
        <span className={!teamWins ? 'pred-winner' : 'pred-loser'}>{opp}</span>
      </div>
      <div className={`prediction-tooltip prediction-tooltip--down${visible ? ' visible' : ''}`} role="tooltip">
        <div className="pred-tip-title">How this prediction works</div>
        <div className="pred-tip-body">
          <div className="pred-tip-row">
            <span className="pred-tip-bullet">◆</span>
            <span>The Hensley Rating gap between teams sets the predicted margin.</span>
          </div>
          <div className="pred-tip-row">
            <span className="pred-tip-bullet">◆</span>
            <span>Scores are derived by blending each team's scoring average with their opponent's points-allowed average, then averaging two independent formulas.</span>
          </div>
          <div className="pred-tip-row">
            <span className="pred-tip-bullet">◆</span>
            <span>The home team receives a +1 rating advantage unless the game is at a neutral site.</span>
          </div>
        </div>
      </div>
    </div>
  )
}

interface TeamProps { teamId?: number; year?: number }
export default function Team({ teamId: propTeamId, year: propYear }: TeamProps = {}) {
  const { id } = useParams<{ id?: string }>()
  const [params] = useSearchParams()
  const { data: yearsData } = useQuery({
    queryKey: ['years'],
    queryFn: () => api.years(),
  })

  const latestAvailableYear = yearsData?.years[0] ?? new Date().getFullYear()
  const effectiveId = propTeamId ?? (id ? Number(id) : undefined)
  const year = propYear ?? Number(params.get('year') ?? latestAvailableYear)

  const { data: team, isLoading, error } = useQuery({
    queryKey: ['team', effectiveId, year],
    queryFn: () => api.team(effectiveId!, year),
    enabled: !!effectiveId,
  })

  const mergedLog = useMemo(
    () => team ? buildMergedLog(team.games, team.byeWeeks) : [],
    [team],
  )

  if (isLoading) return <main className="page"><div className="loading">Loading team…</div></main>
  if (error || !team) return <main className="page"><div className="error">Team not found.</div></main>

  const avgPtsScored = team.pointsScored && team.games.length
    ? (team.pointsScored / Math.max(team.wins + team.losses, 1)).toFixed(1)
    : null
  const avgPtsAllowed = team.pointsAllowed && team.games.length
    ? (team.pointsAllowed / Math.max(team.wins + team.losses, 1)).toFixed(1)
    : null

  const divOpt = team.divisionId ? { id: team.divisionId, name: team.divisionName } : undefined
  const confOpt = team.conferenceId ? { id: team.conferenceId, name: team.conferenceName } : undefined

  const buildYearPath = (y: number) =>
    divOpt && confOpt
      ? buildTeamPath(y, team.teamId, team.name, divOpt, confOpt)
      : `/teams/${team.teamId}?year=${y}&name=${slugify(team.name)}`

  return (
    <main className="page">
      <div style={{ marginBottom: 8, fontSize: 16 }}>
        <Link to={buildRatingsPath(year, undefined, undefined)} style={{ color: 'var(--muted)' }}>All</Link>
        {divOpt && (
          <>
            {' / '}
            <Link to={buildRatingsPath(year, undefined, divOpt)} style={{ color: 'var(--muted)' }}>
              {shortDivName(team.divisionName)}
            </Link>
          </>
        )}
        {confOpt && divOpt && (
          <>
            {' / '}
            <Link to={buildRatingsPath(year, undefined, divOpt, confOpt)} style={{ color: 'var(--muted)' }}>
              {team.conferenceName}
            </Link>
          </>
        )}
        {' / '}
        <span style={{ color: 'var(--text)' }}>{team.name}</span>
      </div>

      {/* Year picker */}
      {yearsData && yearsData.years.length > 0 && (
        <YearPicker
          years={yearsData.years}
          activeYear={year}
          buildPath={buildYearPath}
        />
      )}

      <div className="team-header">
        <div>
          <h1 className="team-name">{team.name}</h1>
          <a href="https://x.com/HensleyRatings" target="_blank" rel="noopener noreferrer" className="page-x-link"><XLogo />@HensleyRatings</a>
        </div>

        <div className="team-stat-row">
          <div className="team-stat">
            <span className="team-stat-val" style={{ color: 'var(--accent)' }}>
              {team.hensleyRating.toFixed(3)}
            </span>
            <span className="team-stat-label">Rating</span>
          </div>
          <div className="team-stat">
            <span className="team-stat-val">#{team.rankOverall}</span>
            <span className="team-stat-label">Rank</span>
          </div>
          <div className="team-stat">
            <span className="team-stat-val">
              {team.wins}–{team.losses}
            </span>
            <span className="team-stat-label">Record</span>
          </div>
          {avgPtsScored && (
            <div className="team-stat">
              <span className="team-stat-val">{avgPtsScored}</span>
              <span className="team-stat-label">Pts/Gm</span>
            </div>
          )}
          {avgPtsAllowed && (
            <div className="team-stat">
              <span className="team-stat-val">{avgPtsAllowed}</span>
              <span className="team-stat-label">Pts Alwd</span>
            </div>
          )}
          <div className="team-stat">
            <span className="team-stat-val">{team.scheduleStrength.toFixed(3)}</span>
            <span className="team-stat-label">Sched Str</span>
          </div>
        </div>
      </div>

      <section className="team-game-log">
        <div className="game-log-scroll-outer">
        <div className="game-log-scroll">
          <div
            className="game-log-row game-log-header"
            style={{ fontFamily: 'var(--font-display)', fontSize: 14, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--muted)', borderBottom: '2px solid var(--border)' }}
          >
            <span>Date</span>
            <span>Opponent</span>
            <span>Result</span>
            <span>Record</span>
            <span>Rating</span>
            <span>Sch Str</span>
          </div>
          {mergedLog.map((entry) => {
            if (entry.kind === 'bye') {
              const b = entry.bye
              return (
                <div key={`bye-${b.week}`} className="game-log-row game-log-bye-row">
                  <span />
                  <span style={{ color: 'var(--muted)', fontFamily: 'var(--font-display)', fontSize: 12, letterSpacing: '0.08em', textTransform: 'uppercase' }}>
                    Bye Week
                  </span>
                  <span />
                  <span style={{ color: 'var(--muted)', fontVariantNumeric: 'tabular-nums' }}>
                    {b.wins}–{b.losses}
                  </span>
                  <span style={{ fontVariantNumeric: 'tabular-nums' }}>
                    <span style={{ fontFamily: 'var(--font-display)', fontWeight: 500 }}>{b.rating.toFixed(3)}</span>
                    {b.ratingRank > 0 && (
                      <span className="rank-suffix" style={{ color: 'var(--muted)', fontSize: 14, marginLeft: 4 }}>(#{b.ratingRank})</span>
                    )}
                    {b.ratingRankDelta != null && b.ratingRankDelta !== 0 && (
                      <span className="sched-delta" style={{ fontSize: 12, marginLeft: 4, color: b.ratingRankDelta > 0 ? 'var(--up)' : 'var(--down)' }}>
                        {b.ratingRankDelta > 0 ? '+' : ''}{b.ratingRankDelta}
                      </span>
                    )}
                  </span>
                  <span style={{ fontVariantNumeric: 'tabular-nums' }}>
                    <span style={{ fontFamily: 'var(--font-display)', fontWeight: 500 }}>{b.scheduleStrength.toFixed(3)}</span>
                    {b.scheduleStrengthRank > 0 && (
                      <span className="rank-suffix" style={{ color: 'var(--muted)', fontSize: 14, marginLeft: 4 }}>(#{b.scheduleStrengthRank})</span>
                    )}
                    {b.scheduleStrengthRankDelta != null && b.scheduleStrengthRankDelta !== 0 && (
                      <span className="sched-delta" style={{ fontSize: 12, marginLeft: 4, color: b.scheduleStrengthRankDelta > 0 ? 'var(--up)' : 'var(--down)' }}>
                        {b.scheduleStrengthRankDelta > 0 ? '+' : ''}{b.scheduleStrengthRankDelta}
                      </span>
                    )}
                  </span>
                </div>
              )
            }

            const g = entry.game
            const isWin = g.isWin
            const isComplete = g.teamScore !== null

            return (
              <div key={g.gameId} className="game-log-row">
                <span className="game-log-date">{formatDate(g.date)}</span>
                <span>
                  <span style={{ color: 'var(--muted)', marginRight: 4 }}>
                    {g.isNeutralSite ? 'vs' : g.isHome ? '' : '@'}
                  </span>
                  {g.opponentRank && g.opponentRank <= 25 && (
                    <span style={{ color: 'var(--accent)', marginRight: 2 }}>#{g.opponentRank}</span>
                  )}
                  <Link
                    to={`/teams/${g.opponentId}?year=${year}&name=${slugify(g.opponentName)}`}
                    style={{ color: 'var(--text)', textDecoration: 'none', fontWeight: 600 }}
                  >
                    {g.opponentName}
                  </Link>
                </span>
                <span>
                  {isComplete ? (
                    <span className={`game-log-result ${isWin ? 'w' : 'l'}`}>
                      {isWin ? 'W' : 'L'} {g.teamScore}–{g.opponentScore}
                    </span>
                  ) : g.predictedTeamScore !== null && g.predictedOpponentScore !== null ? (
                    <PredictionBadge team={g.predictedTeamScore} opp={g.predictedOpponentScore} />
                  ) : (
                    <span style={{ color: 'var(--muted)', fontSize: 14 }}>Upcoming</span>
                  )}
                </span>
                <span style={{ color: 'var(--muted)', fontVariantNumeric: 'tabular-nums' }}>
                  {g.runningWins}–{g.runningLosses}
                </span>
                <span style={{ fontVariantNumeric: 'tabular-nums' }}>
                  {g.teamRating !== null ? (
                    <>
                      <span style={{ fontFamily: 'var(--font-display)', fontWeight: 500 }}>
                        {g.teamRating.toFixed(3)}
                      </span>
                      {g.teamRank && (
                        <span className="rank-suffix" style={{ color: 'var(--muted)', fontSize: 14, marginLeft: 4 }}>
                          (#{g.teamRank})
                        </span>
                      )}
                      {g.teamRankDelta != null && g.teamRankDelta !== 0 && (
                        <span
                          className="sched-delta"
                          style={{
                            fontSize: 12,
                            marginLeft: 4,
                            color: g.teamRankDelta > 0 ? 'var(--up)' : 'var(--down)',
                          }}
                        >
                          {g.teamRankDelta > 0 ? '+' : ''}{g.teamRankDelta}
                        </span>
                      )}
                    </>
                  ) : '—'}
                </span>
                <span style={{ fontVariantNumeric: 'tabular-nums' }}>
                  {g.scheduleStrength != null ? (
                    <span style={{ fontFamily: 'var(--font-display)', fontWeight: 500 }}>
                      {g.scheduleStrength.toFixed(3)}
                      {g.scheduleStrengthRank && (
                        <span className="rank-suffix" style={{ color: 'var(--muted)', fontSize: 14, marginLeft: 4 }}>
                          (#{g.scheduleStrengthRank})
                        </span>
                      )}
                      {g.scheduleStrengthRankDelta != null && g.scheduleStrengthRankDelta !== 0 && (
                        <span
                          className="sched-delta"
                          style={{
                            fontSize: 12,
                            marginLeft: 4,
                            color: g.scheduleStrengthRankDelta > 0 ? 'var(--up)' : 'var(--down)',
                          }}
                        >
                          {g.scheduleStrengthRankDelta > 0 ? '+' : ''}{g.scheduleStrengthRankDelta}
                        </span>
                      )}
                    </span>
                  ) : '—'}
                </span>
              </div>
            )
          })}
          {team.games.length === 0 && (
            <div className="empty">No games on record for this team.</div>
          )}

        </div>
        </div>
      </section>
    </main>
  )
}
