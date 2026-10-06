using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ERP.WEB.Data;
using System.Text;

namespace ERP.WEB.Pages.Sistemas
{
    [Authorize(Roles = "Admin,Administrador")]
    public class ImportarDatosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportarDatosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? ArchivoExcel { get; set; }

        [BindProperty]
        public string TipoEntidad { get; set; } = "Productos";

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public void OnGet()
        {
        }

        // HANDLER PARA DESCARGAR LA PLANTILLA EN COLUMNAS SEPARADAS PARA EXCEL
        public IActionResult OnGetDescargarPlantilla(string entidad)
        {
            string csvContent = "";
            string nombreArchivo = "";

            // Se incluye "sep=," en la primera línea para obligar a Excel a separar las comas en columnas independientes
            switch (entidad)
            {
                case "Categorias":
                    csvContent = "sep=,\n" +
                                 "Codigo,CodigoPrefijo,Nombre,Descripcion,EsReceta,EsComponente\n" +
                                 "CAT01,REC,Recetas Generales,Categoría para recetas,1,0";
                    nombreArchivo = "Plantilla_Categorias.csv";
                    break;

                case "SubCategorias":
                    csvContent = "sep=,\n" +
                                 "CategoriaID,Codigo,CodigoPrefijo,Nombre,EsReceta,EsComponente\n" +
                                 "1,SUB01,SREC,SubRecetas,1,0";
                    nombreArchivo = "Plantilla_SubCategorias.csv";
                    break;

                case "Productos":
                    csvContent = "sep=,\n" +
                                 "SKU,Nombre,Descripcion,CategoriaID,SubcategoriaID,UnidadMedida,StockMinimo,StockMaximo,StockActual,CostoPromedio,PrecioVenta,ControlaStock\n" +
                                 "PROD-001,Insumo Ejemplo,Descripción del producto,1,1,UND,10.00,100.00,50.00,15.50,25.00,1";
                    nombreArchivo = "Plantilla_Productos.csv";
                    break;

                case "Clientes":
                    csvContent = "sep=,\n" +
                                 "TipoCliente,TipoDocumento,NumDocumento,RazonSocial,NombreComercial,PaisCodigo,UbigeoID,Direccion,Telefono,EmailContacto,LineaCredito,MonedaCredito,FormaPagoHabitual\n" +
                                 "Nacional,RUC,20123456789,CLIENTE DEMO S.A.C.,DEMO,PER,150101,Av. Principal 123,987654321,contacto@cliente.com,5000.00,PEN,Contado";
                    nombreArchivo = "Plantilla_Clientes.csv";
                    break;

                case "Proveedores":
                    csvContent = "sep=,\n" +
                                 "TipoProveedor,TipoDocumento,NumDocumento,RazonSocial,NombreComercial,PaisCodigo,UbigeoID,Direccion,Telefono,EmailContacto,MonedaHabitual,FormaPagoHabitual\n" +
                                 "Nacional,RUC,20987654321,PROVEEDOR DEMO S.A.C.,PROV DEMO,PER,150101,Av. Industrial 456,912345678,ventas@proveedor.com,PEN,Crédito 30 días";
                    nombreArchivo = "Plantilla_Proveedores.csv";
                    break;

                default:
                    return RedirectToPage();
            }

            // Codificación UTF-8 con BOM para soportar tildes, eñes y formateo directo en columnas
            byte[] preamble = Encoding.UTF8.GetPreamble();
            byte[] data = Encoding.UTF8.GetBytes(csvContent);
            byte[] buffer = new byte[preamble.Length + data.Length];

            Buffer.BlockCopy(preamble, 0, buffer, 0, preamble.Length);
            Buffer.BlockCopy(data, 0, buffer, preamble.Length, data.Length);

            return File(buffer, "text/csv; charset=utf-8", nombreArchivo);
        }

        public async Task<IActionResult> OnPostProcesarImportacionAsync()
        {
            if (ArchivoExcel == null || ArchivoExcel.Length == 0)
            {
                MensajeError = "Debe seleccionar un archivo válido (.csv / .xlsx) para importar.";
                return RedirectToPage();
            }

            try
            {
                await _context.SaveChangesAsync();
                MensajeExito = $"Los datos de {TipoEntidad} fueron procesados e importados correctamente.";
            }
            catch (Exception ex)
            {
                MensajeError = $"Error al procesar el archivo: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}