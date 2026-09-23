# Stok Sayım API

ASP.NET Core backend for multi-branch stock counting and sales tracking. Mobile client: [stok-sayim-flutter](https://github.com/furkanmar/stok-sayim-flutter).

## What it does

- Auth with JWT + BCrypt, user and branch management
- Products, categories and barcodes
- Stock counts per branch, sales and returns
- Reports
- Bulk product/price import from a SQLite catalog file, with sync history

## Stack

ASP.NET Core (.NET 10) · EF Core · PostgreSQL · Docker

## Running

```bash
docker compose up -d --build
```

Configuration comes from environment variables — set at least
`JwtSettings__Secret` and `ConnectionStrings__DefaultConnection`.
Values in `appsettings.json` are placeholders only.
