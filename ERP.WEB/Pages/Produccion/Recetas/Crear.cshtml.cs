using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Produccion;

namespace ERP.WEB.Pages.Produccion.Recetas
{
    [Authorize]
    [IgnoreAntiforgeryToken] // Permite llamadas AJAX JSON directas
    public class CrearModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CrearModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CategoriaDTO> CategoriasPT { get; set; } = new();
        public List<SubCategoriaDTO> SubCategoriasPT { get; set; } = new();
        public List<CategoriaDTO> CategoriasInsumo { get; set; } = new();
        public List<SubCategoriaDTO> SubCategoriasInsumo { get; set; } = new();

        public async Task OnGetAsync()
        {
            await CargarCatalogosFiltroAsync();
        }

        private async Task CargarCatalogosFiltroAsync()
        {
            CategoriasPT = await _context.Categorias
                .Where(c => c.Estado && c.EsReceta)
                .Select(c => new CategoriaDTO
                {
                    CategoriaID = c.CategoriaID,
                    Codigo2Dig = !string.IsNullOrEmpty(c.Codigo) ? (c.Codigo.Length >= 2 ? c.Codigo.Substring(0, 2) : c.Codigo) : c.CategoriaID.ToString("D2"),
                    Nombre = c.Nombre
                })
                .AsNoTracking()
                .ToListAsync();

            SubCategoriasPT = await _context.SubCategorias
                .Where(s => s.Estado && s.EsReceta)
                .Select(s => new SubCategoriaDTO
                {
                    SubCategoriaID = s.SubCategoriaID,
                    CategoriaID = s.CategoriaID,
                    Codigo4Dig = !string.IsNullOrEmpty(s.Codigo) ? (s.Codigo.Length >= 4 ? s.Codigo.Substring(0, 4) : s.Codigo) : s.SubCategoriaID.ToString("D4"),
                    Nombre = s.Nombre
                })
                .AsNoTracking()
                .ToListAsync();

            CategoriasInsumo = await _context.Categorias
                .Where(c => c.Estado && c.EsComponente)
                .Select(c => new CategoriaDTO
                {
                    CategoriaID = c.CategoriaID,
                    Codigo2Dig = !string.IsNullOrEmpty(c.Codigo) ? (c.Codigo.Length >= 2 ? c.Codigo.Substring(0, 2) : c.Codigo) : c.CategoriaID.ToString("D2"),
                    Nombre = c.Nombre
                })
                .AsNoTracking()
                .ToListAsync();

            SubCategoriasInsumo = await _context.SubCategorias
                .Where(s => s.Estado && s.EsComponente)
                .Select(s => new SubCategoriaDTO
                {
                    SubCategoriaID = s.SubCategoriaID,
                    CategoriaID = s.CategoriaID,
                    Codigo4Dig = !string.IsNullOrEmpty(s.Codigo) ? (s.Codigo.Length >= 4 ? s.Codigo.Substring(0, 4) : s.Codigo) : s.SubCategoriaID.ToString("D4"),
                    Nombre = s.Nombre
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<JsonResult> OnGetBuscarProductoPTAsync(string q, int? catId, int? subCatId)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Estado && p.Categoria != null && p.Categoria.EsReceta)
                .AsQueryable();

            if (catId.HasValue && catId > 0)
                query = query.Where(p => p.CategoriaID == catId.Value);

            if (subCatId.HasValue && subCatId > 0)
                query = query.Where(p => p.SubcategoriaID == subCatId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                string term = q.Trim().ToLower();
                query = query.Where(p => p.SKU.ToLower().Contains(term) || p.Nombre.ToLower().Contains(term));
            }

            var resultados = await query
                .Take(20)
                .Select(p => new
                {
                    id = p.ProductoID,
                    sku = p.SKU,
                    nombre = p.Nombre,
                    displayText = $"[{p.SKU}] {p.Nombre}"
                })
                .AsNoTracking()
                .ToListAsync();

            return new JsonResult(resultados);
        }

        public async Task<JsonResult> OnGetBuscarInsumoAsync(string q, int? catId, int? subCatId)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.SubCategoria)
                .Where(p => p.Estado && p.Categoria != null && p.Categoria.EsComponente)
                .AsQueryable();

            if (catId.HasValue && catId > 0)
                query = query.Where(p => p.CategoriaID == catId.Value);

            if (subCatId.HasValue && subCatId > 0)
                query = query.Where(p => p.SubcategoriaID == subCatId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                string term = q.Trim().ToLower();
                query = query.Where(p => p.SKU.ToLower().Contains(term) || p.Nombre.ToLower().Contains(term));
            }

            var resultados = await query
                .Take(20)
                .Select(p => new
                {
                    id = p.ProductoID,
                    sku = p.SKU,
                    nombre = p.Nombre,
                    categoria = p.Categoria != null ? p.Categoria.Nombre : "Sin Cát.",
                    subcategoria = p.SubCategoria != null ? p.SubCategoria.Nombre : "Sin SubCát.",
                    unidadMedida = p.UnidadMedida,
                    costoPromedio = p.CostoPromedio,
                    displayText = $"[{p.SKU}] {p.Nombre} ({p.Categoria!.Nombre})"
                })
                .AsNoTracking()
                .ToListAsync();

            return new JsonResult(resultados);
        }

        // HANDLER POST DEDICADO Y DIRECTO
        public async Task<IActionResult> OnPostGuardarRecetaAsync([FromBody] RecetaCrearDTO dto)
        {
            if (dto == null || dto.ProductoTerminadoID <= 0 || dto.Detalles == null || !dto.Detalles.Any())
            {
                return new JsonResult(new { success = false, message = "Debe seleccionar un Producto Terminado y agregar al menos un insumo." });
            }

            var existe = await _context.Recetas.AnyAsync(r => r.ProductoTerminadoID == dto.ProductoTerminadoID);
            if (existe)
            {
                return new JsonResult(new { success = false, message = "Este Producto Terminado ya cuenta con una receta/fórmula registrada." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nuevaReceta = new Receta
                {
                    ProductoTerminadoID = dto.ProductoTerminadoID,
                    NombreReceta = dto.NombreReceta,
                    FechaCreacion = DateTime.Now,
                    Estado = true
                };

                _context.Recetas.Add(nuevaReceta);
                await _context.SaveChangesAsync();

                foreach (var item in dto.Detalles)
                {
                    var detalle = new RecetaDetalle
                    {
                        RecetaID = nuevaReceta.RecetaID,
                        InsumoID = item.InsumoID,
                        CantidadRequerida = item.Cantidad,
                        UnidadMedida = item.UnidadMedida
                    };
                    _context.RecetaDetalles.Add(detalle);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new JsonResult(new { success = true, redirectUrl = Url.Page("/Produccion/Recetas/Index") });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new JsonResult(new { success = false, message = $"Error al guardar la receta: {ex.Message}" });
            }
        }

        public class CategoriaDTO
        {
            public int CategoriaID { get; set; }
            public string Codigo2Dig { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
        }

        public class SubCategoriaDTO
        {
            public int SubCategoriaID { get; set; }
            public int CategoriaID { get; set; }
            public string Codigo4Dig { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
        }

        public class RecetaCrearDTO
        {
            public int ProductoTerminadoID { get; set; }
            public string NombreReceta { get; set; } = string.Empty;
            public List<DetalleItemDTO> Detalles { get; set; } = new();
        }

        public class DetalleItemDTO
        {
            public int InsumoID { get; set; }
            public decimal Cantidad { get; set; }
            public string UnidadMedida { get; set; } = "UND";
        }
    }
}