using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ERP.WEB.Data;
using ERP.WEB.Models.Logistica;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ERP.WEB.Pages.Compras
{
    [Authorize]
    public class ProveedoresModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Proveedor> ListaProveedores { get; set; } = new();

        [BindProperty]
        public Proveedor InputProveedor { get; set; } = new();

        [TempData]
        public string? MensajeExito { get; set; }

        [TempData]
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            ListaProveedores = await _context.Proveedores
                .AsNoTracking()
                .OrderBy(p => p.RazonSocial)
                .ToListAsync();
        }

        public async Task<JsonResult> OnGetBuscarUbigeoAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 3)
                return new JsonResult(new List<object>());

            string t = term.Trim().ToLower();

            // Consulta la tabla Maestros.Ubigeo
            var resultados = await _context.Database
                .SqlQueryRaw<UbigeoResult>("SELECT UbigeoID, Departamento, Provincia, Distrito FROM Maestros.Ubigeo WHERE LOWER(Distrito) LIKE {0} OR LOWER(Provincia) LIKE {0} OR UbigeoID LIKE {0}", $"%{t}%")
                .Take(15)
                .ToListAsync();

            return new JsonResult(resultados);
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            if (string.IsNullOrWhiteSpace(InputProveedor.NumDocumento) || string.IsNullOrWhiteSpace(InputProveedor.RazonSocial))
            {
                MensajeError = "El número de documento y la Razón Social son requeridos.";
                return RedirectToPage();
            }

            if (InputProveedor.ProveedorID == 0)
            {
                bool existe = await _context.Proveedores.AnyAsync(p => p.NumDocumento == InputProveedor.NumDocumento);
                if (existe)
                {
                    MensajeError = "Ya existe un proveedor registrado con el mismo número de documento.";
                    return RedirectToPage();
                }

                _context.Proveedores.Add(InputProveedor);
                MensajeExito = $"Proveedor '{InputProveedor.RazonSocial}' registrado correctamente.";
            }
            else
            {
                _context.Proveedores.Update(InputProveedor);
                MensajeExito = $"Proveedor '{InputProveedor.RazonSocial}' actualizado correctamente.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public class UbigeoResult
        {
            public string UbigeoID { get; set; } = string.Empty;
            public string Departamento { get; set; } = string.Empty;
            public string Provincia { get; set; } = string.Empty;
            public string Distrito { get; set; } = string.Empty;
        }

        // HANDLER AJAX para consultar RUC con OpenRUC
        public async Task<JsonResult> OnGetConsultarRucAsync(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc) || ruc.Length != 11)
            {
                return new JsonResult(new { success = false, message = "El RUC debe tener exactamente 11 dígitos." });
            }

            try
            {
                using var httpClient = new HttpClient();

                // Agregar User-Agent para evitar bloqueos de peticiones automatizadas
                httpClient.DefaultRequestHeaders.Add("User-Agent", "ERP-System-App");

                var response = await httpClient.GetAsync($"https://openruc.com/api/ruc/{ruc}");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<OpenRucResultDTO>();

                    if (data != null && !string.IsNullOrWhiteSpace(data.RazonSocial))
                    {
                        return new JsonResult(new
                        {
                            success = true,
                            razonSocial = data.RazonSocial,
                            direccion = data.Direccion,
                            ubigeoID = data.Ubigeo,
                            estadoSunat = data.Estado,
                            condicionSunat = data.Condicion
                        });
                    }
                }

                return new JsonResult(new { success = false, message = "No se encontraron datos en SUNAT para el RUC ingresado." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error al consultar la API: {ex.Message}" });
            }
        }

        // DTO para deserializar la respuesta de OpenRUC
        public class OpenRucResultDTO
        {
            [JsonPropertyName("ruc")]
            public string Ruc { get; set; } = string.Empty;

            [JsonPropertyName("razon_social")]
            public string RazonSocial { get; set; } = string.Empty;

            [JsonPropertyName("estado")]
            public string Estado { get; set; } = string.Empty;

            [JsonPropertyName("condicion")]
            public string Condicion { get; set; } = string.Empty;

            [JsonPropertyName("direccion")]
            public string Direccion { get; set; } = string.Empty;

            [JsonPropertyName("ubigeo")]
            public string Ubigeo { get; set; } = string.Empty;
        }
    }
}