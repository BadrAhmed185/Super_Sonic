using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Super_Sonic.DTOs;
using Super_Sonic.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Super_Sonic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration config;

        public AuthController(UserManager<ApplicationUser> userManager, AppDbContext context, IConfiguration config)
        {
            this._userManager = userManager;
            this._context = context;
            this.config = config;
        }

        [HttpPost("{SignUp}")]
        public async Task<IActionResult> SignUp([FromBody] SignUpDto signUpDto)
        {

            using var transactionScope = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ البيانات غير صالحة",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                // Check if the user already exists
                var existingUser = await _userManager.FindByNameAsync(signUpDto.UserName);
                if (existingUser != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌إسم المستخدم موحود بالفعل"
                    });

                }

                var existingId = await _userManager.Users
                .FirstOrDefaultAsync(u => u.NationalId == signUpDto.NationalId);

                if (existingId != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ الرقم القومي مسجل بالفعل"
                    });

                }



                var user = new ApplicationUser
                {
                    UserName = signUpDto.UserName,
                    NationalId = signUpDto.NationalId,
                    PhoneNumber = signUpDto.PhoneNumber,
                    Role = signUpDto.Role
                };

                var result = await _userManager.CreateAsync(user, signUpDto.Password);

                if (result.Succeeded)
                {
                    // إسناد الدور إن وجد
                    if (!string.IsNullOrEmpty(signUpDto.Role))
                    {
                        var roleResult = await _userManager.AddToRoleAsync(user, signUpDto.Role);
                        if (!roleResult.Succeeded)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "❌ فشل في إسناد الدور للمستخدم",
                                errors = roleResult.Errors.Select(e => e.Description)
                            });
                        }
                    }

                    await transactionScope.CommitAsync();
                    return Ok(new
                    {
                        success = true,
                        message = "✅ تم تسجيل المستخدم بنجاح"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ فشل في تسجيل المستخدم",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }
            }
            catch (Exception ex)
            {
                await transactionScope.RollbackAsync();
                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ خطأ داخلي في الخادم",
                    details = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> LogIn([FromBody] LogInDto logInDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ البيانات غير صالحة",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var user = await _userManager.FindByNameAsync(logInDto.UserName);

                if (user == null || !await _userManager.CheckPasswordAsync(user, logInDto.Password))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "❌ اسم المستخدم أو كلمة المرور غير صحيحة"
                    });
                }

                // Claims
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, logInDto.UserName),
            new Claim("Role", user.Role ?? "User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

                if (!string.IsNullOrEmpty(user.NationalId))
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, user.NationalId));
                }

                // Security Key
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:SecretKey"]));
                var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                //var tokenRepresentation = new JwtSecurityToken(
                //    issuer: config["JWT:ValidIssuer"],
                //    audience: config["JWT:ValidConsumer"],
                //    claims: claims,
                //    expires: DateTime.Now.AddHours(1),
                //    signingCredentials: signingCredentials
                //); 
                
                var tokenRepresentation = new JwtSecurityToken(
                    issuer: config["JWT:ValidIssuer"],
                    claims: claims,
                    expires: DateTime.Now.AddHours(1),
                    signingCredentials: signingCredentials
                );

                return Ok(new
                {
                    success = true,
                    message = "✅ تم تسجيل الدخول بنجاح",
                    token = new JwtSecurityTokenHandler().WriteToken(tokenRepresentation),
                    expiration = tokenRepresentation.ValidTo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ خطأ داخلي في الخادم",
                    details = ex.Message
                });
            }
        }
    }


    }

