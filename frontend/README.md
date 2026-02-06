# Frontend - PotatoBank UI

## Vercel Deployment

1. Connect the `frontend` branch to Vercel
2. Vercel auto-detects Vite/Svelte
3. Set this environment variable in Vercel dashboard:

```
VITE_API_URL=https://your-railway-backend.up.railway.app/api
```

**Important:** `VITE_` prefix is required -- Vite only exposes env vars with this prefix to client code.

## Local Development

```bash
npm install
npm run dev
```

Runs at `http://localhost:5173`, proxies `/api` requests to `http://localhost:5000`.
