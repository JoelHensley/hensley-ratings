import { slugify } from './slugify'

interface DivOpt { id: number; name: string }
interface ConfOpt { id: number; name: string }

/**
 * Builds a ratings URL matching the old site's pattern:
 *   /ratings/2025/week-15/fbs/1/acc/2
 *   /ratings/2025/week-15/fbs/1
 *   /ratings/2025/week-15/all
 *   /ratings/2025/fbs/1   (no week = latest week for that year)
 */
export function buildRatingsPath(
  year: number,
  week: number | undefined,
  div: DivOpt | undefined,
  conf?: ConfOpt,
): string {
  const weekSeg = week ? `/week-${week}` : ''
  if (!div) {
    return week ? `/ratings/${year}/week-${week}/all` : `/ratings/${year}`
  }
  const divSeg = `${slugify(div.name)}/${div.id}`
  if (!conf) return `/ratings/${year}${weekSeg}/${divSeg}`
  const confSeg = `${slugify(conf.name)}/${conf.id}`
  return `/ratings/${year}${weekSeg}/${divSeg}/${confSeg}`
}

/**
 * Builds a team URL nested under ratings, matching the old site:
 *   /ratings/2025/fbs/1/acc/2/georgia/8
 */
export function buildTeamPath(
  year: number,
  teamId: number,
  teamName: string,
  div: DivOpt,
  conf: ConfOpt,
): string {
  const divSeg = `${slugify(div.name)}/${div.id}`
  const confSeg = `${slugify(conf.name)}/${conf.id}`
  const teamSeg = `${slugify(teamName)}/${teamId}`
  return `/ratings/${year}/${divSeg}/${confSeg}/${teamSeg}`
}

/**
 * Builds a schedule URL matching the old site's pattern:
 *   /schedule/2025/week-15/fbs/1
 *   /schedule/2025/fbs/1   (no week = latest week)
 */
export function buildSchedulePath(
  year: number,
  week: number | undefined,
  div: DivOpt | undefined,
): string {
  if (!div) return week ? `/schedule/${year}/week-${week}` : `/schedule/${year}`
  const weekSeg = week ? `/week-${week}` : ''
  const divSeg = `${slugify(div.name)}/${div.id}`
  return `/schedule/${year}${weekSeg}/${divSeg}`
}
