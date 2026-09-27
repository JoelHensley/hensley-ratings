export interface RatedTeam {
  teamId: number
  name: string
  conferenceName: string
  conferenceId: number
  divisionName: string
  divisionId: number
  wins: number
  losses: number
  hensleyRating: number
  scheduleStrength: number
  rankOverall: number
  rankDivision: number
  rankConference: number
  weekOverWeekChange: number | null
  pointsScored: number | null
  pointsAllowed: number | null
}

export interface ScheduleGame {
  gameId: number
  date: string
  homeTeamId: number
  homeTeamName: string
  homeTeamConference: string
  homeScore: number | null
  awayTeamId: number
  awayTeamName: string
  awayTeamConference: string
  awayScore: number | null
  isNeutralSite: boolean
  isComplete: boolean
  predictedHomeScore: number | null
  predictedAwayScore: number | null
  homeTeamRating: number | null
  awayTeamRating: number | null
  homeTeamRank: number | null
  awayTeamRank: number | null
  homeTeamPtsScored: number | null
  awayTeamPtsScored: number | null
}

export interface TeamGame {
  gameId: number
  date: string
  isHome: boolean
  isNeutralSite: boolean
  opponentId: number
  opponentName: string
  opponentRank: number | null
  teamScore: number | null
  opponentScore: number | null
  isWin: boolean | null
  runningWins: number
  runningLosses: number
  teamRating: number | null
  teamRank: number | null
  teamRankDelta: number | null
  scheduleStrength: number | null
  scheduleStrengthRank: number | null
  scheduleStrengthRankDelta: number | null
  weekNumber: number | null
}

export interface TeamDetail {
  teamId: number
  name: string
  conferenceName: string
  conferenceId: number
  divisionName: string
  divisionId: number
  wins: number
  losses: number
  hensleyRating: number
  rankOverall: number
  scheduleStrength: number
  pointsScored: number | null
  pointsAllowed: number | null
  games: TeamGame[]
  byeWeeks: ByeWeek[]
}

export interface ByeWeek {
  week: number
  wins: number
  losses: number
  rating: number
  ratingRank: number
  ratingRankDelta: number | null
  scheduleStrength: number
  scheduleStrengthRank: number
  scheduleStrengthRankDelta: number | null
}

export interface WeekOption {
  week: number
  cutoffDate: string
  hasPrevious: boolean
}

export interface Division {
  divisionId: number
  name: string
}

export interface Conference {
  conferenceId: number
  name: string
  divisionId: number
}

export interface MetaYears {
  years: number[]
}

export interface MetaWeeks {
  weeks: WeekOption[]
}
