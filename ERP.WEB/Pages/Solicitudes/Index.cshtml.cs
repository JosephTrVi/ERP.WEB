using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;

namespace ERP.WEB.Pages.Solicitudes
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<RequerimientoCabecera> ListaRequerimientos { get; set; } = new();

        // Filtros de Fecha (Año y Mes)
        [BindProperty(SupportsGet = true)]
        public int Anio { get; set; } = DateTime.Now.Year;

        [BindProperty(SupportsGet = true)]
        public int Mes { get; set; } = DateTime.Now.Month;

        // Filtros opcionales existentes
        [BindProperty(SupportsGet = true)]
        public string? TipoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EstadoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        public SelectList SelectAnios { get; set; } = null!;
        public SelectList SelectMeses { get; set; } = null!;

        public bool EsAdmin { get; set; } = false;

        public async Task OnGetAsync()
        {
            // 1. Cargar desplegables de Año y Mes
            CargarCombosFecha();

            // ID del usuario logueado (Simulado o desde User.Identity / Claims)
            int usuarioActualID = 1;

            // Evaluar si el usuario tiene rol Admin
            EsAdmin = User.IsInRole("Admin") || User.IsInRole("Administrador");

            var query = _context.RequerimientosCabecera
                .Include(r => r.Detalles)
                .AsNoTracking()
                .AsQueryable();

            // REGLA DE NEGOCIO: Si no es Admin, solo ve sus propios requerimientos
            if (!EsAdmin)
            {
                query = query.Where(r => r.SolicitanteID == usuarioActualID);
            }

            // 2. Filtro por Año
            if (Anio > 0)
            {
                query = query.Where(r => r.FechaSolicitud.Year == Anio);
            }

            // 3. Filtro por Mes (0 = Todos los meses)
            if (Mes > 0)
            {
                query = query.Where(r => r.FechaSolicitud.Month == Mes);
            }

            // 4. Filtros opcionales
            if (!string.IsNullOrWhiteSpace(TipoFiltro))
            {
                query = query.Where(r => r.TipoRequerimiento == TipoFiltro);
            }

            if (!string.IsNullOrWhiteSpace(EstadoFiltro))
            {
                query = query.Where(r => r.Estado == EstadoFiltro);
            }

            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string t = Busqueda.Trim().ToLower();
                query = query.Where(r => r.Codigo.ToLower().Contains(t) || (r.Observaciones != null && r.Observaciones.ToLower().Contains(t)));
            }

            ListaRequerimientos = await query
                .OrderByDescending(r => r.FechaSolicitud)
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
    }
}