using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ERP.WEB.Pages.Compras
{
    [Authorize]
    public class BandejaRequerimientosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BandejaRequerimientosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // PROPIEDAD RENOMBRADA PARA COINCIDIR CON LA VISTA RAZOR
        public List<RequerimientoCabecera> RequerimientosAprobados { get; set; } = new();

        // PROPIEDADES TEMPDATA PARA MENSAJES DE ÉXITO Y ERROR
        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

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

        public async Task OnGetAsync()
        {
            CargarCombosFecha();

            // En la bandeja de Compras solo se procesan Requerimientos Aprobados por las Jefaturas
            var query = _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(r => r.Estado == "Aprobado")
                .AsNoTracking()
                .AsQueryable();

            // 1. Filtro por Año (FechaSolicitud o FechaAprobacion)
            if (Anio > 0)
            {
                query = query.Where(r => r.FechaSolicitud.Year == Anio);
            }

            // 2. Filtro por Mes (0 = Todos los meses)
            if (Mes > 0)
            {
                query = query.Where(r => r.FechaSolicitud.Month == Mes);
            }

            // 3. Filtro por Tipo de Requerimiento
            if (!string.IsNullOrWhiteSpace(TipoFiltro))
            {
                query = query.Where(r => r.TipoRequerimiento == TipoFiltro);
            }

            // 4. Búsqueda por Código u Observaciones
            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string t = Busqueda.Trim().ToLower();
                query = query.Where(r => r.Codigo.ToLower().Contains(t) ||
                                         (r.Observaciones != null && r.Observaciones.ToLower().Contains(t)));
            }

            var resultados = await query
                .OrderByDescending(r => r.FechaAprobacion ?? r.FechaSolicitud)
                .ToListAsync();

            // Asegurar asignación de nombres de productos en la proyección de detalles
            foreach (var req in resultados)
            {
                foreach (var det in req.Detalles)
                {
                    if (string.IsNullOrWhiteSpace(det.DescripcionItem) && det.Producto != null)
                    {
                        det.DescripcionItem = det.Producto.Nombre;
                    }
                }
            }

            RequerimientosAprobados = resultados;
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
    }
}