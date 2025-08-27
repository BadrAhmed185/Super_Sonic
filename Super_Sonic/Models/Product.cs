using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Super_Sonic.Models
{
    public class Product
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم")]
        public string Name { get; set; }

        [Required(ErrorMessage = "التكلفة مطلوبة")]
        [Display(Name = "التكلفة")]
        public decimal Cost { get; set; }

        [Required(ErrorMessage = "سعر البيع نقداً مطلوب")]
        [Display(Name = "سعر البيع نقداً")]
        public decimal CashPrice { get; set; }

        // [Required(ErrorMessage = "المبلغ المدفوع نقداً مطلوب")]
        [Display(Name = "المبلغ المدفوع نقداً")]
        public decimal CashPaid { get; set; } = 0;

       // [Required(ErrorMessage = "السعر الإجمالي مطلوب")]
        [Display(Name = "السعر الإجمالي")]
        public decimal TotalPrice { get; set; } = 0;

        [Required(ErrorMessage = "المدة مطلوبة")]
        [Display(Name = "المدة")]
        public int Duration { get; set; }

        [Display(Name = "القسط الشهري")]
        public decimal Installment { get; set; }

        [Display(Name = "الأشهر المتبقية")]
        public int? RemainingMonths { get; set; }

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

       // [Required(ErrorMessage = "التاريخ مطلوب")]
        [Display(Name = "التاريخ")]
        public DateTime Date { get; set; } = DateTime.Now;

        public string ClientId { get; set; }
        public Client Client { get; set; }

        public ICollection<Transaction> Transactions { get; set; }
        public ICollection<PartnerProduct> PartnerProducts { get; set; }
    }
}
