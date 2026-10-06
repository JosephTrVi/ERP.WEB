using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        // Filtros de Fecha (Año y Mes)
        [BindProperty(SupportsGet = true)]
        public int Anio { get; set; } = DateTime.Now.Year;

        [BindProperty(SupportsGet = true)]
        public int Mes { get; set; } = DateTime.Now.Month;

        // Filtros adicionales
        [BindProperty(SupportsGet = true)]
        public string? TipoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        public SelectList SelectAnios { get; set; } = null!;
        public SelectList SelectMeses { get; set; } = null!;

        public List<RequerimientoCabecera> ListaPendientes { get; set; } = new();
        public List<RequerimientoCabecera> ListaHistorial { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Cargar desplegables de Año y Mes
            CargarCombosFecha();

            // ===== 2. QUERIES BASE =====
            var queryPendientes = _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(r => r.Estado == "Pendiente")
                .AsNoTracking()
                .AsQueryable();

            var queryHistorial = _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(r => r.Estado == "Aprobado" || r.Estado == "Rechazado")
                .AsNoTracking()
                .AsQueryable();

            // ===== 3. APLICAR FILTROS EN AMBAS LISTAS =====
            if (Anio > 0)
            {
                queryPendientes = queryPendientes.Where(r => r.FechaSolicitud.Year == Anio);
                queryHistorial = queryHistorial.Where(r => r.FechaSolicitud.Year == Anio);
            }

            if (Mes > 0)
            {
                queryPendientes = queryPendientes.Where(r => r.FechaSolicitud.Month == Mes);
                queryHistorial = queryHistorial.Where(r => r.FechaSolicitud.Month == Mes);
            }

            if (!string.IsNullOrWhiteSpace(TipoFiltro))
            {
                queryPendientes = queryPendientes.Where(r => r.TipoRequerimiento == TipoFiltro);
                queryHistorial = queryHistorial.Where(r => r.TipoRequerimiento == TipoFiltro);
            }

            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string t = Busqueda.Trim().ToLower();
                queryPendientes = queryPendientes.Where(r => r.Codigo.ToLower().Contains(t) || (r.Observaciones != null && r.Observaciones.ToLower().Contains(t)));
                queryHistorial = queryHistorial.Where(r => r.Codigo.ToLower().Contains(t) || (r.Observaciones != null && r.Observaciones.ToLower().Contains(t)));
            }

            // ===== 4. EJECUTAR CONSULTAS =====
            var pendientesBD = await queryPendientes
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

            ListaHistorial = await queryHistorial
                .OrderByDescending(r => r.FechaAprobacion ?? r.FechaSolicitud)
                .ToListAsync();
        }

        private void CargarCombosFecha()
        {
            int anioActual = DateTime.Now.Year;
            var listaAnios = new List<int>();
            for (int i = anioActual; i >= 2022; i--)
            {
                listaAnios.Add(i);
            }
            SelectAnios = new SelectList(listaAnios, Anio);

            var listaMeses = new[]
            {
                new { Id = 0, Nombre = "-- Todos los Meses --" },
                new { Id = 1, Nombre = "Enero" },
                new { Id = 2, Nombre = "Febrero" },
                new { Id = 3, Nombre = "Marzo" },
                new { Id = 4, Nombre = "Abril" },
                new { Id = 5, Nombre = "Mayo" },
                new { Id = 6, Nombre = "Junio" },
                new { Id = 7, Nombre = "Julio" },
                new { Id = 8, Nombre = "Agosto" },
                new { Id = 9, Nombre = "Septiembre" },
                new { Id = 10, Nombre = "Octubre" },
                new { Id = 11, Nombre = "Noviembre" },
                new { Id = 12, Nombre = "Diciembre" }
            };
            SelectMeses = new SelectList(listaMeses, "Id", "Nombre", Mes);
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