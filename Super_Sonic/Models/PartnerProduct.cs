using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Super_Sonic.Models
{
    public class PartnerProduct
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public string PartnerId { get; set; }
        public Partner Partner { get; set; }

        public decimal Percentage { get; set; }
    }
}
