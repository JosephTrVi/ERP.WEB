using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Inventario;
using ERP.WEB.Models.Ventas;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ERP.WEB.Pages.Ventas
{
    [Authorize]
    public class NuevaCotizacionModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public NuevaCotizacionModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CotizacionesCabecera InputCotiz { get; set; } = new();

        [BindProperty]
        public string ItemsJson { get; set; } = "[]";

        // Desplegable de SubCategorías pertenecientes a Productos Terminados (Código "03")
        public SelectList SelectSubCategoriasPT { get; set; } = null!;

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            int anioActual = DateTime.Now.Year;
            int correlativo = await _context.CotizacionesCabecera
                .Where(c => c.FechaEmision.Year == anioActual)
                .CountAsync() + 1;

            InputCotiz.Codigo = $"COT-{anioActual}-{correlativo.ToString("D4")}";
            InputCotiz.FechaEmision = DateTime.Now;
            InputCotiz.FechaEntregaAprox = DateTime.Now.AddDays(5);
            InputCotiz.TipoCambio = 3.7500m;

            // Cargar SubCategorías asociadas a Producto Terminado (Codigo "03" o CategoriaID 3)
            var subCats = await _context.SubCategorias
                .Include(s => s.Categoria)
                .Where(s => s.Estado && (s.Categoria.Codigo == "03" || s.CategoriaID == 3))
                .AsNoTracking()
                .ToListAsync();

            SelectSubCategoriasPT = new SelectList(subCats, "SubCategoriaID", "Nombre");
        }

        // HANDLER AJAX: BUSCAR PRODUCTOS TERMINADOS (CATEGORÍA "03")
        public async Task<JsonResult> OnGetBuscarProductosTerminadosAsync(int? subCategoriaId, string? query)
        {
            var q = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.SubCategoria)
                .Where(p => p.Estado && (p.Categoria.Codigo == "03" || p.CategoriaID == 3))
                .AsNoTracking()
                .AsQueryable();

            if (subCategoriaId.HasValue && subCategoriaId.Value > 0)
            {
                q = q.Where(p => p.SubcategoriaID == subCategoriaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                string t = query.Trim().ToLower();
                q = q.Where(p => p.SKU.ToLower().Contains(t) || p.Nombre.ToLower().Contains(t));
            }

            var resultados = await q.Take(20).Select(p => new
            {
                productoID = p.ProductoID,
                sku = p.SKU,
                nombre = p.Nombre,
                categoriaSubCat = $"{p.Categoria.Nombre} / {p.SubCategoria.Nombre}",
                unidadMedida = p.UnidadMedida,
                stockActual = p.StockActual,
                precioSinIGV = 100.00
            }).ToListAsync();

            return new JsonResult(resultados);
        }

        // HANDLER AJAX: GENERAR SKU PARA PRODUCTO TERMINADO (Ej: 03-01-0001)
        public async Task<JsonResult> OnGetGenerarSkuPTAsync(int subCategoriaId)
        {
            var subCat = await _context.SubCategorias
                .Include(s => s.Categoria)
                .FirstOrDefaultAsync(s => s.SubCategoriaID == subCategoriaId);

            if (subCat == null) return new JsonResult(new { sku = "" });

            string prefijoCat = subCat.Categoria.CodigoPrefijo ?? subCat.Categoria.Codigo;
            string prefijoSub = subCat.CodigoPrefijo ?? subCat.Codigo;

            int correlativo = await _context.Productos.CountAsync(p => p.SubcategoriaID == subCategoriaId) + 1;

            // Genera SKU con formato numérico de dos dígitos (ej. 03-01-0001)
            string skuGenerado = $"{prefijoCat}{prefijoSub}{correlativo.ToString("D4")}";

            return new JsonResult(new { sku = skuGenerado, subCategoriaID = subCat.SubCategoriaID });
        }

        // HANDLER AJAX: CREAR ARTÍCULO TERMINADO ON-THE-FLY
        public async Task<JsonResult> OnPostCrearProductoExpressAsync([FromBody] Producto nuevoProd)
        {
            if (string.IsNullOrWhiteSpace(nuevoProd.Nombre) || nuevoProd.SubcategoriaID <= 0)
            {
                return new JsonResult(new { success = false, message = "El nombre y la subcategoría son obligatorios." });
            }

            var catPT = await _context.Categorias.FirstOrDefaultAsync(c => c.Codigo == "03" || c.CategoriaID == 3);
            if (catPT == null)
            {
                return new JsonResult(new { success = false, message = "No se encontró la categoría de Producto Terminado (Código 03)." });
            }

            nuevoProd.CategoriaID = catPT.CategoriaID;
            nuevoProd.Estado = true;
            nuevoProd.FechaRegistro = DateTime.Now;

            _context.Productos.Add(nuevoProd);
            await _context.SaveChangesAsync();

            var prodCreado = new
            {
                productoID = nuevoProd.ProductoID,
                sku = nuevoProd.SKU,
                nombre = nuevoProd.Nombre,
                unidadMedida = nuevoProd.UnidadMedida,
                stockActual = nuevoProd.StockActual,
                precioSinIGV = 100.00
            };

            return new JsonResult(new { success = true, producto = prodCreado });
        }

        // HANDLER AJAX: Buscar Clientes por RUC, DNI o Razón Social/Nombre
        public async Task<JsonResult> OnGetBuscarClientesAsync(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return new JsonResult(new List<object>());
            }

            string termino = q.Trim().ToLower();

            var clientes = await _context.Clientes
                .Where(c => c.Estado &&
                           (c.NumDocumento.ToLower().Contains(termino) ||
                            c.RazonSocial.ToLower().Contains(termino)))
                .Take(15)
                .Select(c => new
                {
                    id = c.ClienteID,
                    documento = c.NumDocumento,
                    nombre = c.RazonSocial,
                    direccion = c.Direccion,
                    email = c.EmailContacto,
                    telefono = c.Telefono,
                    displayText = $"[{c.NumDocumento}] {c.RazonSocial}"
                })
                .AsNoTracking()
                .ToListAsync();

            return new JsonResult(clientes);
        }

        public async Task<IActionResult> OnPostGuardarCotizacionAsync()
        {
            if (string.IsNullOrWhiteSpace(ItemsJson) || ItemsJson == "[]")
            {
                MensajeError = "Debe agregar al menos un ítem a la cotización.";
                await OnGetAsync();
                return Page();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var detallesDTO = JsonSerializer.Deserialize<List<DetalleCotizacionDTO>>(ItemsJson, options);

            if (detallesDTO == null || !detallesDTO.Any())
            {
                MensajeError = "El detalle de la cotización no es válido.";
                await OnGetAsync();
                return Page();
            }

            decimal subTotal = detallesDTO.Sum(d => d.Cantidad * d.PrecioUnitarioSinIGV);
            decimal igv = subTotal * 0.18m;
            decimal total = subTotal + igv;

            InputCotiz.SubTotal = subTotal;
            InputCotiz.IGV = igv;
            InputCotiz.Total = total;
            InputCotiz.Estado = "Pendiente";
            InputCotiz.VendedorID = 1;

            _context.CotizacionesCabecera.Add(InputCotiz);
            await _context.SaveChangesAsync();

            foreach (var item in detallesDTO)
            {
                _context.CotizacionesDetalle.Add(new CotizacionesDetalle
                {
                    CotizacionID = InputCotiz.CotizacionID,
                    ProductoID = item.ProductoID > 0 ? item.ProductoID : null,
                    DescripcionItem = item.DescripcionItem,
                    UnidadMedida = string.IsNullOrWhiteSpace(item.UnidadMedida) ? "UND" : item.UnidadMedida,
                    Cantidad = item.Cantidad,
                    PrecioUnitarioSinIGV = item.PrecioUnitarioSinIGV,
                    SubTotalItem = item.Cantidad * item.PrecioUnitarioSinIGV
                });
            }

            await _context.SaveChangesAsync();

            MensajeExito = $"Cotización {InputCotiz.Codigo} registrada exitosamente.";
            return RedirectToPage("/Ventas/BandejaCotizaciones");
        }

        public class DetalleCotizacionDTO
        {
            public int ProductoID { get; set; }
            public string DescripcionItem { get; set; } = string.Empty;
            public string UnidadMedida { get; set; } = "UND";
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitarioSinIGV { get; set; }
        }
    }
}