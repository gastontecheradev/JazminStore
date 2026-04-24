using Jazmin.Data;
using Jazmin.Models;
using Jazmin.Models.ViewModels;
using Jazmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = DbSeeder.AdminRole)]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IImageUploadService _images;

    public ProductsController(ApplicationDbContext db, IImageUploadService images)
    {
        _db = db;
        _images = images;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var q = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search));
        if (categoryId.HasValue && categoryId > 0)
            q = q.Where(p => p.CategoryId == categoryId);

        ViewBag.Categories = await _db.Categories.OrderBy(c => c.SortOrder).ToListAsync();
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;

        var products = await q.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new ProductEditViewModel
        {
            Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(),
            IsActive = true,
            SizesCsv = "XS,S,M,L"
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            return View(vm);
        }

        var slug = SlugHelper.Slugify(vm.Name);
        int counter = 1;
        var originalSlug = slug;
        while (await _db.Products.AnyAsync(p => p.Slug == slug))
            slug = $"{originalSlug}-{++counter}";

        var product = new Product
        {
            Name = vm.Name,
            Slug = slug,
            Description = vm.Description,
            ShortDescription = vm.ShortDescription,
            Price = vm.Price,
            ComparePrice = vm.ComparePrice,
            Stock = vm.Stock,
            IsActive = vm.IsActive,
            IsFeatured = vm.IsFeatured,
            IsNew = vm.IsNew,
            CategoryId = vm.CategoryId,
            SizesCsv = vm.SizesCsv,
            ColorsCsv = vm.ColorsCsv
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        if (vm.NewImages is { Count: > 0 })
        {
            int order = 0;
            bool first = true;
            foreach (var f in vm.NewImages)
            {
                var url = await _images.SaveAsync(f);
                if (url != null)
                {
                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        Url = url,
                        IsPrimary = first,
                        SortOrder = order++
                    });
                    first = false;
                }
            }
            await _db.SaveChangesAsync();
        }

        TempData["AdminMessage"] = "Producto creado ✓";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products
            .Include(p => p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        var vm = new ProductEditViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            Price = product.Price,
            ComparePrice = product.ComparePrice,
            Stock = product.Stock,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            IsNew = product.IsNew,
            CategoryId = product.CategoryId,
            SizesCsv = product.SizesCsv,
            ColorsCsv = product.ColorsCsv,
            ExistingImages = product.Images.ToList(),
            Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            vm.ExistingImages = await _db.ProductImages.Where(i => i.ProductId == vm.Id).ToListAsync();
            return View(vm);
        }

        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == vm.Id);
        if (product == null) return NotFound();

        if (product.Name != vm.Name)
        {
            var slug = SlugHelper.Slugify(vm.Name);
            int counter = 1;
            var originalSlug = slug;
            while (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != product.Id))
                slug = $"{originalSlug}-{++counter}";
            product.Slug = slug;
        }

        product.Name = vm.Name;
        product.Description = vm.Description;
        product.ShortDescription = vm.ShortDescription;
        product.Price = vm.Price;
        product.ComparePrice = vm.ComparePrice;
        product.Stock = vm.Stock;
        product.IsActive = vm.IsActive;
        product.IsFeatured = vm.IsFeatured;
        product.IsNew = vm.IsNew;
        product.CategoryId = vm.CategoryId;
        product.SizesCsv = vm.SizesCsv;
        product.ColorsCsv = vm.ColorsCsv;
        product.UpdatedAt = DateTime.UtcNow;

        if (vm.NewImages is { Count: > 0 })
        {
            int order = product.Images.Count;
            bool hasPrimary = product.Images.Any(i => i.IsPrimary);
            foreach (var f in vm.NewImages)
            {
                var url = await _images.SaveAsync(f);
                if (url != null)
                {
                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        Url = url,
                        IsPrimary = !hasPrimary,
                        SortOrder = order++
                    });
                    hasPrimary = true;
                }
            }
        }

        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Producto actualizado ✓";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int productId)
    {
        var img = await _db.ProductImages.FindAsync(imageId);
        if (img != null && img.ProductId == productId)
        {
            _images.Delete(img.Url);
            _db.ProductImages.Remove(img);
            await _db.SaveChangesAsync();

            // If we deleted the primary, promote another
            if (img.IsPrimary)
            {
                var next = await _db.ProductImages.Where(i => i.ProductId == productId).OrderBy(i => i.SortOrder).FirstOrDefaultAsync();
                if (next != null) { next.IsPrimary = true; await _db.SaveChangesAsync(); }
            }
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimary(int imageId, int productId)
    {
        var images = await _db.ProductImages.Where(i => i.ProductId == productId).ToListAsync();
        foreach (var i in images) i.IsPrimary = i.Id == imageId;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        // if product has been ordered, soft-disable instead of deleting
        bool hasOrders = await _db.OrderItems.AnyAsync(o => o.ProductId == id);
        if (hasOrders)
        {
            product.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["AdminMessage"] = "Producto desactivado (ya tiene ventas registradas).";
            return RedirectToAction(nameof(Index));
        }

        foreach (var img in product.Images) _images.Delete(img.Url);
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Producto eliminado ✓";
        return RedirectToAction(nameof(Index));
    }
}
