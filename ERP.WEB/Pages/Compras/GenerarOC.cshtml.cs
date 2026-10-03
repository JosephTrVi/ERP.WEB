using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;
using System.Text.Json;

namespace ERP.WEB.Pages.Compras
{
    [Authorize]
    public class GenerarOCModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GenerarOCModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public OrdenesCompraCabecera InputOC { get; set; } = new();

        public RequerimientoCabecera RequerimientoBase { get; set; } = null!;

        [BindProperty]
        public string ItemsJson { get; set; } = "[]";

        [BindProperty]
        public string? NombreProveedor { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        [TempData]
        public string? MensajeExito { get; set; }

        public async Task<IActionResult> OnGetAsync(int requerimientoId)
        {
            var req = await _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RequerimientoID == requerimientoId && r.Estado == "Aprobado");

            if (req == null)
            {
                MensajeError = "El requerimiento no existe o no se encuentra en estado Aprobado.";
                return RedirectToPage("/Compras/BandejaRequerimientos");
            }

            RequerimientoBase = req;

            // Generar correlativo OC (ej. OC-2026-0001)
            int anioActual = DateTime.Now.Year;
            int correlativo = await _context.OrdenesCompraCabecera
                .Where(o => o.FechaEmision.Year == anioActual)
                .CountAsync() + 1;

            InputOC.Codigo = $"OC-{anioActual}-{correlativo.ToString("D4")}";
            InputOC.RequerimientoID = req.RequerimientoID;
            InputOC.FechaEmision = DateTime.Now;
            InputOC.FechaEntrega = req.FechaRequerida;
            InputOC.Moneda = req.TipoRequerimiento == "Importacion" ? "USD" : "PEN";
            InputOC.FormaPago = "Crédito 30 días";

            return Page();
        }

        public async Task<IActionResult> OnPostGuardarOCAsync()
        {
            if (string.IsNullOrWhiteSpace(ItemsJson) || ItemsJson == "[]")
            {
                MensajeError = "Debe especificar los precios de los ítems a comprar.";
                return RedirectToPage(new { requerimientoId = InputOC.RequerimientoID });
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var detallesDTO = JsonSerializer.Deserialize<List<ItemOCDTO>>(ItemsJson, options);

            if (detallesDTO == null || !detallesDTO.Any())
            {
                MensajeError = "El detalle de la Orden de Compra es inválido.";
                return RedirectToPage(new { requerimientoId = InputOC.RequerimientoID });
            }

            // Calcular totales
            decimal subTotal = detallesDTO.Sum(d => d.Cantidad * d.PrecioUnitario);
            decimal igv = subTotal * 0.18m;
            decimal total = subTotal + igv;

            InputOC.SubTotal = subTotal;
            InputOC.IGV = igv;
            InputOC.Total = total;
            InputOC.Estado = "Emitido";

            _context.OrdenesCompraCabecera.Add(InputOC);
            await _context.SaveChangesAsync();

            // Guardar detalles de la OC
            foreach (var item in detallesDTO)
            {
                _context.OrdenesCompraDetalle.Add(new OrdenesCompraDetalle
                {
                    OrdenCompraID = InputOC.OrdenCompraID,
                    ProductoID = item.ProductoID > 0 ? item.ProductoID : null,
                    DescripcionItem = item.DescripcionItem,
                    Cantidad = item.Cantidad,
                    UnidadMedida = item.UnidadMedida,
                    PrecioUnitario = item.PrecioUnitario,
                    SubTotalItem = item.Cantidad * item.PrecioUnitario
                });
            }

            // Actualizar estado del Requerimiento origen a "Atendido" o "En Proceso"
            var reqOrigen = await _context.RequerimientosCabecera.FindAsync(InputOC.RequerimientoID);
            if (reqOrigen != null)
            {
                reqOrigen.Estado = "Atendido";
                _context.RequerimientosCabecera.Update(reqOrigen);
            }

            await _context.SaveChangesAsync();

            MensajeExito = $"Orden de Compra {InputOC.Codigo} generada exitosamente por un Total de {InputOC.Moneda} {total:N2}.";
            return RedirectToPage("/Compras/BandejaRequerimientos");
        }

        public class ItemOCDTO
        {
            public int ProductoID { get; set; }
            public string DescripcionItem { get; set; } = string.Empty;
            public decimal Cantidad { get; set; }
            public string UnidadMedida { get; set; } = "UND";
            public decimal PrecioUnitario { get; set; }
        }
    }
}