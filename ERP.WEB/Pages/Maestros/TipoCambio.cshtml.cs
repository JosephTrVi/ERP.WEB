using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Maestros;
using ERP.WEB.Services;

namespace ERP.WEB.Pages.Maestros
{
    [Authorize]
    public class TipoCambioModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SunatTcService _sunatService;

        public TipoCambioModel(ApplicationDbContext context, SunatTcService sunatService)
        {
            _context = context;
            _sunatService = sunatService;
        }

        public List<TipoCambio> HistorialTC { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int Anio { get; set; } = DateTime.Now.Year;

        [BindProperty(SupportsGet = true)]
        public int Mes { get; set; } = DateTime.Now.Month;

        [BindProperty(SupportsGet = true)]
        public DateTime FechaConsulta { get; set; } = DateTime.Now.Date;

        [BindProperty]
        public TipoCambio NuevoTC { get; set; } = new TipoCambio
        {
            Fecha = DateTime.Now.Date
        };

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            // Cargar el historial del mes y año seleccionados
            HistorialTC = await _context.TipoCambio
                .Where(t => t.Fecha.Year == Anio && t.Fecha.Month == Mes)
                .OrderByDescending(t => t.Fecha)
                .AsNoTracking()
                .ToListAsync();
        }

        // HANDLER PARA CONSULTAR UN DÍA ESPECÍFICO
        public async Task<IActionResult> OnPostConsultarDiaAsync()
        {
            var tc = await _sunatService.ObtenerTcDiaAsync(FechaConsulta);

            if (tc != null)
            {
                MensajeExito = $"Tipo de cambio registrado para el {tc.Fecha:dd/MM/yyyy}: Compra S/ {tc.PrecioCompra:N4} | Venta S/ {tc.PrecioVenta:N4}.";
            }
            else
            {
                MensajeError = $"No se encontró registro para el {FechaConsulta:dd/MM/yyyy}. Utiliza el botón de Registro Manual.";
            }

            return RedirectToPage(new { Anio = FechaConsulta.Year, Mes = FechaConsulta.Month, FechaConsulta = FechaConsulta.ToString("yyyy-MM-dd") });
        }

        // HANDLER PARA REGISTRO Y EDICIÓN MANUAL
        public async Task<IActionResult> OnPostGuardarManualAsync()
        {
            if (NuevoTC.PrecioCompra <= 0 || NuevoTC.PrecioVenta <= 0)
            {
                MensajeError = "Los precios de compra y venta deben ser mayores a 0.";
                return RedirectToPage(new { Anio, Mes });
            }

            var existente = await _context.TipoCambio.FirstOrDefaultAsync(t => t.Fecha == NuevoTC.Fecha.Date && t.MonedaOrigen == "USD");

            if (existente == null)
            {
                NuevoTC.Fecha = NuevoTC.Fecha.Date;
                NuevoTC.MonedaOrigen = "USD";
                NuevoTC.MonedaDestino = "PEN";
                NuevoTC.OrigenData = "Manual";
                NuevoTC.FechaRegistro = DateTime.Now;

                _context.TipoCambio.Add(NuevoTC);
            }
            else
            {
                existente.PrecioCompra = NuevoTC.PrecioCompra;
                existente.PrecioVenta = NuevoTC.PrecioVenta;
                existente.OrigenData = "Manual";
                _context.TipoCambio.Update(existente);
            }

            await _context.SaveChangesAsync();
            MensajeExito = $"Tipo de cambio para la fecha {NuevoTC.Fecha:dd/MM/yyyy} guardado correctamente.";

            return RedirectToPage(new { Anio = NuevoTC.Fecha.Year, Mes = NuevoTC.Fecha.Month });
        }
    }
}