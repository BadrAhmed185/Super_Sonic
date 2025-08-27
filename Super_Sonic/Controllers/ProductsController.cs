using Microsoft.AspNetCore.Mvc;
using Super_Sonic.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
        {


            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Cost <= 0 || dto.CashPrice <= 0 || dto.CashPaid < 0)
                return BadRequest(new { message = "⚠ القيم المالية يجب أن تكون موجبة." });

            var toTalCash = await _context.Partners.SumAsync(p => (decimal?)p.Cash) ?? 0m;
            if (toTalCash <= 0)
                return BadRequest(new { message = "⚠ لا يوجد رصيد كافي أو لا يوجد شركاء." });

            if (dto.Cost >= toTalCash)
                return BadRequest(new { message = "عذرآ, الرصيد غير كافي" });

            var clientExists = await _context.Clients.AnyAsync(c => c.NationalId == dto.ClientId);
            if (!clientExists)
                return BadRequest(new { message = "⚠ العميل غير موجود." });

            //var validPartners = await _context.Partners
            //    .Where(p => p.Cash >= 500)
            //    .ToListAsync(); 

            var validPartners = await _context.Partners
                .ToListAsync();

            if (!validPartners.Any())
                return BadRequest(new { message = "⚠ لا يوجد شركاء مؤهلين." });

            using var transactionScope = await _context.Database.BeginTransactionAsync();
            try
            {
                //Memory variables that we will need in the try 
                var transactions = new List<Transaction>();
                var PartnersOfProduct = new List<PartnerProduct>();
                var subTransactionsOfProduct = new List<SubTransaction>();
                var totalPercentage = 0m;
                var totalCost = 0m;

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

                for (int i = 0; i < dto.Duration; i++)
                {

                    var debitTransaction = new Transaction
                    {
                        Amount = product.Installment,
                        ProductId = product.ID,
                        IsDebit = true,
                        IsPaid = false,
                        Date = DateTime.Now.AddMonths(i + 1)
                    };

                    transactions.Add(debitTransaction);

                }
                _context.Transactions.AddRange(transactions);
                await _context.SaveChangesAsync();


                foreach (var partner in validPartners)
                {
                    // var percentage = Math.Round(partner.Cash / toTalCash, 4);
                    var percentage = partner.Cash / toTalCash;

                    if (partner.Cash < transaction.Amount * percentage)
                        continue;

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

                    totalPercentage += percentage;
                    totalCost += subTransaction.Amount;

                    partner.Cash -= subTransaction.Amount;
                    partner.WorkingCapital += subTransaction.Amount;
                    partner.NumberOfActiveInventory++;
                }

                Console.WriteLine(totalPercentage);
                Console.WriteLine(totalCost);

                _context.PartnerProducts.AddRange(PartnersOfProduct);
                _context.SubTransactions.AddRange(subTransactionsOfProduct);
                await _context.SaveChangesAsync();

                await transactionScope.CommitAsync();

                return CreatedAtAction(nameof(GetProductById), new { productId = product.ID }, new
                {
                    product.ID,
                    product.Name,
                    product.Cost,
                    product.CashPrice,
                    TotalPartners = PartnersOfProduct.Count
                });
            }
            catch (Exception ex)
            {
                await transactionScope.RollbackAsync();
                return StatusCode(500, new { message = "⚠ حدث خطأ أثناء إضافة المنتج.", error = ex.Message });
            }

        }

        [HttpPost("payment/{id}")]
        public async Task<IActionResult> PaymentOfInstallment([FromRoute] int id)
        {

 
            if (id == null || id <= 0)
                return BadRequest(new { message = "Invalid transaction id." });

            using var transactionScope = await _context.Database.BeginTransactionAsync();

            try
            {
                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.ID == id);

                if (transaction == null)
                    return NotFound(new { message = "Transaction not found." });

                if (transaction.IsPaid)
                    return BadRequest(new { message = "This installment is already paid." });

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ID == transaction.ProductId);

                if (product == null)
                    return NotFound(new { message = "Related product not found." });

                var partnersOfProduct = await _context.PartnerProducts
                    .Where(p => p.ProductId == transaction.ProductId)
                    .ToListAsync();

                if (!partnersOfProduct.Any())
                    return BadRequest(new { message = "No partners found for this product." });

                var partnerIds = partnersOfProduct.Select(pp => pp.PartnerId).ToList();

                var partnersThemSelves = await _context.Partners
                    .Where(p => partnerIds.Contains(p.NationalId))
                    .ToListAsync();

                if (partnersThemSelves.Count != partnerIds.Count)
                    return BadRequest(new { message = "Some partners are missing in the database." });

                var subTransactions = new List<SubTransaction>();

                //  Mark main transaction as paid
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

                    var partnerThemSelf = partnersThemSelves
                        .FirstOrDefault(p => p.NationalId == partner.PartnerId);

                    if (partnerThemSelf == null)
                        continue; // ✅ extra safety, skip missing partner

                    //  Financial calculations
                    var principalAmountFromInstallment = product.CashPrice / product.Duration;
                    var interestFromInstallment = product.Installment - principalAmountFromInstallment;

                    var partnerPrincipalAmount = principalAmountFromInstallment * partner.Percentage;
                    var partnerInterestAmount = subTransaction.Amount - partnerPrincipalAmount;

                    // Update partner balances
                    partnerThemSelf.Cash += subTransaction.Amount;
                    partnerThemSelf.Capital += partnerInterestAmount;
                    partnerThemSelf.WorkingCapital -= partnerPrincipalAmount;
                    //////////////////////////////////////////////////////////////////////



                }
                await _context.SubTransactions.AddRangeAsync(subTransactions);

                await _context.SaveChangesAsync();
                await transactionScope.CommitAsync();

                return Ok(new
                {
                    message = "Installment payment processed successfully.",
                    transactionId = transaction.ID,
                    productId = product.ID,
                    totalSubTransactions = subTransactions.Count
                });
            }
            catch (Exception ex)
            {
                await transactionScope.RollbackAsync();
                // log ex here with ILogger
                return StatusCode(500, new { message = "An error occurred while processing the installment.", error = ex.Message });
            }
        }


        //            {
        //                "name": "Iphone 17 pro max",
        //  "cost": 100000,
        //  "cashPrice": 120000,
        //  "cashPaid": 20000,
        //  "duration": 12,
        //  "description": "This is my first product add test",
        //  "clientId": "3030"
        //}

        // Optional GET by Id for CreatedAtAction

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();
            return Ok(product);
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
