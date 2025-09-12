using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Super_Sonic.Common;
using Super_Sonic.Dtos;
using Super_Sonic.Models;

namespace Super_Sonic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly decimal rate;

        public ProductsController(AppDbContext context)
        {
            _context = context;
            this.rate = _context.InterestRates
                                 .Select(s => s.Rate)
                                 .FirstOrDefault();
        }

        // ✅ Create Product
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ServiceResult<string>.Failure("⚠ البيانات المدخلة غير صالحة."));

            if (dto.Cost <= 0 || dto.CashPrice <= 0 || dto.CashPaid < 0)
                return BadRequest(ServiceResult<string>.Failure("⚠ القيم المالية يجب أن تكون موجبة."));

            try
            {
                var toTalCash = await _context.Partners.SumAsync(p => (decimal?)p.Cash) ?? 0m;
                if (toTalCash <= 0)
                    return BadRequest(ServiceResult<string>.Failure("⚠ لا يوجد رصيد كافي أو لا يوجد شركاء."));

                if (dto.Cost >= toTalCash)
                    return BadRequest(ServiceResult<string>.Failure("عذرآ, الرصيد غير كافي"));

                var clientExists = await _context.Clients.AnyAsync(c => c.NationalId == dto.ClientId);
                if (!clientExists)
                    return BadRequest(ServiceResult<string>.Failure("⚠ العميل غير موجود."));

                var validPartners = await _context.Partners.ToListAsync();
                if (!validPartners.Any())
                    return BadRequest(ServiceResult<string>.Failure("⚠ لا يوجد شركاء مؤهلين."));

                using var transactionScope = await _context.Database.BeginTransactionAsync();
                try
                {
                    var transactions = new List<Transaction>();
                    var PartnersOfProduct = new List<PartnerProduct>();
                    var subTransactionsOfProduct = new List<SubTransaction>();

                    var product = new Product
                    {
                        Name = dto.Name,
                        Cost = dto.Cost,
                        CashPrice = dto.CashPrice,
                        CashPaid = dto.CashPaid,
                        TotalPrice = dto.CashPrice + ((dto.CashPrice - dto.CashPaid) * rate * dto.Duration),
                        Duration = dto.Duration,
                        RemainingMonths = dto.Duration,
                        Description = dto.Description,
                        ClientId = dto.ClientId,
                        Date = DateTime.Now
                    };

                    product.Installment = product.TotalPrice / product.Duration;
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    var transaction = new Transaction
                    {
                        Amount = product.Cost,
                        ProductId = product.ID,
                        IsDebit = false
                    };
                    transactions.Add(transaction);

                    // create installment transactions
                    for (int i = 0; i < dto.Duration; i++)
                    {
                        transactions.Add(new Transaction
                        {
                            Amount = product.Installment,
                            ProductId = product.ID,
                            IsDebit = true,
                            IsPaid = false,
                            Date = DateTime.Now.AddMonths(i + 1)
                        });
                    }

                    _context.Transactions.AddRange(transactions);
                    await _context.SaveChangesAsync();

                    // distribute product cost among partners
                    foreach (var partner in validPartners)
                    {
                        var percentage = partner.Cash / toTalCash;
                        if (partner.Cash < transaction.Amount * percentage) continue;

                        var partnerProduct = new PartnerProduct
                        {
                            PartnerId = partner.NationalId,
                            ProductId = product.ID,
                            Percentage = percentage
                        };
                        PartnersOfProduct.Add(partnerProduct);

                        var subTransaction = new SubTransaction
                        {
                            PartnerId = partner.NationalId,
                            TransactionId = transaction.ID,
                            Amount = transaction.Amount * percentage
                        };
                        subTransactionsOfProduct.Add(subTransaction);

                        partner.Cash -= subTransaction.Amount;
                        partner.WorkingCapital += subTransaction.Amount;
                        partner.NumberOfActiveInventory++;
                    }

                    _context.PartnerProducts.AddRange(PartnersOfProduct);
                    _context.SubTransactions.AddRange(subTransactionsOfProduct);
                    await _context.SaveChangesAsync();

                    await transactionScope.CommitAsync();

                    return Ok(ServiceResult<object>.Success(new
                    {
                        product.ID,
                        product.Name,
                        product.Cost,
                        product.CashPrice,
                        TotalPartners = PartnersOfProduct.Count
                    }, "✅ تم إضافة المنتج بنجاح."));
                }
                catch (Exception ex)
                {
                    await transactionScope.RollbackAsync();
                    return StatusCode(500,
                        ServiceResult<string>.Failure($"⚠ خطأ أثناء إضافة المنتج. {ex.Message}"));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<string>.Failure($"⚠ خطأ غير متوقع: {ex.Message}"));
            }
        }

        // ✅ Pay Installment
        [HttpPost("payment/{id}")]
        public async Task<IActionResult> PaymentOfInstallment([FromRoute] int id)
        {
            if (id <= 0)
                return BadRequest(ServiceResult<string>.Failure("❌ معرف العملية غير صالح."));

            using var transactionScope = await _context.Database.BeginTransactionAsync();

            try
            {
                var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.ID == id);
                if (transaction == null)
                    return NotFound(ServiceResult<string>.Failure("⚠ العملية غير موجودة."));

                if (transaction.IsPaid)
                    return BadRequest(ServiceResult<string>.Failure("❌ هذه القسط مدفوع بالفعل."));

                var product = await _context.Products.FirstOrDefaultAsync(p => p.ID == transaction.ProductId);
                if (product == null)
                    return NotFound(ServiceResult<string>.Failure("⚠ المنتج المرتبط غير موجود."));

                var partnersOfProduct = await _context.PartnerProducts
                    .Where(p => p.ProductId == transaction.ProductId)
                    .ToListAsync();

                if (!partnersOfProduct.Any())
                    return BadRequest(ServiceResult<string>.Failure("⚠ لا يوجد شركاء لهذا المنتج."));

                var partnerIds = partnersOfProduct.Select(pp => pp.PartnerId).ToList();
                var partnersThemSelves = await _context.Partners
                    .Where(p => partnerIds.Contains(p.NationalId))
                    .ToListAsync();

                var subTransactions = new List<SubTransaction>();

                transaction.IsPaid = true;

                foreach (var partner in partnersOfProduct)
                {
                    var subTransaction = new SubTransaction
                    {
                        PartnerId = partner.PartnerId,
                        TransactionId = transaction.ID,
                        Amount = transaction.Amount * partner.Percentage
                    };
                    subTransactions.Add(subTransaction);

                    var partnerEntity = partnersThemSelves.FirstOrDefault(p => p.NationalId == partner.PartnerId);
                    if (partnerEntity == null) continue;

                    var principalAmountFromInstallment = product.CashPrice / product.Duration;
                    var partnerPrincipalAmount = principalAmountFromInstallment * partner.Percentage;

                    partnerEntity.Cash += subTransaction.Amount;
                    partnerEntity.Capital += subTransaction.Amount - partnerPrincipalAmount;
                    partnerEntity.WorkingCapital -= partnerPrincipalAmount;
                }

                await _context.SubTransactions.AddRangeAsync(subTransactions);
                await _context.SaveChangesAsync();
                await transactionScope.CommitAsync();

                return Ok(ServiceResult<object>.Success(new
                {
                    transactionId = transaction.ID,
                    productId = product.ID,
                    totalSubTransactions = subTransactions.Count
                }, "✅ تم دفع القسط بنجاح."));
            }
            catch (Exception ex)
            {
                await transactionScope.RollbackAsync();
                return StatusCode(500,
                    ServiceResult<string>.Failure($"⚠ خطأ أثناء معالجة القسط. {ex.Message}"));
            }
        }

        // ✅ Get All Products
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Client)
                    .AsNoTracking()
                    .ToListAsync();

                if (!products.Any())
                    return NotFound(ServiceResult<List<Product>>.Failure("⚠ لا يوجد منتجات."));

                return Ok(ServiceResult<List<Product>>.Success(products, "✅ تم جلب المنتجات."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<string>.Failure($"⚠ خطأ أثناء جلب المنتجات. {ex.Message}"));
            }
        }

        // ✅ Get Product by Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Client)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ID == id);

                if (product == null)
                    return NotFound(ServiceResult<string>.Failure("⚠ المنتج غير موجود."));

                return Ok(ServiceResult<Product>.Success(product, "✅ تم جلب المنتج."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<string>.Failure($"⚠ خطأ أثناء جلب المنتج. {ex.Message}"));
            }
        }

        // ✅ Update Product
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product dto)
        {
            if (id != dto.ID)
                return BadRequest(ServiceResult<string>.Failure("❌ المعرف لا يطابق المنتج."));

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(ServiceResult<string>.Failure("⚠ المنتج غير موجود."));

                product.Name = dto.Name;
                product.Cost = dto.Cost;
                product.CashPrice = dto.CashPrice;
                product.CashPaid = dto.CashPaid;
                product.TotalPrice = dto.TotalPrice;
                product.Duration = dto.Duration;
                product.Installment = dto.Installment;
                product.RemainingMonths = dto.RemainingMonths;
                product.Description = dto.Description;
                product.ClientId = dto.ClientId;

                await _context.SaveChangesAsync();

                return Ok(ServiceResult<Product>.Success(product, "✅ تم تحديث المنتج بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<string>.Failure($"⚠ خطأ أثناء تحديث المنتج. {ex.Message}"));
            }
        }

        // ✅ Delete Product
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(ServiceResult<string>.Failure("⚠ المنتج غير موجود."));

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(ServiceResult<string>.Success("✅ تم حذف المنتج بنجاح."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<string>.Failure($"⚠ خطأ أثناء حذف المنتج. {ex.Message}"));
            }
        }

        // ✅ Get Products by Partner
        [HttpGet("partner/{partnerId}")]
        public async Task<IActionResult> GetProductsByPartner(string partnerId)
        {
            try
            {
                var products = await _context.PartnerProducts
                    .Where(pp => pp.PartnerId == partnerId)
                    .Include(pp => pp.Product)
                    .Select(pp => pp.Product)
                    .AsNoTracking()
                    .ToListAsync();

                if (!products.Any())
                    return NotFound(ServiceResult<List<Product>>.Failure("⚠ لا يوجد منتجات مرتبطة بهذا الشريك."));

                return Ok(ServiceResult<List<Product>>.Success(products, "✅ تم جلب منتجات الشريك."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResult<string>.Failure($"⚠ خطأ أثناء جلب منتجات الشريك. {ex.Message}"));
            }
        }
    }
}






