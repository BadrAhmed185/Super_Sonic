using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace Super_Sonic.Models
{
    public class ApplicationUser : IdentityUser
    {

        public string NationalId { get; set; }
        public string? Role { get; set; } 


    }
}
