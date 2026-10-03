using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Inventario;
using ERP.WEB.Models.Logistica;
using System.Text.Json;

namespace ERP.WEB.Pages.Almacen
{
    [Authorize]
    public class RecepcionOCModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RecepcionOCModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public OrdenesCompraCabecera OrdenCompra { get; set; } = null!;
        public SelectList SelectAlmacenes { get; set; } = null!;

        [BindProperty]
        public IngresosAlmacenCabecera InputIngreso { get; set; } = new();

        [BindProperty]
        public string ItemsIngresoJson { get; set; } = "[]";

        // Diccionario para almacenar lo recibido previamente en otras entregas parciales
        public Dictionary<int, decimal> CantidadesAtendidasPrevias { get; set; } = new();

        [TempData]
        public string? MensajeError { get; set; }

        [TempData]
        public string? MensajeExito { get; set; }

        public async Task<IActionResult> OnGetAsync(int ordenCompraId)
        {
            var oc = await _context.OrdenesCompraCabecera
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrdenCompraID == ordenCompraId);

            if (oc == null)
            {
                MensajeError = "La Orden de Compra especificada no existe.";
                return RedirectToPage("/Compras/OrdenesCompra");
            }

            OrdenCompra = oc;

            // Calcular recepciones parciales anteriores
            var ingresosPrevios = await _context.IngresosAlmacenDetalle
                .Where(id => oc.Detalles.Select(d => d.OrdenCompraDetalleID).Contains(id.OrdenCompraDetalleID))
                .GroupBy(id => id.OrdenCompraDetalleID)
                .Select(g => new { DetalleID = g.Key, TotalRecibido = g.Sum(x => x.CantidadRecibida) })
                .ToListAsync();

            CantidadesAtendidasPrevias = ingresosPrevios.ToDictionary(x => x.DetalleID, x => x.TotalRecibido);

            // Cargar Almacenes
            var almacenes = await _context.Almacenes.Where(a => a.Estado).AsNoTracking().ToListAsync();
            SelectAlmacenes = new SelectList(almacenes, "AlmacenID", "Nombre");

            // Correlativo de Nota de Ingreso (ej. NI-2026-0001)
            int anioActual = DateTime.Now.Year;
            int correlativo = await _context.IngresosAlmacenCabecera
                .Where(i => i.FechaIngreso.Year == anioActual)
                .CountAsync() + 1;

            InputIngreso.Codigo = $"NI-{anioActual}-{correlativo.ToString("D4")}";
            InputIngreso.OrdenCompraID = oc.OrdenCompraID;

            return Page();
        }

        public async Task<IActionResult> OnPostGuardarIngresoAsync()
        {
            if (string.IsNullOrWhiteSpace(ItemsIngresoJson) || ItemsIngresoJson == "[]")
            {
                MensajeError = "Debe especificar las cantidades a ingresar.";
                return RedirectToPage(new { ordenCompraId = InputIngreso.OrdenCompraID });
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var recepcionItems = JsonSerializer.Deserialize<List<ItemRecepcionDTO>>(ItemsIngresoJson, options);

            if (recepcionItems == null || !recepcionItems.Any(i => i.CantidadIngresar > 0))
            {
                MensajeError = "Debe ingresar una cantidad mayor a 0 en al menos un ítem.";
                return RedirectToPage(new { ordenCompraId = InputIngreso.OrdenCompraID });
            }

            // 1. Guardar Cabecera de Ingreso a Almacén
            InputIngreso.FechaIngreso = DateTime.Now;
            InputIngreso.UsuarioRegistroID = 1;
            _context.IngresosAlmacenCabecera.Add(InputIngreso);
            await _context.SaveChangesAsync();

            // 2. Procesar Detalle de Ingreso e Incrementar Stock en Almacén
            foreach (var item in recepcionItems.Where(i => i.CantidadIngresar > 0))
            {
                _context.IngresosAlmacenDetalle.Add(new IngresosAlmacenDetalle
                {
                    IngresoID = InputIngreso.IngresoID,
                    OrdenCompraDetalleID = item.OrdenCompraDetalleID,
                    ProductoID = item.ProductoID > 0 ? item.ProductoID : null,
                    CantidadRecibida = item.CantidadIngresar
                });

                // Aumentar stock físico si el producto controla inventario
                if (item.ProductoID > 0)
                {
                    var prod = await _context.Productos.FindAsync(item.ProductoID);
                    if (prod != null && prod.ControlaStock)
                    {
                        prod.StockActual += item.CantidadIngresar;
                        _context.Productos.Update(prod);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // 3. Evaluar si la OC quedó Atendida al 100% o Pendiente de Atención
            var ocObj = await _context.OrdenesCompraCabecera
                .Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.OrdenCompraID == InputIngreso.OrdenCompraID);

            if (ocObj != null)
            {
                var totalesIngresados = await _context.IngresosAlmacenDetalle
                    .Where(id => ocObj.Detalles.Select(d => d.OrdenCompraDetalleID).Contains(id.OrdenCompraDetalleID))
                    .GroupBy(id => id.OrdenCompraDetalleID)
                    .Select(g => new { DetalleID = g.Key, TotalRecibido = g.Sum(x => x.CantidadRecibida) })
                    .ToDictionaryAsync(x => x.DetalleID, x => x.TotalRecibido);

                bool estaCompleta = true;
                foreach (var det in ocObj.Detalles)
                {
                    decimal acumulado = totalesIngresados.ContainsKey(det.OrdenCompraDetalleID) ? totalesIngresados[det.OrdenCompraDetalleID] : 0;
                    if (acumulado < det.Cantidad)
                    {
                        estaCompleta = false;
                        break;
                    }
                }

                // REGLA DE NEGOCIO DE ESTADOS OC:
                ocObj.Estado = estaCompleta ? "Atendido" : "Pendiente de Atención";
                _context.OrdenesCompraCabecera.Update(ocObj);
                await _context.SaveChangesAsync();

                MensajeExito = estaCompleta
                    ? $"Nota de Ingreso {InputIngreso.Codigo} registrada. La Orden de Compra {ocObj.Codigo} ha sido ATENDIDA COMPLETAMENTE."
                    : $"Nota de Ingreso {InputIngreso.Codigo} registrada. La Orden de Compra {ocObj.Codigo} queda PENDIENTE DE ATENCIÓN (Recepción Parcial).";
            }

            return RedirectToPage("/Compras/OrdenesCompra");
        }

        public class ItemRecepcionDTO
        {
            public int OrdenCompraDetalleID { get; set; }
            public int ProductoID { get; set; }
            public decimal CantidadIngresar { get; set; }
        }
    }
}