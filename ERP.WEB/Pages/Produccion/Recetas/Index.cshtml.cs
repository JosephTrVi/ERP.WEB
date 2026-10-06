using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Produccion;

namespace ERP.WEB.Pages.Produccion.Recetas
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<RecetaItemDTO> ListaRecetas { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Buscar { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoriaID { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SubcategoriaID { get; set; }

        public SelectList SelectCategorias { get; set; } = null!;
        public SelectList SelectSubcategorias { get; set; } = null!;

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            await CargarCombosFiltroAsync();

            // Solo consulta si el usuario ha presionado el botón "Filtrar"
            bool esPeticionFiltrada = Request.Query.ContainsKey("Buscar") ||
                                     Request.Query.ContainsKey("CategoriaID") ||
                                     Request.Query.ContainsKey("SubcategoriaID");

            if (!esPeticionFiltrada)
            {
                ListaRecetas = new List<RecetaItemDTO>();
                return;
            }

            var query = _context.Recetas
                .Include(r => r.ProductoTerminado)
                    .ThenInclude(p => p!.Categoria)
                .Include(r => r.ProductoTerminado)
                    .ThenInclude(p => p!.SubCategoria)
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Insumo)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Buscar))
            {
                string filtro = Buscar.Trim().ToLower();
                query = query.Where(r =>
                    r.NombreReceta.ToLower().Contains(filtro) ||
                    (r.ProductoTerminado != null && r.ProductoTerminado.SKU.ToLower().Contains(filtro)) ||
                    (r.ProductoTerminado != null && r.ProductoTerminado.Nombre.ToLower().Contains(filtro)));
            }

            if (CategoriaID.HasValue && CategoriaID.Value > 0)
            {
                query = query.Where(r => r.ProductoTerminado != null && r.ProductoTerminado.CategoriaID == CategoriaID.Value);
            }

            if (SubcategoriaID.HasValue && SubcategoriaID.Value > 0)
            {
                query = query.Where(r => r.ProductoTerminado != null && r.ProductoTerminado.SubcategoriaID == SubcategoriaID.Value);
            }

            ListaRecetas = await query
                .OrderByDescending(r => r.FechaCreacion)
                .Select(r => new RecetaItemDTO
                {
                    RecetaID = r.RecetaID,
                    NombreReceta = r.NombreReceta,
                    ProductoTerminadoID = r.ProductoTerminadoID,
                    SKUPT = r.ProductoTerminado != null ? r.ProductoTerminado.SKU : "",
                    NombrePT = r.ProductoTerminado != null ? r.ProductoTerminado.Nombre : "",
                    CategoriaPT = r.ProductoTerminado != null && r.ProductoTerminado.Categoria != null ? r.ProductoTerminado.Categoria.Nombre : "Sin Cát.",
                    SubCategoriaPT = r.ProductoTerminado != null && r.ProductoTerminado.SubCategoria != null ? r.ProductoTerminado.SubCategoria.Nombre : "Sin SubCát.",
                    TotalInsumos = r.Detalles.Count,
                    CostoEstimadoTotal = r.Detalles.Sum(d => d.CantidadRequerida * (d.Insumo != null ? d.Insumo.CostoPromedio : 0)),
                    FechaCreacion = r.FechaCreacion,
                    Estado = r.Estado
                })
                .ToListAsync();
        }

        // ENDPOINT JSON: Carga únicamente las subcategorías hijas de la categoría seleccionada
        public async Task<JsonResult> OnGetSubcategoriasPorCategoriaAsync(int categoriaId)
        {
            var querySubcat = _context.SubCategorias
                .Where(s => s.Estado && s.EsReceta)
                .AsQueryable();

            if (categoriaId > 0)
            {
                querySubcat = querySubcat.Where(s => s.CategoriaID == categoriaId);
            }

            var subcategorias = await querySubcat
                .OrderBy(s => s.Nombre)
                .Select(s => new {
                    value = s.SubCategoriaID,
                    text = $"[{s.Codigo}] {s.Nombre}"
                })
                .ToListAsync();

            return new JsonResult(subcategorias);
        }

        private async Task CargarCombosFiltroAsync()
        {
            var categorias = await _context.Categorias
                .Where(c => c.Estado && c.EsReceta)
                .OrderBy(c => c.Nombre)
                .Select(c => new { c.CategoriaID, Nombre = $"[{c.Codigo}] {c.Nombre}" })
                .ToListAsync();

            SelectCategorias = new SelectList(categorias, "CategoriaID", "Nombre", CategoriaID);

            var querySubcat = _context.SubCategorias
                .Where(s => s.Estado && s.EsReceta)
                .AsQueryable();

            if (CategoriaID.HasValue && CategoriaID.Value > 0)
            {
                querySubcat = querySubcat.Where(s => s.CategoriaID == CategoriaID.Value);
            }

            var subcategorias = await querySubcat
                .OrderBy(s => s.Nombre)
                .Select(s => new { s.SubCategoriaID, Nombre = $"[{s.Codigo}] {s.Nombre}" })
                .ToListAsync();

            SelectSubcategorias = new SelectList(subcategorias, "SubCategoriaID", "Nombre", SubcategoriaID);
        }

        public async Task<IActionResult> OnPostCambiarEstadoAsync(int recetaId)
        {
            var receta = await _context.Recetas.FindAsync(recetaId);
            if (receta == null)
            {
                MensajeError = "La receta no fue encontrada.";
                return RedirectToPage();
            }

            receta.Estado = !receta.Estado;
            _context.Recetas.Update(receta);
            await _context.SaveChangesAsync();

            MensajeExito = $"El estado de la receta '{receta.NombreReceta}' fue actualizado correctamente.";
            return RedirectToPage(new { Buscar, CategoriaID, SubcategoriaID });
        }

        public async Task<IActionResult> OnGetObtenerDetalleAsync(int recetaId)
        {
            var receta = await _context.Recetas
                .Include(r => r.ProductoTerminado)
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Insumo)
                        .ThenInclude(i => i!.Categoria)
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Insumo)
                        .ThenInclude(i => i!.SubCategoria)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RecetaID == recetaId);

            if (receta == null)
            {
                return new JsonResult(new { success = false, message = "Receta no encontrada." })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var result = new
            {
                success = true,
                recetaID = receta.RecetaID,
                nombreReceta = receta.NombreReceta,
                skuPT = receta.ProductoTerminado?.SKU,
                nombrePT = receta.ProductoTerminado?.Nombre,
                detalles = receta.Detalles.Select(d => new
                {
                    skuInsumo = d.Insumo?.SKU,
                    nombreInsumo = d.Insumo?.Nombre,
                    categoria = d.Insumo?.Categoria?.Nombre ?? "Sin Cát.",
                    subCategoria = d.Insumo?.SubCategoria?.Nombre ?? "Sin SubCát.",
                    cantidad = d.CantidadRequerida,
                    unidadMedida = d.UnidadMedida,
                    costoUnitario = d.Insumo?.CostoPromedio ?? 0,
                    subtotal = d.CantidadRequerida * (d.Insumo?.CostoPromedio ?? 0)
                }).ToList()
            };

            return new JsonResult(result);
        }

        public class RecetaItemDTO
        {
            public int RecetaID { get; set; }
            public string NombreReceta { get; set; } = string.Empty;
            public int ProductoTerminadoID { get; set; }
            public string SKUPT { get; set; } = string.Empty;
            public string NombrePT { get; set; } = string.Empty;
            public string CategoriaPT { get; set; } = string.Empty;
            public string SubCategoriaPT { get; set; } = string.Empty;
            public int TotalInsumos { get; set; }
            public decimal CostoEstimadoTotal { get; set; }
            public DateTime FechaCreacion { get; set; }
            public bool Estado { get; set; }
        }
    }
}