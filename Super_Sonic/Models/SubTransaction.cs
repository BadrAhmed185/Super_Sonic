using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Super_Sonic.Models
{
    public class SubTransaction
    {
        [Key]
        public int ID { get; set; }

     //   [Required(ErrorMessage = "المبلغ مطلوب")]
       // [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Required]
        public string PartnerId { get; set; }
        public Partner Partner { get; set; }

        [Required]
        public int TransactionId { get; set; }
        public Transaction Transaction { get; set; }
    }
}
