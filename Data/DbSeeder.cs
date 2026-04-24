using Jazmin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Data;

public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string CustomerRole = "Customer";

    public static async Task SeedAsync(IServiceProvider sp, IConfiguration config)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        // Roles
        foreach (var role in new[] { AdminRole, CustomerRole })
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));

        // Admin
        var adminEmail = config["AdminSeed:Email"] ?? "admin@jazmin.uy";
        var adminPass = config["AdminSeed:Password"] ?? "Admin123!";
        var adminName = config["AdminSeed:FullName"] ?? "Jazmín";
        var admin = await userMgr.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = adminName
            };
            var res = await userMgr.CreateAsync(admin, adminPass);
            if (res.Succeeded)
                await userMgr.AddToRoleAsync(admin, AdminRole);
        }
        else if (!await userMgr.IsInRoleAsync(admin, AdminRole))
        {
            await userMgr.AddToRoleAsync(admin, AdminRole);
        }

        // Categories
        if (!await db.Categories.AnyAsync())
        {
            var cats = new[]
            {
                new Category { Name = "Vestidos", Slug = "vestidos", Description = "Vestidos únicos, elegantes y cómodos", SortOrder = 1 },
                new Category { Name = "Blusas", Slug = "blusas", Description = "Blusas para cada ocasión", SortOrder = 2 },
                new Category { Name = "Faldas", Slug = "faldas", Description = "Faldas con caída y personalidad", SortOrder = 3 },
                new Category { Name = "Pantalones", Slug = "pantalones", Description = "Pantalones versátiles", SortOrder = 4 },
                new Category { Name = "Abrigos", Slug = "abrigos", Description = "Abrigos para los días fríos", SortOrder = 5 },
                new Category { Name = "Accesorios", Slug = "accesorios", Description = "El detalle perfecto", SortOrder = 6 }
            };
            db.Categories.AddRange(cats);
            await db.SaveChangesAsync();
        }

        // Demo products
        if (!await db.Products.AnyAsync())
        {
            var vestidos = await db.Categories.FirstAsync(c => c.Slug == "vestidos");
            var blusas = await db.Categories.FirstAsync(c => c.Slug == "blusas");
            var faldas = await db.Categories.FirstAsync(c => c.Slug == "faldas");
            var pantalones = await db.Categories.FirstAsync(c => c.Slug == "pantalones");
            var abrigos = await db.Categories.FirstAsync(c => c.Slug == "abrigos");
            var accesorios = await db.Categories.FirstAsync(c => c.Slug == "accesorios");

            var demo = new List<Product>
            {
                new() { Name = "Vestido Primavera", Slug = "vestido-primavera", ShortDescription = "Vestido midi en tonos pastel con caída suave",
                    Description = "Vestido midi confeccionado a mano en tela fresca con caída. Ideal para eventos de día, perfecto para primavera-verano. Corte en A que favorece todas las siluetas.",
                    Price = 2490, ComparePrice = 2990, Stock = 8, IsActive = true, IsFeatured = true, IsNew = true,
                    CategoryId = vestidos.Id, SizesCsv = "XS,S,M,L",
                    Images = new() { new ProductImage { Url = "/img/products/vestido-1.svg", IsPrimary = true } } },

                new() { Name = "Blusa Bordada", Slug = "blusa-bordada", ShortDescription = "Blusa de algodón con bordado artesanal",
                    Description = "Blusa en algodón 100% con bordado hecho a mano en el escote. Piezas únicas, cada una con pequeñas variaciones que la hacen especial.",
                    Price = 1690, Stock = 12, IsActive = true, IsFeatured = true,
                    CategoryId = blusas.Id, SizesCsv = "S,M,L",
                    Images = new() { new ProductImage { Url = "/img/products/blusa-1.svg", IsPrimary = true } } },

                new() { Name = "Falda Plisada", Slug = "falda-plisada", ShortDescription = "Falda plisada midi, elegante y versátil",
                    Description = "Falda plisada midi con cintura alta y caída fluida. Combinable con blusas o buzos, para un look casual chic.",
                    Price = 1890, Stock = 6, IsActive = true, IsNew = true,
                    CategoryId = faldas.Id, SizesCsv = "XS,S,M,L",
                    Images = new() { new ProductImage { Url = "/img/products/falda-1.svg", IsPrimary = true } } },

                new() { Name = "Pantalón Palazzo", Slug = "pantalon-palazzo", ShortDescription = "Pantalón wide-leg de caída",
                    Description = "Pantalón palazzo de caída, cintura alta con cinto. Tela fresca y cómoda para usar todo el día.",
                    Price = 2190, Stock = 10, IsActive = true, IsFeatured = true,
                    CategoryId = pantalones.Id, SizesCsv = "S,M,L,XL",
                    Images = new() { new ProductImage { Url = "/img/products/pantalon-1.svg", IsPrimary = true } } },

                new() { Name = "Tapado Beige", Slug = "tapado-beige", ShortDescription = "Tapado largo en paño suave",
                    Description = "Tapado largo en paño 100% con forrado interior. Cierre con botones y bolsillos laterales. Prenda atemporal.",
                    Price = 4490, ComparePrice = 4990, Stock = 4, IsActive = true, IsNew = true,
                    CategoryId = abrigos.Id, SizesCsv = "S,M,L",
                    Images = new() { new ProductImage { Url = "/img/products/tapado-1.svg", IsPrimary = true } } },

                new() { Name = "Vestido Lino", Slug = "vestido-lino", ShortDescription = "Vestido de lino natural",
                    Description = "Vestido en lino 100% natural, fresco e ideal para el verano uruguayo. Sin forro, respeta la caída natural del lino.",
                    Price = 2790, Stock = 7, IsActive = true, IsFeatured = true,
                    CategoryId = vestidos.Id, SizesCsv = "XS,S,M,L",
                    Images = new() { new ProductImage { Url = "/img/products/vestido-2.svg", IsPrimary = true } } },

                new() { Name = "Blusa Seda", Slug = "blusa-seda", ShortDescription = "Blusa en seda con lazo",
                    Description = "Blusa en seda artificial con detalle de lazo en el cuello. Elegante y femenina.",
                    Price = 1990, Stock = 9, IsActive = true,
                    CategoryId = blusas.Id, SizesCsv = "S,M,L",
                    Images = new() { new ProductImage { Url = "/img/products/blusa-2.svg", IsPrimary = true } } },

                new() { Name = "Pañuelo Estampado", Slug = "panuelo-estampado", ShortDescription = "Pañuelo 60×60 en gasa",
                    Description = "Pañuelo cuadrado en gasa suave con estampado floral. Accesorio versátil.",
                    Price = 690, Stock = 20, IsActive = true, IsNew = true,
                    CategoryId = accesorios.Id, SizesCsv = "Único",
                    Images = new() { new ProductImage { Url = "/img/products/panuelo-1.svg", IsPrimary = true } } }
            };

            db.Products.AddRange(demo);
            await db.SaveChangesAsync();
        }
    }
}