//[HttpPost]
//public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
//{

//    var totalPercentage = 0.0m;
//    var totalCost = 0.0m;

//    //////////////
//    var toTalCash = await _context.Partners.SumAsync(p => p.Cash);  
//    //var validPartners = await _context.Partners.Where(p => p.Cash >= 500).ToListAsync();
//    var validPartners = await _context.Partners.ToListAsync();

//    var PartnersOfProduct = new List<PartnerProduct>();
//    var subTransactionsOfProduct = new List<SubTransaction>();

//    if (!ModelState.IsValid)
//        return BadRequest(ModelState);

//    if (dto.Cost >= toTalCash)
//        return BadRequest(new { message = "عذرآ, الرصيد غير كافي" });

//    var product = new Product
//    {
//        Name = dto.Name,
//        Cost = dto.Cost,
//        CashPrice = dto.CashPrice,
//        CashPaid = dto.CashPaid,
//        TotalPrice = dto.CashPrice + ((dto.CashPrice - dto.CashPaid) * rate * dto.Duration),
//        Duration = dto.Duration,
//        RemainingMonths = dto.Duration, // ✅ set automatically
//        Description = dto.Description,
//        ClientId = dto.ClientId,
//        Date = DateTime.Now // ✅ set automatically
//    };

//    _context.Products.Add(product);
//    await _context.SaveChangesAsync();

