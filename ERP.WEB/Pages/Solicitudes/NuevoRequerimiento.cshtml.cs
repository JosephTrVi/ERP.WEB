using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Inventario;
using ERP.WEB.Models.Logistica;
using System.Text.Json;

namespace ERP.WEB.Pages.Solicitudes
{
    [Authorize]
    public class NuevoRequerimientoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public NuevoRequerimientoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RequerimientoCabecera InputRequerimiento { get; set; } = new();

        [BindProperty]
        public string ItemsJson { get; set; } = "[]";

        public SelectList SelectCategorias { get; set; } = null!;

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            var categorias = await _context.Categorias.Where(c => c.Estado).AsNoTracking().ToListAsync();
            SelectCategorias = new SelectList(categorias, "CategoriaID", "Nombre");

            // Generar código correlativo (ej. REQ-2026-0001)
            int anioActual = DateTime.Now.Year;
            int correlativo = await _context.RequerimientosCabecera
                .Where(r => r.FechaSolicitud.Year == anioActual)
                .CountAsync() + 1;

            InputRequerimiento.Codigo = $"REQ-{anioActual}-{correlativo.ToString("D4")}";
            InputRequerimiento.FechaRequerida = DateTime.Now.AddDays(7);
        }

        public async Task<JsonResult> OnGetBuscarArticulosAsync(int? categoriaId, int? subCategoriaId, string? busqueda, string tipoReq)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.SubCategoria)
                .Where(p => p.Estado)
                .AsNoTracking()
                .AsQueryable();

            if (tipoReq == "Servicio")
            {
                query = query.Where(p => !p.ControlaStock);
            }

            if (categoriaId.HasValue && categoriaId > 0)
                query = query.Where(p => p.CategoriaID == categoriaId.Value);

            if (subCategoriaId.HasValue && subCategoriaId > 0)
                query = query.Where(p => p.SubcategoriaID == subCategoriaId.Value);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string t = busqueda.Trim().ToLower();
                query = query.Where(p => p.SKU.ToLower().Contains(t) || p.Nombre.ToLower().Contains(t));
            }

            var resultados = await query.Select(p => new
            {
                productoID = p.ProductoID,
                sku = p.SKU,
                nombre = p.Nombre,
                categoria = p.Categoria != null ? p.Categoria.Nombre : "",
                subCategoria = p.SubCategoria != null ? p.SubCategoria.Nombre : "",
                unidadMedida = p.UnidadMedida,
                controlaStock = p.ControlaStock,
                stockActual = p.StockActual
            }).ToListAsync();

            return new JsonResult(resultados);
        }

        public async Task<JsonResult> OnPostCrearArticuloExpressAsync([FromBody] Producto nuevoProd)
        {
            ModelState.Remove("Categoria");
            ModelState.Remove("SubCategoria");

            if (string.IsNullOrWhiteSpace(nuevoProd.Nombre) || string.IsNullOrWhiteSpace(nuevoProd.SKU))
            {
                return new JsonResult(new { success = false, message = "SKU y Nombre son requeridos." });
            }

            _context.Productos.Add(nuevoProd);
            await _context.SaveChangesAsync();

            var prodCreado = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.SubCategoria)
                .FirstOrDefaultAsync(p => p.ProductoID == nuevoProd.ProductoID);

            return new JsonResult(new
            {
                success = true,
                producto = new
                {
                    productoID = prodCreado!.ProductoID,
                    sku = prodCreado.SKU,
                    nombre = prodCreado.Nombre,
                    categoria = prodCreado.Categoria?.Nombre ?? "",
                    subCategoria = prodCreado.SubCategoria?.Nombre ?? "",
                    unidadMedida = prodCreado.UnidadMedida,
                    controlaStock = prodCreado.ControlaStock,
                    stockActual = prodCreado.StockActual
                }
            });
        }

        public async Task<JsonResult> OnGetSubCategoriasAsync(int categoriaId)
        {
            var subs = await _context.SubCategorias
                .Where(s => s.CategoriaID == categoriaId && s.Estado)
                .Select(s => new { id = s.SubCategoriaID, nombre = s.Nombre })
                .AsNoTracking()
                .ToListAsync();

            return new JsonResult(subs);
        }

        public async Task<JsonResult> OnGetGenerarCodigoAsync(int categoriaId, int subCategoriaId)
        {
            var cat = await _context.Categorias.FindAsync(categoriaId);
            var subcat = await _context.SubCategorias.FindAsync(subCategoriaId);

            string pCat = cat?.Codigo?.PadLeft(2, '0') ?? categoriaId.ToString("D2");
            string pSub = subcat?.Codigo?.PadLeft(2, '0') ?? subCategoriaId.ToString("D2");
            string pBase = $"{pCat}{pSub}";

            var ult = await _context.Productos
                .Where(p => p.CategoriaID == categoriaId && p.SubcategoriaID == subCategoriaId && p.SKU.StartsWith(pBase))
                .OrderByDescending(p => p.SKU)
                .FirstOrDefaultAsync();

            int corr = 1;
            if (ult != null && ult.SKU.Length >= pBase.Length)
            {
                if (int.TryParse(ult.SKU.Substring(pBase.Length), out int uNum))
                    corr = uNum + 1;
            }

            return new JsonResult(new { codigo = $"{pBase}{corr.ToString("D4")}" });
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            if (string.IsNullOrWhiteSpace(ItemsJson) || ItemsJson == "[]")
            {
                MensajeError = "Debe agregar al menos un artículo o servicio al requerimiento.";
                await OnGetAsync();
                return Page();
            }

            InputRequerimiento.SolicitanteID = 1;
            InputRequerimiento.FechaSolicitud = DateTime.Now;
            InputRequerimiento.Estado = "Pendiente";

            _context.RequerimientosCabecera.Add(InputRequerimiento);
            await _context.SaveChangesAsync();

            var listaItems = JsonSerializer.Deserialize<List<DetalleDTO>>(ItemsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (listaItems != null)
            {
                foreach (var item in listaItems)
                {
                    // Si por alguna razón el nombre viene vacío, obtenerlo de la BD usando el ProductoID
                    string nombreFinal = item.Nombre;
                    if (string.IsNullOrWhiteSpace(nombreFinal) && item.ProductoID > 0)
                    {
                        var prod = await _context.Productos.FindAsync(item.ProductoID);
                        if (prod != null)
                        {
                            nombreFinal = prod.Nombre;
                        }
                    }

                    _context.RequerimientosDetalle.Add(new RequerimientoDetalle
                    {
                        RequerimientoID = InputRequerimiento.RequerimientoID,
                        ProductoID = item.ProductoID > 0 ? item.ProductoID : null,
                        DescripcionItem = string.IsNullOrWhiteSpace(nombreFinal) ? "Artículo / Servicio" : nombreFinal,
                        Cantidad = item.Cantidad,
                        UnidadMedida = string.IsNullOrWhiteSpace(item.UnidadMedida) ? "UND" : item.UnidadMedida,
                        EspecificacionesTecnicas = item.Observaciones,
                        EstadoItem = "Pendiente"
                    });
                }
                await _context.SaveChangesAsync();
            }

            MensajeExito = $"Requerimiento {InputRequerimiento.Codigo} registrado correctamente.";
            return RedirectToPage("/Solicitudes/Index");
        }

        public class DetalleDTO
        {
            public int ProductoID { get; set; }
            public string Nombre { get; set; } = string.Empty; // <-- A veces viene como 'nombre' o 'descripcion'
            public string UnidadMedida { get; set; } = "UND";
            public decimal Cantidad { get; set; }
            public string? Observaciones { get; set; }
        }
    }
}