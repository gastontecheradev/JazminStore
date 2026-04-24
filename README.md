# JAZMÍN — E-commerce. 

ASP.NET Core 8 MVC + Identity + SQLite.

---

## 📦 Project Structure

```
Jazmin/
├── Controllers/          # Home, Catalog, Product, Cart, Favorites, Checkout, Order, Account, Webhook
│   └── Admin/           # Dashboard, Products, Categories, Orders (todos [Area("Admin")])
├── Models/              # EF + ViewModels
│   └── ViewModels/
├── Data/                # ApplicationDbContext, DbSeeder
├── Services/            # CartService, MercadoPagoService, ShippingService, EmailService, ImageUploadService
├── Views/               # Razor Views
│   ├── Shared/          # _Layout, _AdminNav, _ProductCard
│   ├── Admin/           # Admin area
│   └── …
├── wwwroot/
│   ├── css/             # site.css, admin.css
│   ├── js/              # site.js
│   ├── img/
│   │   └── products/   # SVGs
│   └── uploads/         # Images
├── appsettings.json
├── appsettings.Development.json  (gitignored — podés meter credenciales acá para dev)
└── Program.cs
```
