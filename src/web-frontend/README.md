# Web Frontend

React + TypeScript + Tailwind UI for browsing Hensley Ratings by year, week, conference, and division.

## Prerequisites

- Node.js 20+
- The backend API running at `http://localhost:5000` (see [`src/web-backend`](../web-backend/README.md))

## Running

```bash
cd src/web-frontend
npm install
npm run dev
```

The app starts at **http://localhost:5173**.

All `/api` requests are proxied to `http://localhost:5000` via Vite's dev proxy — no CORS config needed in development.

## Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start Vite dev server with HMR |
| `npm run build` | Type-check and build for production (`dist/`) |
| `npm run preview` | Serve the production build locally at port 4173 |
| `npm run lint` | Run oxlint |

## Stack

- React 19 + React Router 7
- TanStack Query 5
- Tailwind CSS 4 (via Vite plugin)
- TypeScript 6
