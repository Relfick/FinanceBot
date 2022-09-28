using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceBot.Models;

namespace FinanceBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseController : ControllerBase
    {
        private readonly ApplicationContext _db;

        public ExpenseController(ApplicationContext db)
        {
            _db = db;
        }

        // GET: api/Expense
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses()
        {
          if (_db.Expenses == null)
          {
              return NotFound();
          }
            return await _db.Expenses.ToListAsync();
        }

        // GET: api/Expense/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Expense>> GetExpense(int id)
        {
          if (_db.Expenses == null)
          {
              return NotFound();
          }
            var expense = await _db.Expenses.FindAsync(id);

            if (expense == null)
            {
                return NotFound();
            }

            return expense;
        }

        // PUT: api/Expense/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutExpense(int id, Expense expense)
        {
            if (id != expense.id)
            {
                return BadRequest();
            }

            _db.Entry(expense).State = EntityState.Modified;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExpenseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Expense
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Expense>> PostExpense(Expense expense)
        {
          if (_db.Expenses == null)
          {
              return Problem("Entity set 'ApplicationContext.Expenses'  is null.");
          }
          _db.Expenses.Add(expense);
          await _db.SaveChangesAsync();

          return CreatedAtAction("GetExpense", new { id = expense.id }, expense);
        }

        // DELETE: api/Expense/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            if (_db.Expenses == null)
            {
                return NotFound();
            }
            var expense = await _db.Expenses.FindAsync(id);
            if (expense == null)
            {
                return NotFound();
            }

            _db.Expenses.Remove(expense);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private bool ExpenseExists(int id)
        {
            return (_db.Expenses?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}