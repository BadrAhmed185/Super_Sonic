using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Super_Sonic.Common;
using Super_Sonic.DTOs;
using Super_Sonic.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Super_Sonic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartnersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartnersController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Create Partner
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePartnerDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ServiceResult<object>.Failure(errors));
                }

                if (await _context.Partners.AnyAsync(p => p.NationalId == dto.NationalId))
                    return Conflict(ServiceResult<object>.Failure("⚠ الرقم القومي موجود بالفعل."));

                var partner = new Partner
                {
                    NationalId = dto.NationalId,
                    Name = dto.Name,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    Description = dto.Description,
                    Profession = dto.Profession,
                    Capital = dto.Capital,
                    Cash = dto.Capital // Initialize Cash with Capital
                };

                _context.Partners.Add(partner);
                await _context.SaveChangesAsync();

                return Ok(ServiceResult<CreatePartnerDto>.Success(dto, "✅ تم إضافة الشريك بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<object>.Failure(new List<string>
                {
                    "❌ حدث خطأ أثناء إضافة الشريك.", ex.Message
                }));
            }
        }

        // ✅ Create Partners in Bulk
        [HttpPost("bulk")]
        public async Task<IActionResult> CreateBulk([FromBody] List<CreatePartnerDto> partnersDto)
        {
            try
            {
                if (partnersDto == null || partnersDto.Count == 0)
                    return BadRequest(ServiceResult<object>.Failure("⚠ قائمة الشركاء فارغة."));

                // Validate each partner DTO
                var validationErrors = new List<object>();
                for (int i = 0; i < partnersDto.Count; i++)
                {
                    var context = new ValidationContext(partnersDto[i], null, null);
                    var results = new List<ValidationResult>();
                    if (!Validator.TryValidateObject(partnersDto[i], context, results, true))
                    {
                        validationErrors.Add(new
                        {
                            Index = i,
                            Errors = results.Select(r => r.ErrorMessage)
                        });
                    }
                }

                if (validationErrors.Any())
                    return BadRequest(ServiceResult<object>.Failure("⚠ حدثت أخطاء في البيانات المدخلة."));

                // Check for duplicate NationalId
                var nationalIds = partnersDto.Select(p => p.NationalId).ToList();
                var existingIds = await _context.Partners
                    .Where(p => nationalIds.Contains(p.NationalId))
                    .Select(p => p.NationalId)
                    .ToListAsync();

                if (existingIds.Any())
                    return Conflict(ServiceResult<object>.Failure($"⚠ بعض الأرقام القومية موجودة بالفعل: {string.Join(",", existingIds)}"));

                // Create entities
                var partners = partnersDto.Select(dto => new Partner
                {
                    NationalId = dto.NationalId,
                    Name = dto.Name,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    Description = dto.Description,
                    Profession = dto.Profession,
                    Capital = dto.Capital,
                    Cash = dto.Capital
                }).ToList();

                await _context.Partners.AddRangeAsync(partners);
                await _context.SaveChangesAsync();

                return Ok(ServiceResult<int>.Success(partners.Count, "✅ تم إضافة الشركاء بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<object>.Failure(new List<string>
                {
                    "❌ حدث خطأ أثناء إضافة الشركاء.", ex.Message
                }));
            }
        }

        // ✅ Get All Partners
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var partners = await _context.Partners
                    .AsNoTracking()
                    .Select(p => new PartnerDto
                    {
                        NationalId = p.NationalId,
                        Name = p.Name,
                        PhoneNumber = p.PhoneNumber,
                        Address = p.Address,
                        Description = p.Description,
                        Profession = p.Profession,
                        Capital = p.Capital,
                        Cash = p.Cash,
                        WorkingCapital = p.WorkingCapital,
                        NumberOfActiveInventory = p.NumberOfActiveInventory
                    })
                    .ToListAsync();

                if (!partners.Any())
                    return NotFound(ServiceResult<List<PartnerDto>>.Failure("⚠ لا يوجد شركاء مسجلين."));

                return Ok(ServiceResult<List<PartnerDto>>.Success(partners, "✅ تم جلب بيانات الشركاء بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<List<PartnerDto>>.Failure(new List<string>
                {
                    "❌ حدث خطأ أثناء جلب بيانات الشركاء.", ex.Message
                }));
            }
        }

        // ✅ Get Partner by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var partner = await _context.Partners
                    .AsNoTracking()
                    .Where(p => p.NationalId == id)
                    .Select(p => new PartnerDto
                    {
                        NationalId = p.NationalId,
                        Name = p.Name,
                        PhoneNumber = p.PhoneNumber,
                        Address = p.Address,
                        Description = p.Description,
                        Profession = p.Profession,
                        Capital = p.Capital,
                        Cash = p.Cash,
                        WorkingCapital = p.WorkingCapital,
                        NumberOfActiveInventory = p.NumberOfActiveInventory
                    })
                    .FirstOrDefaultAsync();

                if (partner == null)
                    return NotFound(ServiceResult<object>.Failure("⚠ الشريك غير موجود."));

                return Ok(ServiceResult<PartnerDto>.Success(partner, "✅ تم جلب بيانات الشريك بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<object>.Failure(new List<string>
                {
                    "❌ حدث خطأ أثناء جلب بيانات الشريك.", ex.Message
                }));
            }
        }

        // ✅ Update Partner
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PartnerDto dto)
        {
            try
            {
                if (id != dto.NationalId)
                    return BadRequest(ServiceResult<object>.Failure("❌ الرقم القومي المرسل لا يتطابق مع بيانات الشريك."));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ServiceResult<object>.Failure(errors));
                }

                var existingPartner = await _context.Partners.FindAsync(id);
                if (existingPartner == null)
                    return NotFound(ServiceResult<object>.Failure("⚠ الشريك غير موجود."));

                existingPartner.Name = dto.Name;
                existingPartner.PhoneNumber = dto.PhoneNumber;
                existingPartner.Address = dto.Address;
                existingPartner.Description = dto.Description;
                existingPartner.Profession = dto.Profession;
                existingPartner.Capital = dto.Capital;
                existingPartner.Cash = dto.Cash;
                existingPartner.WorkingCapital = dto.WorkingCapital;
                existingPartner.NumberOfActiveInventory = dto.NumberOfActiveInventory;

                await _context.SaveChangesAsync();

                return Ok(ServiceResult<PartnerDto>.Success(dto, "✅ تم تحديث بيانات الشريك بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<object>.Failure(new List<string>
                {
                    "❌ حدث خطأ أثناء تحديث بيانات الشريك.", ex.Message
                }));
            }
        }

        // ✅ Deposit
        [HttpPatch("{id}/deposit")]
        public async Task<IActionResult> Deposit([FromRoute] string id, [FromQuery] decimal amount)
        {
            try
            {
                if (id == null)
                    return BadRequest(ServiceResult<object>.Failure("❌ الرقم القومي غير صحيح."));

                if (amount <= 0)
                    return BadRequest(ServiceResult<object>.Failure("❌ المبلغ المودع يجب أن يكون أكبر من صفر."));

                var partner = await _context.Partners.FindAsync(id);
                if (partner == null)
                    return NotFound(ServiceResult<object>.Failure("⚠ الشريك غير موجود."));

                var logEntry = new PartnerLogForInvest_Drawal
                {
                    IsDebit = true,
                    Date = DateTime.UtcNow,
                    Amount = amount,
                    PartnerId = id
                };
                _context.PartnerLogForInvest_Drawals.Add(logEntry);

                partner.Cash += amount;
                partner.Capital += amount;

                _context.Partners.Update(partner);
                await _context.SaveChangesAsync();

                return Ok(ServiceResult<object>.Success(new { partner.NationalId, newBalance = partner.Cash }, "✅ تم إيداع المبلغ بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<object>.Failure(new List<string>
                {
                    "❌ حدث خطأ أثناء الإيداع.", ex.Message
                }));
            }
        }

        // ✅ Withdraw
        [HttpPatch("{id}/withdraw")]
        public async Task<IActionResult> WithDraw([FromRoute] string id, [FromQuery] decimal amount)
        {
            try
            {
                if (id == null)
                    return BadRequest(ServiceResult<object>.Failure("❌ الرقم القومي غير صحيح."));

                if (amount <= 0)
                    return BadRequest(ServiceResult<object>.Failure("❌ المبلغ المسحوب يجب أن يكون أكبر من صفر."));

                var partner = await _context.Partners.FindAsync(id);
                if (partner == null)
                    return NotFound(ServiceResult<object>.Failure("⚠ الشريك غير موجود."));

                if (partner.Cash < amount)
                    return BadRequest(ServiceResult<object>.Failure($"❌ لا يوجد رصيد كافي للسحب. الرصيد المتاح {partner.Cash}"));

                var logEntry = new PartnerLogForInvest_Drawal
                {
                    IsDebit = false,
                    Date = DateTime.UtcNow,
                    Amount = amount,
                    PartnerId = id
                };
                _context.PartnerLogForInvest_Drawals.Add(logEntry);

                partner.Cash -= amount;
                partner.Capital -= amount;

                _context.Partners.Update(partner);
                await _context.SaveChangesAsync();

                return Ok(ServiceResult<object>.Success(new { partner.NationalId, remainingCash = partner.Cash }, "✅ تم سحب المبلغ بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<object>.Failure(new List<string>
                {
                    "❌ حدث خطأ أثناء السحب.", ex.Message
                }));
            }
        }

        // ✅ Delete Partner
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var partner = await _context.Partners.FindAsync(id);
                if (partner == null)
                    return NotFound(ServiceResult<object>.Failure("⚠ الشريك غير موجود."));

                _context.Partners.Remove(partner);
                await _context.SaveChangesAsync();

                return Ok(ServiceResult<object>.Success("✅ تم حذف الشريك بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<object>.Failure(new List<string>
                {
                    "❌ حدث خطأ أثناء حذف الشريك.", ex.Message
                }));
            }
        }
    }
}
