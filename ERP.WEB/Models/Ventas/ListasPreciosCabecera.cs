using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Ventas
{
    [Table("ListasPreciosCabecera", Schema = "Ventas")]
    public class ListasPreciosCabecera
    {
        [Key]
        public int ListaPrecioID { get; set; }

        public int VendedorID { get; set; }

        public int Anio { get; set; }

        public int Mes { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreLista { get; set; } = string.Empty;

        public bool Estado { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public virtual ICollection<ListasPreciosDetalle> Detalles { get; set; } = new List<ListasPreciosDetalle>();
    }
}