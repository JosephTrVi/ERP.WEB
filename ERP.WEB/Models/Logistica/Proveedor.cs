using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Logistica
{
    [Table("Proveedores", Schema = "Compras")]
    public class Proveedor
    {
        [Key]
        public int ProveedorID { get; set; }

        [Required]
        [StringLength(15)]
        public string TipoProveedor { get; set; } = "Nacional"; // Nacional, Extranjero

        [Required]
        [StringLength(10)]
        public string TipoDocumento { get; set; } = "RUC";

        [Required]
        [StringLength(20)]
        public string NumDocumento { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string RazonSocial { get; set; } = string.Empty;

        [StringLength(200)]
        public string? NombreComercial { get; set; }

        [Required]
        [StringLength(3)]
        public string PaisCodigo { get; set; } = "PER";

        [StringLength(6)]
        public string? UbigeoID { get; set; }

        [Required]
        [StringLength(300)]
        public string Direccion { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CiudadEstadoExtranjero { get; set; }

        [StringLength(15)]
        public string? CodigoPostal { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? EmailContacto { get; set; }

        [StringLength(100)]
        public string? ContactoNombre { get; set; }

        public bool EsAgenteRetencion { get; set; } = false;

        public bool EsAgentePercepcion { get; set; } = false;

        public bool EsBuenContribuyente { get; set; } = false;

        [StringLength(20)]
        public string? EstadoSunat { get; set; } = "HABIDO";

        [Required]
        [StringLength(3)]
        public string MonedaHabitual { get; set; } = "PEN";

        [StringLength(50)]
        public string? FormaPagoHabitual { get; set; } = "Crédito 30 días";

        public bool Estado { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}