using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Ventas;


namespace ERP.WEB.Pages.Ventas
{
    [Authorize]
    [ValidateAntiForgeryToken]
    public class DetalleCotizacionModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetalleCotizacionModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public CotizacionesCabecera Cotizacion { get; set; } = default!;

        // Propiedad requerida por DetalleCotizacion.cshtml para el Stock Actual
        public Dictionary<int, decimal> StocksProductos { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // 1. Obtener la Cotización seleccionada con sus detalles
            var cot = await _context.CotizacionesCabecera
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.CotizacionID == id);

            if (cot == null)
            {
                MensajeError = "La cotización solicitada no fue encontrada.";
                return RedirectToPage("/Ventas/AprobacionCotizaciones");
            }

            Cotizacion = cot;

            // 2. Extraer los IDs de Artículos/Productos cotizados
            var productoIds = Cotizacion.Detalles
                .Where(d => d.ProductoID.HasValue)
                .Select(d => d.ProductoID!.Value)
                .Distinct()
                .ToList();

            // 3. Cargar el stock disponible de cada producto en el diccionario
            if (productoIds.Any())
            {
                StocksProductos = await _context.Productos
                    .Where(a => productoIds.Contains(a.ProductoID))
                    .AsNoTracking()
                    .ToDictionaryAsync(a => a.ProductoID, a => a.StockActual);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAprobarAsync(int cotizacionId)
        {
            var cot = await _context.CotizacionesCabecera.FindAsync(cotizacionId);
            if (cot != null)
            {
                cot.Estado = "Aprobada";
                cot.FechaAprobacion = DateTime.Now;
                _context.CotizacionesCabecera.Update(cot);
                await _context.SaveChangesAsync();
                MensajeExito = $"La cotización #{cot.Codigo} ha sido aprobada comercialmente.";
            }

            return RedirectToPage("/Ventas/AprobacionCotizaciones");
        }

        public async Task<IActionResult> OnPostRechazarAsync(int cotizacionId)
        {
            var cot = await _context.CotizacionesCabecera.FindAsync(cotizacionId);
            if (cot != null)
            {
                cot.Estado = "Rechazada";
                _context.CotizacionesCabecera.Update(cot);
                await _context.SaveChangesAsync();
                MensajeExito = $"La cotización #{cot.Codigo} ha sido rechazada.";
            }

            return RedirectToPage("/Ventas/AprobacionCotizaciones");
        }
    }
}