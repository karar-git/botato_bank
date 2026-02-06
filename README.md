# Backend - CoreBank API

## Railway Deployment

1. Connect this branch (`backend`) to Railway
2. Railway auto-detects the `Dockerfile`
3. Set these environment variables in Railway dashboard:

```
DatabaseProvider=PostgreSQL
ConnectionStrings__DefaultConnection=<your Railway PostgreSQL connection string>
Jwt__Key=<your-secret-key-min-32-chars>
Jwt__Issuer=CoreBank
Jwt__Audience=CoreBankClients
Cors__AllowedOrigins__0=https://your-app.vercel.app
Cors__AllowedOrigins__1=http://localhost:5173
```

4. Railway will provision a PostgreSQL database - use its `DATABASE_URL` in the connection string.

## Local Development

```bash
dotnet restore
dotnet run
```

API available at `http://localhost:5000`
Swagger at `http://localhost:5000/swagger`
