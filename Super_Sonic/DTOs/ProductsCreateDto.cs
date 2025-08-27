using System.ComponentModel.DataAnnotations;

namespace Super_Sonic.Dtos
{
    public class ProductCreateDto
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم")]
        public string Name { get; set; }

        [Required(ErrorMessage = "التكلفة مطلوبة")]
        [Display(Name = "التكلفة")]
        public decimal Cost { get; set; }

        [Required(ErrorMessage = "سعر البيع نقداً مطلوب")]
        [Display(Name = "سعر البيع نقداً")]
        public decimal CashPrice { get; set; }

        [Display(Name = "المبلغ المدفوع نقداً")]
        public decimal CashPaid { get; set; } = 0;

        [Required(ErrorMessage = "المدة مطلوبة")]
        [Display(Name = "المدة (بالأشهر)")]
        public int Duration { get; set; }

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "العميل مطلوب")]
        [Display(Name = "معرف العميل")]
        public string ClientId { get; set; }
    }
}