//    var transaction = new Transaction
//    {
//        Amount = product.Cost,  
//        ProductId = product.ID,
//        IsDebit = false, // It is a credit transaction

//    };

//    _context.Transactions.Add(transaction);
//    await _context.SaveChangesAsync();

//    foreach (var partner in validPartners)
//    {
//        var partnerProduct = new PartnerProduct {
//            PartnerId = partner.NationalId,
//            ProductId= product.ID,
//            Percentage = partner.Cash / toTalCash,
//        };

//        if(partner.Cash < transaction.Amount * partnerProduct.Percentage)
//            continue;

//        PartnersOfProduct.Add(partnerProduct);

//        var subTransaction = new SubTransaction
//        {
//            PartnerId = partner.NationalId,
//            TransactionId = transaction.ID,
//            Amount = transaction.Amount * partnerProduct.Percentage,
//        };

//        subTransactionsOfProduct.Add(subTransaction);

//        ////////////////////////////////// test
//        ////////////////////////////////// test
//        totalPercentage += partner.Cash / toTalCash;
//        totalCost += subTransaction.Amount;

//        partner.Cash -= subTransaction.Amount;
//        partner.WorkingCapital += subTransaction.Amount;
//        partner.NumberOfActiveInventory++;



//    }

//    _context.PartnerProducts.AddRange(PartnersOfProduct);
//    _context.SubTransactions.AddRange(subTransactionsOfProduct);

//    Console.WriteLine(totalPercentage);
//    Console.WriteLine(totalCost);

//    await _context.SaveChangesAsync();



//    return CreatedAtAction(nameof(GetProductById), new { id = product.ID }, new
//    {
//        product.ID,
//        product.Name,
//        product.Cost,
//        product.CashPrice
//    });
//}















/////////////////////////////////////////////////////////// TEST
///

//[HttpPost("payment/{id}")]
//public async Task<IActionResult> PaymentOfInstallment([FromRoute] int id)
//{

//    //    ////////////////////////////////test
//    decimal toTalAmount = 0.00m;
//    if (id == null || id <= 0)
//        return BadRequest(new { message = "Invalid transaction id." });

//    using var transactionScope = await _context.Database.BeginTransactionAsync();

//    try
//    {
//        var transaction = await _context.Transactions
//            .FirstOrDefaultAsync(t => t.ID == id);

