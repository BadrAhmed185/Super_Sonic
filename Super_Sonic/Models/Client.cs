using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Super_Sonic.Models
{
    public class Client
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
        public string ? Description { get; set; }

        [Display(Name = "المهنة")]
        public string? Profession { get; set; }

        [Display(Name = "حد السحب")]
        public decimal? CreditLimit { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}
