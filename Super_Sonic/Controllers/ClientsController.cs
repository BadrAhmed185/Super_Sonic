
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Super_Sonic.DTOs;
using Super_Sonic.Models;

namespace Super_Sonic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Create Client
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClientDto dto)
        {
         
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = "⚠ حدثت أخطاء في البيانات المدخلة.", errors });
            }

            if (await _context.Clients.AnyAsync(c => c.NationalId == dto.NationalId))
                return Conflict(new { message = "⚠ الرقم القومي موجود بالفعل." });

            var client = new Client
            {
                NationalId = dto.NationalId,
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Description = dto.Description,
                Profession = dto.Profession,
                CreditLimit = dto.CreditLimit
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = client.NationalId }, client);
        }

        // ✅ Get All Clients
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var clients = await _context.Clients
                .AsNoTracking()
                .Select(c => new ClientDto
                {
                    NationalId = c.NationalId,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Address = c.Address,
                    Description = c.Description,
                    Profession = c.Profession,
                    CreditLimit = c.CreditLimit
                })
                .ToListAsync();

            return Ok(clients);
        }

        // ✅ Get Client by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .Where(c => c.NationalId == id)
                .Select(c => new ClientDto
                {
                    NationalId = c.NationalId,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Address = c.Address,
                    Description = c.Description,
                    Profession = c.Profession,
                    CreditLimit = c.CreditLimit
                })
                .FirstOrDefaultAsync();

            if (client == null)
                return NotFound(new { message = "⚠ العميل غير موجود." });

            return Ok(client);
        }

        // ✅ Update Client
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ClientDto dto)
        {
            if (id != dto.NationalId)
                return BadRequest(new { message = "❌ الرقم القومي المرسل لا يتطابق مع بيانات العميل." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = "⚠ حدثت أخطاء في البيانات المدخلة.", errors });
            }

            var existingClient = await _context.Clients.FindAsync(id);
            if (existingClient == null)
                return NotFound(new { message = "⚠ العميل غير موجود." });

            existingClient.Name = dto.Name;
            existingClient.PhoneNumber = dto.PhoneNumber;
            existingClient.Address = dto.Address;
            existingClient.Description = dto.Description;
            existingClient.Profession = dto.Profession;
            existingClient.CreditLimit = dto.CreditLimit;

            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ تم تحديث بيانات العميل بنجاح.", client = dto });
        }

        // ✅ Delete Client
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound(new { message = "⚠ العميل غير موجود." });

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ تم حذف العميل بنجاح." });
        }
    }
}
