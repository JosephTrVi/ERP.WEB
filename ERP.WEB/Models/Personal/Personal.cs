using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Personal
{
    [Table("Personal", Schema = "Personal")]
    public class Personal
    {
        [Key]
        public int PersonalID { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Email { get; set; }

        public bool Estado { get; set; } = true;
    }
}