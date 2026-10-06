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
    public class OrdenesCompraModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrdenesCompraModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<OrdenesCompraCabecera> ListaOrdenesCompra { get; set; } = new();

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
        public string? EstadoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        public SelectList SelectAnios { get; set; } = null!;
        public SelectList SelectMeses { get; set; } = null!;

        public async Task OnGetAsync()
        {
            CargarCombosFecha();

            // Usamos la entidad directa e incluimos sólo la relación de Detalles
            var query = _context.OrdenesCompraCabecera
                .Include(o => o.Detalles)
                .AsNoTracking()
                .AsQueryable();

            // 1. Filtro por Año
            if (Anio > 0)
            {
                query = query.Where(o => o.FechaEmision.Year == Anio);
            }

            // 2. Filtro por Mes (0 = Todos los meses)
            if (Mes > 0)
            {
                query = query.Where(o => o.FechaEmision.Month == Mes);
            }

            // 3. Filtro por Estado
            if (!string.IsNullOrWhiteSpace(EstadoFiltro))
            {
                query = query.Where(o => o.Estado == EstadoFiltro);
            }

            // 4. Búsqueda por Código
            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                string t = Busqueda.Trim().ToLower();
                query = query.Where(o => o.Codigo.ToLower().Contains(t));
            }

            ListaOrdenesCompra = await query
                .OrderByDescending(o => o.FechaEmision)
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