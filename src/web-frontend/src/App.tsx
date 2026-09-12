import { NavLink, Navigate, Route, Routes } from 'react-router-dom'
import Ratings from './pages/Ratings'
import Schedule from './pages/Schedule'
import Team from './pages/Team'
import Theory from './pages/Theory'

export default function App() {
  return (
    <>
      <nav className="nav">
        <div className="nav-inner">
          <NavLink to="/" className="nav-brand">
            Hensley<span>Ratings</span>
          </NavLink>
          <div className="nav-links">
            <NavLink to="/ratings" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Ratings
            </NavLink>
            <NavLink to="/schedule" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Schedule
            </NavLink>
            <NavLink to="/theory" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Theory
            </NavLink>
          </div>
        </div>
      </nav>
      <Routes>
        <Route path="/" element={<Navigate to="/ratings" replace />} />

        {/* Splat routes — all path-param parsing happens inside the component */}
        <Route path="/ratings" element={<Ratings />} />
        <Route path="/ratings/*" element={<Ratings />} />
        <Route path="/schedule" element={<Schedule />} />
        <Route path="/schedule/*" element={<Schedule />} />

        {/* Legacy route kept for any existing bookmarks / internal links */}
        <Route path="/teams/:id" element={<Team />} />

        <Route path="/theory" element={<Theory />} />
      </Routes>
    </>
  )
}
