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

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var cot = await _context.CotizacionesCabecera
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.CotizacionID == id);

            if (cot == null)
            {
                MensajeError = "La cotización solicitada no fue encontrada.";
                return RedirectToPage("/Ventas/AprobacionCotizaciones");
            }

            // Opcional: Si tienes el stock en la tabla de Articulos/Productos, 
            // EF Core cargará la información para relacionarla en la vista.
            Cotizacion = cot;
            return Page();
        }
        public async Task<IActionResult> OnPostAprobarAsync(int cotizacionId)
        {
            try
            {
                var cotizacion = await _context.CotizacionesCabecera
                    .FirstOrDefaultAsync(c => c.CotizacionID == cotizacionId);

                if (cotizacion == null)
                {
                    MensajeError = "La cotización no fue encontrada.";
                    return RedirectToPage("/Ventas/AprobacionCotizaciones");
                }

                if (cotizacion.Estado == "Aprobada")
                {
                    MensajeError = "La cotización ya se encuentra aprobada.";
                    return RedirectToPage(new { id = cotizacionId });
                }

                cotizacion.Estado = "Aprobada";
                cotizacion.FechaAprobacion = DateTime.Now;

                _context.CotizacionesCabecera.Update(cotizacion);
                await _context.SaveChangesAsync();

                MensajeExito = $"La Cotización #{cotizacion.Codigo} fue APROBADA exitosamente y quedó lista para Producción.";
                return RedirectToPage("/Ventas/AprobacionCotizaciones");
            }
            catch (Exception ex)
            {
                MensajeError = $"Error al aprobar la cotización: {ex.Message}";
                return RedirectToPage(new { id = cotizacionId });
            }
        }

        public async Task<IActionResult> OnPostRechazarAsync(int cotizacionId)
        {
            try
            {
                var cotizacion = await _context.CotizacionesCabecera
                    .FirstOrDefaultAsync(c => c.CotizacionID == cotizacionId);

                if (cotizacion == null)
                {
                    MensajeError = "La cotización no fue encontrada.";
                    return RedirectToPage("/Ventas/AprobacionCotizaciones");
                }

                cotizacion.Estado = "Rechazada";
                _context.CotizacionesCabecera.Update(cotizacion);
                await _context.SaveChangesAsync();

                MensajeExito = $"La Cotización #{cotizacion.Codigo} fue RECHAZADA.";
                return RedirectToPage("/Ventas/AprobacionCotizaciones");
            }
            catch (Exception ex)
            {
                MensajeError = $"Error al rechazar la cotización: {ex.Message}";
                return RedirectToPage(new { id = cotizacionId });
            }
        }
    }
}