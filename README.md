# JAZMÍN — Tienda online

E-commerce de ropa femenina hecha a mano en Uruguay. ASP.NET Core 8 MVC + Identity + SQLite.

## 🏃 Arranque rápido

**Requisitos:** Visual Studio 2022/2026 o `dotnet SDK 8` + un terminal.

```bash
# 1. Restaurar paquetes
dotnet restore

# 2. Instalar herramienta EF (si no la tenés)
dotnet tool install --global dotnet-ef

# 3. Crear la base de datos SQLite
dotnet ef migrations add Init
dotnet ef database update

# 4. Correr
dotnet run
```

Luego abrí **https://localhost:7123** (o el puerto que indique la consola).

> Con Visual Studio: `F5` o el botón ▶ funciona directo. El seed crea la base, el admin y 8 productos demo la primera vez.

### Login admin por defecto

- **Email:** `admin@jazmin.uy`
- **Contraseña:** `Admin123!`

Podés (y deberías) cambiarlo en `appsettings.json` bajo `AdminSeed` **antes del primer arranque**, o cambiar la contraseña desde `/Account/ChangePassword` luego de iniciar sesión.

---

## ⚙️ Configuración

### Datos del sitio (sección `Site` en `appsettings.json`)

Editá los placeholders con tus datos reales:

```json
"Site": {
  "Name": "JAZMÍN",
  "Tagline": "Ropa hecha con amor",
  "Url": "https://jazmin.somee.com",         // URL pública del sitio
  "WhatsAppNumber": "598991234567",          // sin + ni espacios
  "WhatsAppMessage": "Hola Jazmín! ...",     // mensaje pre-llenado
  "Instagram": "jazmin.uy",
  "InstagramUrl": "https://instagram.com/jazmin.uy",
  "ContactEmail": "hola@jazmin.uy"
}
```

### Tarifas de envío (`Shipping`)

```json
"Shipping": {
  "MontevideoRate": 150,    // UYU
  "InteriorRate": 250,
  "FreeShippingFrom": 0     // 0 = desactivado; poné un monto para envío gratis a partir de X
}
```

---

## 💳 Mercado Pago (opcional)

El sistema arranca con MP **desactivado**. Para activarlo:

1. Creá una cuenta en [Mercado Pago Developers](https://www.mercadopago.com.uy/developers) y obtené tus credenciales **TEST** primero.
2. Reemplazá en `appsettings.Development.json` (o mejor, usá **user secrets**):

   ```json
   "MercadoPago": {
     "AccessToken": "TEST-xxxxxxxx...",
     "PublicKey": "TEST-xxxxxxxx...",
     "Enabled": true
   }
   ```

3. Con **user secrets** (recomendado en dev):

   ```bash
   dotnet user-secrets set "MercadoPago:AccessToken" "TEST-xxxx..."
   dotnet user-secrets set "MercadoPago:PublicKey"   "TEST-xxxx..."
   dotnet user-secrets set "MercadoPago:Enabled"     "true"
   ```

4. Para producción: cambiá a credenciales `APP_USR-*` en tu hosting.

> Si `Enabled: false`, el checkout simplemente marca el pedido como *Pendiente* y no llama a MP — perfecto para probar sin credenciales reales.

El webhook se recibe en `POST /Webhook/MercadoPago`. En Somee, asegurate de que esa URL sea pública.

---

## 📧 Email (SMTP)

Por defecto desactivado. Al activarlo, se envían:
- Email de bienvenida al registrarse
- Confirmación de pedido
- Notificación cuando cambia el estado del pedido
- Enlace para restablecer contraseña

### Gmail (recomendado)

1. Activá la **verificación en 2 pasos** en tu cuenta Google.
2. Generá una **App Password** en https://myaccount.google.com/apppasswords.
3. Usá esa clave de 16 caracteres en `appsettings.Development.json`:

   ```json
   "Email": {
     "Enabled": true,
     "From": "hola@jazmin.uy",
     "FromName": "JAZMÍN",
     "SmtpHost": "smtp.gmail.com",
     "SmtpPort": 587,
     "SmtpUser": "tu-mail@gmail.com",
     "SmtpPassword": "xxxxxxxxxxxxxxxx",
     "UseSsl": true
   }
   ```

### Outlook / Hotmail

```json
"SmtpHost": "smtp.office365.com",
"SmtpPort": 587,
"UseSsl": true
```

---

## 🚀 Deploy a Somee

1. **Cambiá la base de datos** a algo persistente. SQLite local funciona, pero en Somee lo ideal es:
   - Opción A (sin cambios): usar SQLite en el disco de Somee. Funciona pero lento en concurrencia.
   - Opción B (recomendado): migrar a **SQL Server** (Somee da gratis). Cambiá `UseSqlite` a `UseSqlServer` en `Program.cs` y el `ConnectionStrings:DefaultConnection` en `appsettings.json`.

2. **Nunca subas secretos a GitHub.** El `.gitignore` ya excluye `appsettings.Development.json`, `*.db`, `wwwroot/uploads/*`. En Somee, configurá las variables sensibles (`MercadoPago:AccessToken`, `Email:SmtpPassword`, `AdminSeed:Password`) como **environment variables** en el panel.

3. **Publish desde Visual Studio:**
   - Clic derecho en el proyecto → *Publish* → FTP.
   - Host: el que te dé Somee, `/site/wwwroot`.

4. **Primera vez en producción:**
   - El seed corre automáticamente al primer startup y crea el admin. **Cambiá la contraseña** enseguida.
   - Verificá que la carpeta `wwwroot/uploads/` tenga permisos de escritura.

5. **URLs de Mercado Pago:** una vez deployeado, la URL del sitio (`Site:Url`) se usa para las `back_urls` y el webhook. Asegurate de que sea la URL real de Somee.

---

## 📦 Estructura del proyecto

```
Jazmin/
├── Controllers/          # Home, Catalog, Product, Cart, Favorites, Checkout, Order, Account, Webhook
│   └── Admin/           # Dashboard, Products, Categories, Orders (todos [Area("Admin")])
├── Models/              # Entidades EF + ViewModels
│   └── ViewModels/
├── Data/                # ApplicationDbContext, DbSeeder
├── Services/            # CartService, MercadoPagoService, ShippingService, EmailService, ImageUploadService
├── Views/               # Todas las vistas Razor
│   ├── Shared/          # _Layout, _AdminNav, _ProductCard
│   ├── Admin/           # Área admin
│   └── …
├── wwwroot/
│   ├── css/             # site.css, admin.css
│   ├── js/              # site.js
│   ├── img/
│   │   └── products/   # SVGs demo (reemplazables subiendo imágenes desde el panel)
│   └── uploads/         # Aquí se guardan las imágenes subidas por la admin
├── appsettings.json
├── appsettings.Development.json  (gitignored — podés meter credenciales acá para dev)
└── Program.cs
```

---

## 🧠 Reglas de negocio importantes

- **Reseñas**: solo pueden dejarse por productos comprados. El sistema requiere:
  1. Que exista un pedido del usuario con ese producto.
  2. Que el pedido esté en estado **Delivered** (Entregado).
  3. Que no haya reseñado ese producto en ese pedido antes.

  El enlace se muestra automáticamente en la página del producto **y** en `Mis pedidos` → detalle.

- **Stock**: se descuenta al confirmar el pedido (no al pagar). Si el pago falla, el admin puede cancelar la orden manualmente y ajustar stock.

- **Zonas de envío**:
  - `Montevideo`, `Interior`, `Pickup` (retiro en persona, sin costo).

- **Roles**:
  - `Admin` — ve `/Admin/*`, puede CRUD productos/categorías/pedidos.
  - `Customer` — todos los clientes registrados.

---

## 🛠️ Tips

- **Limpiar la base**: borrá `Jazmin.db*` y corré `dotnet ef database update` de nuevo. El seed volverá a crear admin + 6 categorías + 8 productos demo.
- **Las imágenes demo en `wwwroot/img/products/`** son SVGs simples — reemplazalas subiendo fotos reales desde el panel admin.
- **Agregar nuevo admin**: desde el panel no se puede (por diseño). Cambiá en `AdminSeed` y borrá la DB, o ejecutá un INSERT en la tabla `AspNetUserRoles`.
- **Personalizar colores**: todo está en las variables CSS de `wwwroot/css/site.css` (`:root { ... }`). Cambiá `--pink-700` y el resto se adapta.

---

## 📞 Contacto

Si tenés dudas o ves algo raro, abrite un issue o escribime.

Hecho con amor ♥ desde Uruguay.
