using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Super_Sonic.DTOs;
using Super_Sonic.Models;
using Super_Sonic.DTOs;
using Super_Sonic.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace Super_Sonic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { message = "⚠ حدثت أخطاء في البيانات المدخلة.", errors });
            }

            if (await _context.Partners.AnyAsync(p => p.NationalId == dto.NationalId))
                return Conflict(new { message = "⚠ الرقم القومي موجود بالفعل." });

            var partner = new Partner
            {
                NationalId = dto.NationalId,
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Description = dto.Description,
                Profession = dto.Profession,
                Capital = dto.Capital
            };

            partner.Cash = partner.Capital; // Initialize Cash with Capital

            _context.Partners.Add(partner);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = partner.NationalId }, dto);
        }


        [HttpPost("bulk")]
        public async Task<IActionResult> CreateBulk([FromBody] List<CreatePartnerDto> partnersDto)
        {
            if (partnersDto == null || partnersDto.Count == 0)
                return BadRequest(new { message = "⚠ قائمة الشركاء فارغة." });

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
                return BadRequest(new { message = "⚠ حدثت أخطاء في البيانات المدخلة.", errors = validationErrors });

            // Check for duplicate NationalId in DB or within the incoming list
            var nationalIds = partnersDto.Select(p => p.NationalId).ToList();

            var existingIds = await _context.Partners
                .Where(p => nationalIds.Contains(p.NationalId))
                .Select(p => p.NationalId)
                .ToListAsync();

            if (existingIds.Any())
                return Conflict(new { message = "⚠ بعض الأرقام القومية موجودة بالفعل.", existing = existingIds });

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
                Cash = dto.Capital // initialize cash
            }).ToList();

            // Save to DB
            await _context.Partners.AddRangeAsync(partners);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ تم إضافة الشركاء بنجاح.", count = partners.Count });
        }


        // ✅ Get All Partners
        [HttpGet]
        public async Task<IActionResult> GetAll()
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

            return Ok(partners);
        }

        // ✅ Get Partner by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
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
                return NotFound(new { message = "⚠ الشريك غير موجود." });

            return Ok(partner);
        }

        // ✅ Update Partner
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PartnerDto dto)
        {
            if (id != dto.NationalId)
                return BadRequest(new { message = "❌ الرقم القومي المرسل لا يتطابق مع بيانات الشريك." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { message = "⚠ حدثت أخطاء في البيانات المدخلة.", errors });
            }

            var existingPartner = await _context.Partners.FindAsync(id);
            if (existingPartner == null)
                return NotFound(new { message = "⚠ الشريك غير موجود." });

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

            return Ok(new { message = "✅ تم تحديث بيانات الشريك بنجاح.", partner = dto });
        }

        [HttpPatch("{id}/deposit")]
        public async Task<IActionResult> Deposit([FromRoute] string id, [FromQuery] decimal amount)
        {
            if (id == null)
                return BadRequest(new { message = "❌ الرقم القومي غير صحيح." });

            if (amount <= 0)
                return BadRequest(new { message = "❌ المبلغ المودع يجب أن يكون أكبر من صفر." });

            var partner = await _context.Partners.FindAsync(id);
            if (partner == null)
                return NotFound(new { message = "⚠ الشريك غير موجود." });

            // Create a log entry for the deposit
            var logEntry = new PartnerLogForInvest_Drawal
            {
                IsDebit = true,  // true = deposit
                Date = DateTime.UtcNow,
                Amount = amount,
                PartnerId = id
            };
            _context.PartnerLogForInvest_Drawals.Add(logEntry);

            // Update the partner's cash balance
            partner.Cash += amount;
            partner.Capital += amount; // update Capital as well

            _context.Partners.Update(partner);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ تم إيداع المبلغ بنجاح.", newBalance = Math.Round(partner.Cash, 2) });
        }



        [HttpPatch("{id}")]
        public async Task<IActionResult> WithDraw([FromRoute] string id , [FromQuery] decimal amount)
        {
            if(id == null)
                return BadRequest(new { message = "❌ الرقم القومي غير صحيح." });


            if (amount <= 0)
                return BadRequest(new { message = "❌ المبلغ المسحوب يجب أن يكون أكبر من صفر." });

            var partner = await _context.Partners.FindAsync(id);

            if (partner == null)
                return NotFound(new { message = "⚠ الشريك غير موجود." });

            if (partner.Cash < amount)
                return BadRequest(new { message = $"أجمالي الرصيد المتاح {Math.Round(partner.Cash, 2)} \n ❌ لا يوجد رصيد كافي للسحب." });
            // Create a log entry for the withdrawal
            var logEntry = new PartnerLogForInvest_Drawal
            {
                IsDebit = false,
                Date = DateTime.UtcNow,
                Amount = amount,
                PartnerId = id
            };
            _context.PartnerLogForInvest_Drawals.Add(logEntry);
            // Update the partner's cash balance
            partner.Cash -= amount;
            partner.Capital -= amount; // update Capital as well

            _context.Partners.Update(partner);
            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ تم سحب المبلغ بنجاح.", remainingCash = Math.Round(partner.Cash, 2) });
        }

        // ✅ Delete Partner
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var partner = await _context.Partners.FindAsync(id);
            if (partner == null)
                return NotFound(new { message = "⚠ الشريك غير موجود." });

            _context.Partners.Remove(partner);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ تم حذف الشريك بنجاح." });
        }
    }
}
