using System.ComponentModel.DataAnnotations;

namespace Super_Sonic.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public class SignUpDto
    {
        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [StringLength(50, ErrorMessage = "اسم المستخدم يجب ألا يتجاوز 50 حرفًا")]
        public string UserName { get; set; }

        //[Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        //[EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
        //public string Email { get; set; }


        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [StringLength(14, ErrorMessage = "اسم المستخدم يجب ألا يتجاوز 14 حرفًا")]
        [MinLength(14, ErrorMessage = "اسم المستخدم يجب  أن يكون 14 حرفًا")]
        public string NationalId { get; set; }


        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
        [StringLength(15, ErrorMessage = "رقم الهاتف يجب ألا يتجاوز 15 رقمًا")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 6 أحرف")]
        public string Password { get; set; }

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "الدور مطلوب")]
        [StringLength(20, ErrorMessage = "الدور يجب ألا يتجاوز 20 حرفًا")]
        public string Role { get; set; }
    }

}
