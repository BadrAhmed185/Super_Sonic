using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Super_Sonic.Models
{
    public class Transaction
    {
        [Key]
        public int ID { get; set; }

       // [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        //[Display(Name = "مدين")]
        public bool IsDebit { get; set; } = true;

      //  [Display(Name = "التاريخ")]
        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsPaid { get; set; } = true;

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public ICollection<SubTransaction> SubTransactions { get; set; }
    }
}
