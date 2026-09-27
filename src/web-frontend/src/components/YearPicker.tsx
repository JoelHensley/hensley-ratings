import { useState } from 'react'
import { Link } from 'react-router-dom'

function groupByDecade(years: number[]): Map<number, number[]> {
  const map = new Map<number, number[]>()
  for (const y of years) {
    const decade = Math.floor(y / 10) * 10
    if (!map.has(decade)) map.set(decade, [])
    map.get(decade)!.push(y)
  }
  return map
}

interface YearPickerProps {
  years: number[]
  activeYear: number
  buildPath: (year: number) => string
}

export function YearPicker({ years, activeYear, buildPath }: YearPickerProps) {
  const sorted = [...years].sort((a, b) => a - b)
  const activeDecade = Math.floor(activeYear / 10) * 10
  const [expandedDecade, setExpandedDecade] = useState(activeDecade)

  const grouped = groupByDecade(sorted)
  const decades = [...grouped.keys()].sort((a, b) => a - b)

  if (decades.length <= 1) {
    return (
      <div className="year-nav">
        {sorted.map((y) => (
          <span key={y}>
            <Link to={buildPath(y)} className={`year-link${y === activeYear ? ' active' : ''}`}>
              {y}
            </Link>
          </span>
        ))}
      </div>
    )
  }

  const expandedYears = expandedDecade >= 0 ? (grouped.get(expandedDecade) ?? []) : []

  return (
    <div className="year-nav-decades">
      <div className="decade-buttons">
        {decades.map((decade) => {
          const isExpanded = decade === expandedDecade
          const hasActive = (grouped.get(decade) ?? []).includes(activeYear)
          return (
            <button
              key={decade}
              className={`decade-toggle${isExpanded ? ' expanded' : ''}${hasActive && !isExpanded ? ' has-active' : ''}`}
              onClick={() => setExpandedDecade(isExpanded ? -1 : decade)}
              aria-expanded={isExpanded}
            >
              {decade}s {isExpanded ? '▾' : '▸'}
            </button>
          )
        })}
      </div>
      <div className={`decade-years${expandedYears.length > 0 ? ' expanded' : ''}`}>
        {expandedYears.map((y) => (
          <span key={y}>
            <Link to={buildPath(y)} className={`year-link${y === activeYear ? ' active' : ''}`}>
              {y}
            </Link>
          </span>
        ))}
      </div>
    </div>
  )
}
