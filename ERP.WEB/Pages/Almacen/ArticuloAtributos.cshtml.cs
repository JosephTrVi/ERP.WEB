using ERP.WEB.Data;
using ERP.WEB.Models.Inventario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ERP.WEB.Pages.Almacen
{
    [Authorize]
    public class ArticulosAtributosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ArticulosAtributosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int ProductoID { get; set; }

        // FILTROS PARA EL SELECTOR DE PRODUCTOS
        [BindProperty(SupportsGet = true)]
        public int? CategoriaID { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SubcategoriaID { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        public Producto? ProductoSeleccionado { get; set; }
        public List<ProductoAtributo> ListaAtributosAsignados { get; set; } = new();

        [BindProperty]
        public ProductoAtributo NuevoAtributo { get; set; } = new();

        public SelectList SelectProductos { get; set; } = null!;
        public SelectList SelectAtributos { get; set; } = null!;
        public SelectList SelectCategorias { get; set; } = null!;
        public SelectList SelectSubcategorias { get; set; } = null!;

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            await CargarCombosAsync();

            if (ProductoID > 0)
            {
                ProductoSeleccionado = await _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.SubCategoria)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProductoID == ProductoID);

                if (ProductoSeleccionado != null)
                {
                    ListaAtributosAsignados = await _context.ProductoAtributos
                        .Include(pa => pa.Atributo)
                        .Where(pa => pa.ProductoID == ProductoID)
                        .AsNoTracking()
                        .ToListAsync();
                }
            }
        }

        public async Task<IActionResult> OnPostAgregarAtributoAsync()
        {
            if (NuevoAtributo.ProductoID <= 0 || NuevoAtributo.AtributoID <= 0 || string.IsNullOrWhiteSpace(NuevoAtributo.ValorAtributo))
            {
                MensajeError = "Debe seleccionar un atributo e ingresar un valor válido.";
                return RedirectToPage(new { ProductoID = NuevoAtributo.ProductoID, CategoriaID, SubcategoriaID, Busqueda });
            }

            try
            {
                bool existe = await _context.ProductoAtributos
                    .AnyAsync(pa => pa.ProductoID == NuevoAtributo.ProductoID && pa.AtributoID == NuevoAtributo.AtributoID);

                if (existe)
                {
                    var existenteBD = await _context.ProductoAtributos
                        .FirstOrDefaultAsync(pa => pa.ProductoID == NuevoAtributo.ProductoID && pa.AtributoID == NuevoAtributo.AtributoID);

                    if (existenteBD != null)
                    {
                        existenteBD.ValorAtributo = NuevoAtributo.ValorAtributo.Trim();
                        _context.ProductoAtributos.Update(existenteBD);
                        MensajeExito = "Especificación actualizada correctamente.";
                    }
                }
                else
                {
                    _context.ProductoAtributos.Add(NuevoAtributo);
                    MensajeExito = "Especificación técnica asignada con éxito.";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                MensajeError = $"Error al guardar el atributo: {ex.Message}";
            }

            return RedirectToPage(new { ProductoID = NuevoAtributo.ProductoID, CategoriaID, SubcategoriaID, Busqueda });
        }

        public async Task<IActionResult> OnPostEliminarAtributoAsync(int id, int productoId)
        {
            var item = await _context.ProductoAtributos.FindAsync(id);
            if (item != null)
            {
                _context.ProductoAtributos.Remove(item);
                await _context.SaveChangesAsync();
                MensajeExito = "Especificación eliminada del artículo.";
            }

            return RedirectToPage(new { ProductoID = productoId, CategoriaID, SubcategoriaID, Busqueda });
        }

        private async Task CargarCombosAsync()
        {
            // 1. Combo Categorías
            var categorias = await _context.Categorias
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .Select(c => new { c.CategoriaID, Nombre = $"[{c.Codigo}] {c.Nombre}" })
                .ToListAsync();
            SelectCategorias = new SelectList(categorias, "CategoriaID", "Nombre", CategoriaID);

            // 2. Combo Subcategorías
            var querySubcat = _context.SubCategorias.Where(s => s.Estado).AsQueryable();
            if (CategoriaID.HasValue && CategoriaID.Value > 0)
            {
                querySubcat = querySubcat.Where(s => s.CategoriaID == CategoriaID.Value);
            }
            var subcategorias = await querySubcat
                .OrderBy(s => s.Nombre)
                .Select(s => new { s.SubCategoriaID, Nombre = $"[{s.Codigo}] {s.Nombre}" })
                .ToListAsync();
            SelectSubcategorias = new SelectList(subcategorias, "SubCategoriaID", "Nombre", SubcategoriaID);

            // 3. Filtrar Productos según Categoría, Subcategoría y Texto de Búsqueda
            var queryProductos = _context.Productos.Where(p => p.Estado).AsQueryable();

            if (CategoriaID.HasValue && CategoriaID.Value > 0)
            {
                queryProductos = queryProductos.Where(p => p.CategoriaID == CategoriaID.Value);
            }

            if (SubcategoriaID.HasValue && SubcategoriaID.Value > 0)
            {
                queryProductos = queryProductos.Where(p => p.SubcategoriaID == SubcategoriaID.Value);
            }

            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string term = Busqueda.Trim().ToLower();
                queryProductos = queryProductos.Where(p => p.Nombre.ToLower().Contains(term) || p.SKU.ToLower().Contains(term));
            }

            var productos = await queryProductos
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.ProductoID, NombreCompleto = $"[{p.SKU}] {p.Nombre}" })
                .ToListAsync();

            SelectProductos = new SelectList(productos, "ProductoID", "NombreCompleto", ProductoID);

            // 4. Combo Atributos Maestros
            var atributos = await _context.Atributos
                .Where(a => a.Estado)
                .OrderBy(a => a.NombreAtributo)
                .Select(a => new { a.AtributoID, NombreConUM = string.IsNullOrEmpty(a.UnidadMedida) ? a.NombreAtributo : $"{a.NombreAtributo} ({a.UnidadMedida})" })
                .ToListAsync();

            SelectAtributos = new SelectList(atributos, "AtributoID", "NombreConUM");
        }
    }
}