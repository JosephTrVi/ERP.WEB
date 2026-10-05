using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ERP.WEB.Models.Ventas
{
    [Table("Clientes", Schema = "Ventas")]
    public class Cliente
    {
        [Key]
        public int ClienteID { get; set; }

        [Required]
        [StringLength(15)]
        public string TipoCliente { get; set; } = "Nacional"; // Nacional, Extranjero

        [Required]
        [StringLength(10)]
        public string TipoDocumento { get; set; } = "RUC"; // RUC, DNI, TaxID, Pasaporte

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

        [Column(TypeName = "decimal(12, 4)")]
        public decimal LineaCredito { get; set; } = 0.0000m;

        [Required]
        [StringLength(3)]
        public string MonedaCredito { get; set; } = "PEN";

        [StringLength(50)]
        public string? FormaPagoHabitual { get; set; } = "Contado";

        public bool EsAgenteRetencion { get; set; } = false;

        public bool EsBuenContribuyente { get; set; } = false;

        [StringLength(20)]
        public string? EstadoSunat { get; set; } = "HABIDO";

        public bool Estado { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}