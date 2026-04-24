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
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    public CategoriesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var cats = await _db.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();
        return View(cats);
    }

    [HttpGet]
    public IActionResult Create() => View(new CategoryEditViewModel { IsActive = true });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var slug = SlugHelper.Slugify(vm.Name);
        int counter = 1;
        var originalSlug = slug;
        while (await _db.Categories.AnyAsync(c => c.Slug == slug))
            slug = $"{originalSlug}-{++counter}";

        _db.Categories.Add(new Category
        {
            Name = vm.Name,
            Slug = slug,
            Description = vm.Description,
            SortOrder = vm.SortOrder,
            IsActive = vm.IsActive
        });
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Categoría creada ✓";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        return View(new CategoryEditViewModel
        {
            Id = cat.Id,
            Name = cat.Name,
            Description = cat.Description,
            SortOrder = cat.SortOrder,
            IsActive = cat.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var cat = await _db.Categories.FindAsync(vm.Id);
        if (cat == null) return NotFound();

        if (cat.Name != vm.Name)
        {
            var slug = SlugHelper.Slugify(vm.Name);
            int counter = 1;
            var originalSlug = slug;
            while (await _db.Categories.AnyAsync(c => c.Slug == slug && c.Id != cat.Id))
                slug = $"{originalSlug}-{++counter}";
            cat.Slug = slug;
        }

        cat.Name = vm.Name;
        cat.Description = vm.Description;
        cat.SortOrder = vm.SortOrder;
        cat.IsActive = vm.IsActive;
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Categoría actualizada ✓";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return NotFound();
        if (cat.Products.Any())
        {
            cat.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["AdminMessage"] = "No se puede borrar: tiene productos. La desactivamos en su lugar.";
            return RedirectToAction(nameof(Index));
        }
        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Categoría eliminada ✓";
        return RedirectToAction(nameof(Index));
    }
}
