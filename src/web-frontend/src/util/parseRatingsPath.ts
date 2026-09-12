/**
 * Parses the splat portion of /ratings/* or /schedule/* into structured params.
 *
 * Ratings patterns (old-site URL shape):
 *   2025/week-15/all                    → year + week, all divisions
 *   2025/week-15/fbs/1                  → year + week + div
 *   2025/week-15/fbs/1/acc/2            → year + week + div + conf
 *   2025/fbs/1                          → year + div (no week → latest week)
 *   2025/fbs/1/acc/2                    → year + div + conf (no week)
 *   2025/fbs/1/acc/2/ohio-state/27      → team page (7 segments starting from year)
 */
export interface ParsedRatingsPath {
  year?: number
  week?: number
  divId?: number
  confId?: number
  teamId?: number
  isAll: boolean   // explicit "all divisions" segment
}

export function parseRatingsPath(splat: string | undefined): ParsedRatingsPath {
  const segs = (splat ?? '').split('/').filter(Boolean)
  if (!segs.length) return { isAll: false }

  const year = Number(segs[0])
  let i = 1

  let week: number | undefined
  if (segs[i]?.startsWith('week-')) {
    week = Number(segs[i].slice(5))
    i++
  }

  if (segs[i] === 'all') {
    return { year, week, isAll: true }
  }

  let divId: number | undefined
  if (segs[i] && segs[i + 1]) {
    // segs[i] = divSlug, segs[i+1] = divId
    divId = Number(segs[i + 1])
    i += 2
  }

  let confId: number | undefined
  if (segs[i] && segs[i + 1]) {
    // segs[i] = confSlug, segs[i+1] = confId
    confId = Number(segs[i + 1])
    i += 2
  }

  let teamId: number | undefined
  if (segs[i] && segs[i + 1]) {
    // segs[i] = teamSlug, segs[i+1] = teamId
    teamId = Number(segs[i + 1])
  }

  return { year, week, divId, confId, teamId, isAll: false }
}
