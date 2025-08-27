using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Super_Sonic.Models
{
    public class PartnerLogForInvest_Drawal
    {

        [Key]
        public int Id { get; set; }

        public bool IsDebit { get; set; } = true;

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        [ForeignKey("Partner")]
        public string PartnerId { get; set; }

        //Nav Properties

        public Partner Partner { get; set; }

    }
}
