using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;
using System.Text.Json;

namespace ERP.WEB.Pages.Solicitudes
{
    [Authorize]
    public class AprobacionesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AprobacionesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<RequerimientoCabecera> ListaPendientes { get; set; } = new();
        public List<RequerimientoCabecera> ListaHistorial { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            // Cargar Solicitudes Pendientes proyectando explícitamente las propiedades necesarias
            var pendientesBD = await _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(r => r.Estado == "Pendiente")
                .AsNoTracking()
                .OrderByDescending(r => r.FechaSolicitud)
                .ToListAsync();

            // Asegurar que cada detalle tenga asignada su DescripcionItem o Nombre del Producto
            foreach (var req in pendientesBD)
            {
                foreach (var det in req.Detalles)
                {
                    if (string.IsNullOrWhiteSpace(det.DescripcionItem) && det.Producto != null)
                    {
                        det.DescripcionItem = det.Producto.Nombre;
                    }
                }
            }

            ListaPendientes = pendientesBD;

            // Cargar Historial
            ListaHistorial = await _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(r => r.Estado == "Aprobado" || r.Estado == "Rechazado")
                .AsNoTracking()
                .OrderByDescending(r => r.FechaAprobacion)
                .Take(50)
                .ToListAsync();
        }

        // HANDLER: Aprobar Requerimiento (con ajuste opcional de cantidades)
        public async Task<IActionResult> OnPostAprobarAsync(int requerimientoId, string? comentario, string? detallesJson)
        {
            var req = await _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.RequerimientoID == requerimientoId);

            if (req == null)
            {
                MensajeError = "El requerimiento especificado no existe.";
                return RedirectToPage();
            }

            // Procesar reajustes de cantidades aprobadas si se enviaron desde la vista
            if (!string.IsNullOrWhiteSpace(detallesJson))
            {
                var cambios = JsonSerializer.Deserialize<List<AjusteCantidadDTO>>(detallesJson);
                if (cambios != null)
                {
                    foreach (var cambio in cambios)
                    {
                        var det = req.Detalles.FirstOrDefault(d => d.RequerimientoDetalleID == cambio.DetalleID);
                        if (det != null && cambio.NuevaCantidad > 0)
                        {
                            det.Cantidad = cambio.NuevaCantidad;
                        }
                    }
                }
            }

            req.Estado = "Aprobado";
            req.AprobadorID = 1; // Asignar según el ID del usuario autenticado (Jefe)
            req.FechaAprobacion = DateTime.Now;
            req.ComentarioAprobacion = comentario;

            _context.RequerimientosCabecera.Update(req);
            await _context.SaveChangesAsync();

            MensajeExito = $"El Requerimiento {req.Codigo} fue APROBADO correctamente y transferido al área de Compras.";
            return RedirectToPage();
        }

        // HANDLER: Rechazar Requerimiento
        public async Task<IActionResult> OnPostRechazarAsync(int requerimientoId, string comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
            {
                MensajeError = "Es obligatorio ingresar un motivo para rechazar la solicitud.";
                return RedirectToPage();
            }

            var req = await _context.RequerimientosCabecera.FindAsync(requerimientoId);
            if (req == null)
            {
                MensajeError = "El requerimiento especificado no existe.";
                return RedirectToPage();
            }

            req.Estado = "Rechazado";
            req.AprobadorID = 1;
            req.FechaAprobacion = DateTime.Now;
            req.ComentarioAprobacion = comentario;

            _context.RequerimientosCabecera.Update(req);
            await _context.SaveChangesAsync();

            MensajeExito = $"El Requerimiento {req.Codigo} ha sido RECHAZADO.";
            return RedirectToPage();
        }

        public class AjusteCantidadDTO
        {
            public int DetalleID { get; set; }
            public decimal NuevaCantidad { get; set; }
        }
    }
}