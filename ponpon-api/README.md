# PonPon Backend

.NET 10 modular monolith backend.

Current implemented scope:

- `PonPon.Shared`
- `PonPon.Modules.Identity`
- `PonPon.Api` integration for Identity

The other modules remain skeleton-only.

## Supabase PostgreSQL

Supabase is used only as PostgreSQL. Do not use Supabase Auth.

Set the connection string with configuration, user secrets, or environment variables. The application reads:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

For local EF design-time commands, you can also set:

```powershell
$env:PONPON_CONNECTION_STRING="Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

## JWT and LINE

Configure these values through `appsettings.Development.json`, user secrets, or environment variables:

```json
{
  "Jwt": {
    "Secret": "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_FOR_DEV_ONLY",
    "Issuer": "PonPon.Api",
    "Audience": "PonPon.Liff",
    "AccessTokenMinutes": 60,
    "RefreshTokenDays": 30
  },
  "Line": {
    "ChannelId": "YOUR_LINE_CHANNEL_ID",
    "ChannelSecret": "YOUR_LINE_CHANNEL_SECRET"
  }
}
```

## Identity Migrations

Create an Identity migration from the solution root:

```powershell
dotnet ef migrations add InitialIdentity --project src/PonPon.Modules.Identity --startup-project src/PonPon.Api --context IdentityDbContext --output-dir Infrastructure/Persistence/Migrations
```

Apply migrations:

```powershell
dotnet ef database update --project src/PonPon.Modules.Identity --startup-project src/PonPon.Api --context IdentityDbContext
```

## ZORT Product Sync

ZORT is the source of truth for product and stock. PonPon stores a product snapshot/cache in PostgreSQL for the LIFF ecommerce API.

Configure ZORT through user secrets or environment variables. Source-controlled appsettings files contain placeholders only:

```json
{
  "Zort": {
    "BaseUrl": "https://open-api.zortout.com/v4",
    "StoreName": "",
    "ApiKey": "",
    "ApiSecret": "",
    "DefaultPageLimit": 100,
    "WarehouseCode": ""
  }
}
```

Create a Catalog migration from the solution root:

```powershell
dotnet ef migrations add InitialCatalog --project src/PonPon.Modules.Catalog --startup-project src/PonPon.Api --context CatalogDbContext --output-dir Infrastructure/Persistence/Migrations
```

Apply Catalog migrations:

```powershell
dotnet ef database update --project src/PonPon.Modules.Catalog --startup-project src/PonPon.Api --context CatalogDbContext
```

Product sync creates new LIFF-tagged ZORT products, updates existing products only when the
incoming ZORT snapshot differs from the local snapshot, and reports skipped rows as `unchanged`.
SKUs in the form `BASE-VARIANT` are grouped under one product by `BASE`; each full SKU is stored
as a product variant.

## ZORT Orders

PonPon synchronizes LINE LIFF orders from ZORT into PostgreSQL. Apply the Ordering migration:

```powershell
dotnet ef database update --project src/PonPon.Modules.Ordering --startup-project src/PonPon.Api --context OrderingDbContext
```

Sync orders:

```http
POST /api/admin/orders/sync-zort
Authorization: Bearer <admin_access_token>
Content-Type: application/json

{
  "pageStart": 1,
  "pageLimit": 100,
  "maxPages": 1
}
```

The sync calls `Order/GetOrders` with `saleschannel=LineLiff`, then creates or updates orders,
items, and payments in the `ordering` schema. The complete source payload is retained as `jsonb`.

Read synchronized orders from PostgreSQL:

```http
GET /api/admin/orders
GET /api/admin/orders/{id}
```

List filters: `keyword`, `status`, `paymentStatus`, `page`, and `pageSize`.

Customer order page APIs require a customer access token:

```http
POST /api/orders
GET /api/orders
GET /api/orders/{id}
Authorization: Bearer <customer_access_token>
```

`POST /api/orders` validates products and stock against the Catalog database, calculates item
prices on the server, creates the order in ZORT, and saves the returned order snapshot locally.
Use a stable `clientRequestId` for retries; it is sent to ZORT as `uniquenumber`.

```json
{
  "clientRequestId": "11111111-2222-3333-4444-555555555555",
  "customerName": "Customer",
  "customerEmail": "customer@example.com",
  "customerPhone": "0812345678",
  "customerAddress": "Billing address",
  "shippingName": "Customer",
  "shippingPhone": "0812345678",
  "shippingAddress": "Shipping address",
  "shippingChannel": "Delivery",
  "shippingAmount": 0,
  "description": null,
  "items": [
    {
      "productId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      "variantId": "bbbbbbbb-cccc-dddd-eeee-ffffffffffff",
      "quantity": 1
    }
  ]
}
```

Customers only receive orders linked to their internal PonPon customer ID. List filters:
`status`, `paymentStatus`, `page`, and `pageSize`.