//        if (transaction == null)
//            return NotFound(new { message = "Transaction not found." });

//        if (transaction.IsPaid)
//            return BadRequest(new { message = "This installment is already paid." });

//        var product = await _context.Products
//            .FirstOrDefaultAsync(p => p.ID == transaction.ProductId);

//        if (product == null)
//            return NotFound(new { message = "Related product not found." });

//        var partnersOfProduct = await _context.PartnerProducts
//            .Where(p => p.ProductId == transaction.ProductId)
//            .ToListAsync();

//        if (!partnersOfProduct.Any())
//            return BadRequest(new { message = "No partners found for this product." });

//        var partnerIds = partnersOfProduct.Select(pp => pp.PartnerId).ToList();

//        var partnersThemSelves = await _context.Partners
//            .Where(p => partnerIds.Contains(p.NationalId))
//            .ToListAsync();

//        if (partnersThemSelves.Count != partnerIds.Count)
//            return BadRequest(new { message = "Some partners are missing in the database." });

//        var subTransactions = new List<SubTransaction>();

//        // ✅ Mark main transaction as paid
//        transaction.IsPaid = true;

//        foreach (var partner in partnersOfProduct)
//        {
//            var subTransaction = new SubTransaction
//            {
//                PartnerId = partner.PartnerId,
//                TransactionId = transaction.ID,
//                Amount = transaction.Amount * partner.Percentage
//            };

//            toTalAmount += subTransaction.Amount;

//            subTransactions.Add(subTransaction);

//            var partnerThemSelf = partnersThemSelves
//                .FirstOrDefault(p => p.NationalId == partner.PartnerId);

//            if (partnerThemSelf == null)
//                continue; // ✅ extra safety, skip missing partner

//            //// ✅ Financial calculations
//            //var principalAmountFromInstallment = product.CashPrice / product.Duration;
//            //var interestFromInstallment = product.Installment - principalAmountFromInstallment;

//            //var partnerPrincipalAmount = principalAmountFromInstallment * partner.Percentage;
//            //var partnerInterestAmount = subTransaction.Amount - partnerPrincipalAmount;

//            //// ✅ Update partner balances
//            //partnerThemSelf.Cash += subTransaction.Amount;
//            //partnerThemSelf.Capital += partnerInterestAmount;
//            //partnerThemSelf.WorkingCapital -= partnerPrincipalAmount;
//            //////////////////////////////////////////////////////////////////////

//            var principalAmountFromIntallment = product.CashPrice / product.Duration;
//            Console.WriteLine($"principalAmountFromIntallment {principalAmountFromIntallment}");
//            var interestFromIntallment = product.Installment - principalAmountFromIntallment;
//            Console.WriteLine($"interestFromIntallment {interestFromIntallment}");

//            var partnerPrincipalAmountFromIntallment = principalAmountFromIntallment * partner.Percentage;
//            Console.WriteLine($"partnerPrincipalAmountFromIntallment {partnerPrincipalAmountFromIntallment}");

//            var partnerInterestFromIntallment = subTransaction.Amount - partnerPrincipalAmountFromIntallment;
//            Console.WriteLine($"partnerInterestFromIntallment {partnerInterestFromIntallment}");

//            Console.WriteLine($"partnerThemSelf.Cash {partnerThemSelf.Cash}");
//            partnerThemSelf.Cash += subTransaction.Amount;
//            Console.WriteLine($"partnerThemSelf.Cash {partnerThemSelf.Cash}");
//            Console.WriteLine($"partnerThemSelf.Capital {partnerThemSelf.Capital}");

//            partnerThemSelf.Capital += partnerInterestFromIntallment;
//            Console.WriteLine($"partnerThemSelf.Capital {partnerThemSelf.Capital}");
//            Console.WriteLine($" {partnerThemSelf.WorkingCapital}");
//            partnerThemSelf.WorkingCapital -= partnerPrincipalAmountFromIntallment;
//            Console.WriteLine($"partnerThemSelf.WorkingCapital {partnerThemSelf.WorkingCapital}");

//        }

//        Console.WriteLine("Ttansaction stored amount" + transaction.Amount);
//        Console.WriteLine("toTalAmount" + toTalAmount);
//        await _context.SubTransactions.AddRangeAsync(subTransactions);

//        await _context.SaveChangesAsync();
//        await transactionScope.CommitAsync();

//        return Ok(new
//        {
//            message = "Installment payment processed successfully.",
//            transactionId = transaction.ID,
//            productId = product.ID,
//            totalSubTransactions = subTransactions.Count
//        });
//    }
//    catch (Exception ex)
//    {
//        await transactionScope.RollbackAsync();
//        // log ex here with ILogger
//        return StatusCode(500, new { message = "An error occurred while processing the installment.", error = ex.Message });
//    }
//}
