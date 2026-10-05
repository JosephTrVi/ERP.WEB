using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Ventas;

namespace ERP.WEB.Pages.Ventas
{
    [Authorize]
    public class AprobacionCotizacionesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AprobacionCotizacionesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Propiedad requerida por la vista Razor
        public List<CotizacionesCabecera> Pendientes { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            // Cargar únicamente las cotizaciones que están en estado 'Pendiente'
            Pendientes = await _context.CotizacionesCabecera
                .Include(c => c.Detalles)
                .Where(c => c.Estado == "Pendiente")
                .AsNoTracking()
                .OrderByDescending(c => c.FechaEmision)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostProcesarAsync(int cotizacionId, string accion, string? comentario)
        {
            var cotizacion = await _context.CotizacionesCabecera.FindAsync(cotizacionId);

            if (cotizacion == null)
            {
                MensajeError = "La cotización especificada no existe.";
                return RedirectToPage();
            }

            if (accion == "Aprobar")
            {
                cotizacion.Estado = "Aprobado";
                cotizacion.FechaAprobacion = DateTime.Now;
                cotizacion.ComentarioAprobacion = comentario;
                cotizacion.AprobadorID = 1; // ID de usuario según sesión

                MensajeExito = $"La Cotización {cotizacion.Codigo} ha sido APROBADA exitosamente.";
            }
            else if (accion == "Rechazar")
            {
                cotizacion.Estado = "Rechazado";
                cotizacion.FechaAprobacion = DateTime.Now;
                cotizacion.ComentarioAprobacion = comentario;
                cotizacion.AprobadorID = 1;

                MensajeExito = $"La Cotización {cotizacion.Codigo} ha sido RECHAZADA.";
            }

            _context.CotizacionesCabecera.Update(cotizacion);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}