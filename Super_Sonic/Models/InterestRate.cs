using System.ComponentModel.DataAnnotations;

namespace Super_Sonic.Models
{
    public class InterestRate
    {
        [Key]
        public int ID { get; set; }

        public decimal Rate { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
