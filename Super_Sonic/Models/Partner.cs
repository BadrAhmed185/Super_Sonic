using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Super_Sonic.Models
{
    public class Partner
    {
        [Key]
        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [Display(Name = "الرقم القومي")]
        public string NationalId { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم")]
        public string Name { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        [Display(Name = "العنوان")]
        public string Address { get; set; }

        [Display(Name = "الوصف")]
        public string? Description { get; set; } 

        [Display(Name = "المهنة")]
        public string? Profession { get; set; }

        [Required(ErrorMessage = "رأس المال مطلوب")]
        [Display(Name = "رأس المال")]
        public decimal Capital { get; set; }

       // [Required(ErrorMessage = "النقدية مطلوبة")]
       // [Display(Name = "النقدية")]
        public decimal Cash { get; set; } = 0 ;

       // [Required(ErrorMessage = "رأس المال العامل مطلوب")]
       // [Display(Name = "رأس المال العامل")]
        public decimal WorkingCapital { get; set; } = 0;

        // [Required(ErrorMessage = "عدد المخزون النشط مطلوب")]
       // [Display(Name = "عدد المخزون النشط")]
        public int NumberOfActiveInventory { get; set; } = 0;

        public ICollection<PartnerProduct>? PartnerProducts { get; set; }
        public ICollection<SubTransaction>? SubTransactions { get; set; }
        public ICollection<PartnerLogForInvest_Drawal>? PartnerLogForInvest_Drawals { get; set; }
    }
}
