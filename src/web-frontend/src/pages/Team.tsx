import { useQuery } from '@tanstack/react-query'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import { buildRatingsPath } from '../util/paths'
import { slugify } from '../util/slugify'

const DIV_ABBREV: Record<string, string> = {
  'Division-II': 'D-II',
  'Division-III': 'D-III',
}
const shortDivName = (name: string) => DIV_ABBREV[name] ?? name

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
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

  return (
    <main className="page">
      <div style={{ marginBottom: 8, fontSize: 13 }}>
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

      <div className="team-header">
        <div>
          <h1 className="team-name">{team.name}</h1>
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
        <div
          className="game-log-row"
          style={{ fontFamily: 'var(--font-display)', fontSize: 11, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--muted)', borderBottom: '2px solid var(--border)' }}
        >
          <span>Date</span>
          <span>Opponent</span>
          <span>Result</span>
          <span>Record</span>
          <span>Rating</span>
        </div>
        {team.games.map((g) => {
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
                ) : (
                  <span style={{ color: 'var(--muted)', fontSize: 12 }}>Upcoming</span>
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
                      <span style={{ color: 'var(--muted)', fontSize: 12, marginLeft: 4 }}>
                        (#{g.teamRank})
                      </span>
                    )}
                  </>
                ) : '—'}
              </span>
            </div>
          )
        })}
        {team.games.length === 0 && (
          <div className="empty">No games on record for this team.</div>
        )}
      </section>
    </main>
  )
}
