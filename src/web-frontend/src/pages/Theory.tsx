export default function Theory() {
  return (
    <main className="page">
      <h1 className="page-heading">The Theory Behind the Ratings</h1>
      <div className="theory-content">
        <p>
          The Hensley Rating System assigns a single numeric rating to every college football team by
          solving a large system of linear equations — one equation per game played. The system is
          built around a core insight: a team's rating should equal the average of its opponents'
          ratings, offset by how convincingly it won or lost each game.
        </p>

        <h2>Level of Victory</h2>
        <p>
          Raw point differential rewards blowouts too heavily. Instead, each game contributes a
          nonlinear <em>Level of Victory</em> value clamped between 1.0 and 4.0:
        </p>
        <div className="math">
          LOV = √((1 − loserScore / winnerScore) × (winnerScore − loserScore))
        </div>
        <p>
          This formula increases quickly for close games and flattens out as the margin grows,
          reducing the incentive to run up the score.
        </p>

        <h2>Home Field Advantage</h2>
        <p>
          One variant of the system treats home field advantage as an unknown to be solved for
          rather than a fixed constant. The linear system gains an extra variable: the value of
          playing at home. For the 2024 season, this came out to approximately 1.0 rating points.
        </p>

        <h2>Solving the System</h2>
        <p>
          The system of equations is assembled into a matrix and solved via Gauss–Jordan
          elimination — the same algorithm taught in a first linear algebra course. Every team,
          conference, and division gets a rating in one pass.
        </p>
        <p>
          Teams that have not played each other — directly or through common opponents — cannot be
          compared using this method. The system partitions the full schedule into connected
          components and solves each independently.
        </p>

        <h2>Four Rating Variants</h2>
        <p>The system produces four ratings per team:</p>
        <ul style={{ paddingLeft: 20, lineHeight: 1.8, color: 'var(--muted)' }}>
          <li><strong style={{ color: 'var(--text)' }}>Standard</strong> — raw point differential as the right-hand side</li>
          <li><strong style={{ color: 'var(--text)' }}>Home Field Advantage</strong> — HFA solved as an additional unknown</li>
          <li><strong style={{ color: 'var(--text)' }}>Max Point Differential</strong> — Standard with per-game margin capped at 14 points</li>
          <li><strong style={{ color: 'var(--text)' }}>Hensley</strong> — HFA plus the nonlinear LOV scoring function (this is the headline rating)</li>
        </ul>

        <h2>Academic Background</h2>
        <p>
          This rating system originated as a graduate research project. The full mathematical
          derivation is available in the accompanying paper:{' '}
          <em>Advanced Computational Ratings for College Football Teams</em>.
        </p>
      </div>
    </main>
  )
}
