using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Maestros;

namespace ERP.WEB.Services
{
    public class SunatTcService
    {
        private readonly ApplicationDbContext _context;

        public SunatTcService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el Tipo de Cambio guardado en la Base de Datos para una fecha específica.
        /// </summary>
        public async Task<TipoCambio?> ObtenerTcDiaAsync(DateTime fecha)
        {
            DateTime fechaSinHora = fecha.Date;

            return await _context.TipoCambio
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Fecha == fechaSinHora && t.MonedaOrigen == "USD");
        }
    }
}