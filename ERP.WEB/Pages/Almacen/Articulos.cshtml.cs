using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Inventario;

namespace ERP.WEB.Pages.Almacen
{
    [Authorize]
    public class ArticulosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ArticulosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Producto> ListaProductos { get; set; } = new();
        public SelectList SelectCategorias { get; set; } = null!;
        public SelectList SelectSubCategorias { get; set; } = null!;

        // Propiedades de Filtro (Soportan peticiones GET)
        [BindProperty(SupportsGet = true)]
        public int? CategoriaFiltroID { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SubCategoriaFiltroID { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        // Propiedades auxiliares para recibir los filtros activos desde el formulario modal
        [BindProperty]
        public int? CatFiltro { get; set; }

        [BindProperty]
        public int? SubCatFiltro { get; set; }

        [BindProperty]
        public string? BusquedaFiltro { get; set; }

        // Control de ejecución de búsqueda
        public bool SeEfectuoBusqueda { get; set; } = false;

        [BindProperty]
        public Producto InputProducto { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Cargar combo de Categorías
            var categorias = await _context.Categorias.Where(c => c.Estado).AsNoTracking().ToListAsync();
            SelectCategorias = new SelectList(categorias, "CategoriaID", "Nombre", CategoriaFiltroID);

            // 2. Cargar SubCategorías para el filtro
            var querySubCat = _context.SubCategorias.Where(s => s.Estado).AsQueryable();
            if (CategoriaFiltroID.HasValue && CategoriaFiltroID > 0)
            {
                querySubCat = querySubCat.Where(s => s.CategoriaID == CategoriaFiltroID.Value);
            }

            var subcategorias = await querySubCat.AsNoTracking().ToListAsync();
            SelectSubCategorias = new SelectList(subcategorias, "SubCategoriaID", "Nombre", SubCategoriaFiltroID);

            // 3. Evaluar si el usuario aplicó algún criterio de búsqueda
            SeEfectuoBusqueda = (CategoriaFiltroID.HasValue && CategoriaFiltroID > 0) ||
                               (SubCategoriaFiltroID.HasValue && SubCategoriaFiltroID > 0) ||
                               !string.IsNullOrWhiteSpace(Busqueda);

            // 4. Consultar base de datos solo si se presionó "Filtrar"
            if (SeEfectuoBusqueda)
            {
                var query = _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.SubCategoria)
                    .AsNoTracking()
                    .AsQueryable();

                if (CategoriaFiltroID.HasValue && CategoriaFiltroID > 0)
                {
                    query = query.Where(p => p.CategoriaID == CategoriaFiltroID.Value);
                }

                if (SubCategoriaFiltroID.HasValue && SubCategoriaFiltroID > 0)
                {
                    query = query.Where(p => p.SubcategoriaID == SubCategoriaFiltroID.Value);
                }

                if (!string.IsNullOrWhiteSpace(Busqueda))
                {
                    string termino = Busqueda.Trim().ToLower();
                    query = query.Where(p => p.SKU.ToLower().Contains(termino) || p.Nombre.ToLower().Contains(termino));
                }

                // Ordenar por ProductoID para evitar errores por columnas no existentes
                ListaProductos = await query.OrderByDescending(p => p.ProductoID).ToListAsync();
            }
        }

        // HANDLER AJAX: Devuelve SubCategorías por Categoría en formato JSON
        public async Task<JsonResult> OnGetSubCategoriasPorCategoriaAsync(int categoriaId)
        {
            var subcategorias = await _context.SubCategorias
                .Where(s => s.CategoriaID == categoriaId && s.Estado)
                .Select(s => new { id = s.SubCategoriaID, nombre = s.Nombre })
                .AsNoTracking()
                .ToListAsync();

            return new JsonResult(subcategorias);
        }

        // HANDLER AJAX: Genera el SKU correlativo automáticamente
        public async Task<JsonResult> OnGetGenerarCodigoAsync(int categoriaId, int subCategoriaId)
        {
            var categoria = await _context.Categorias.FindAsync(categoriaId);
            var subcategoria = await _context.SubCategorias.FindAsync(subCategoriaId);

            if (categoria == null || subcategoria == null)
            {
                return new JsonResult(new { codigo = "" });
            }

            string prefijoCat = !string.IsNullOrEmpty(categoria.Codigo) ? categoria.Codigo.PadLeft(2, '0') : categoriaId.ToString("D2");
            string prefijoSubCat = !string.IsNullOrEmpty(subcategoria.Codigo) ? subcategoria.Codigo.PadLeft(2, '0') : subCategoriaId.ToString("D2");
            string prefijoBase = $"{prefijoCat}{prefijoSubCat}";

            var ultimoProducto = await _context.Productos
                .Where(p => p.CategoriaID == categoriaId && p.SubcategoriaID == subCategoriaId && p.SKU.StartsWith(prefijoBase))
                .OrderByDescending(p => p.SKU)
                .FirstOrDefaultAsync();

            int correlativo = 1;

            if (ultimoProducto != null && ultimoProducto.SKU.Length >= prefijoBase.Length)
            {
                string sufijo = ultimoProducto.SKU.Substring(prefijoBase.Length);
                if (int.TryParse(sufijo, out int ultimoCorrelativo))
                {
                    correlativo = ultimoCorrelativo + 1;
                }
            }

            string nuevoCodigo = $"{prefijoBase}{correlativo.ToString("D4")}";

            return new JsonResult(new { codigo = nuevoCodigo });
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            if (InputProducto.ProductoID == 0)
            {
                InputProducto.FechaRegistro = DateTime.Now;
                _context.Productos.Add(InputProducto);
                MensajeExito = "Artículo registrado correctamente.";
            }
            else
            {
                var prodBD = await _context.Productos.FindAsync(InputProducto.ProductoID);
                if (prodBD != null)
                {
                    prodBD.SKU = InputProducto.SKU;
                    prodBD.Nombre = InputProducto.Nombre;
                    prodBD.Descripcion = InputProducto.Descripcion;
                    prodBD.CategoriaID = InputProducto.CategoriaID;
                    prodBD.SubcategoriaID = InputProducto.SubcategoriaID;
                    prodBD.UnidadMedida = InputProducto.UnidadMedida;
                    prodBD.StockMinimo = InputProducto.StockMinimo;
                    prodBD.StockMaximo = InputProducto.StockMaximo;
                    prodBD.PrecioVenta = InputProducto.PrecioVenta;
                    prodBD.Estado = InputProducto.Estado;

                    _context.Productos.Update(prodBD);
                    MensajeExito = "Artículo actualizado correctamente.";
                }
            }

            await _context.SaveChangesAsync();

            // Preservar filtros tras guardar
            int? catARedirigir = CatFiltro.HasValue && CatFiltro > 0 ? CatFiltro : InputProducto.CategoriaID;
            int? subCatARedirigir = SubCatFiltro.HasValue && SubCatFiltro > 0 ? SubCatFiltro : InputProducto.SubcategoriaID;
            string busquedaARedirigir = !string.IsNullOrWhiteSpace(BusquedaFiltro) ? BusquedaFiltro : InputProducto.SKU;

            return RedirectToPage(new
            {
                CategoriaFiltroID = catARedirigir,
                SubCategoriaFiltroID = subCatARedirigir,
                Busqueda = busquedaARedirigir
            });
        }
    }
}