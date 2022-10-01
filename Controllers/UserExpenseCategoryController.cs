using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceBot.Models;

namespace FinanceBot.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserExpenseCategoryController : ControllerBase
{
    private readonly ApplicationContext _db;

    public UserExpenseCategoryController(ApplicationContext db)
    {
        _db = db;
    }

    // GET: api/UserExpenseCategory
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserExpenseCategory>>> GetUserExpenseCategories()
    {
        if (_db.UserExpenseCategories == null)
        {
            return NotFound();
        }
        return await _db.UserExpenseCategories.ToListAsync();
    }
        
    // GET: api/UserExpenseCategory/5/"еда"/
    [HttpGet("{userId}/{expenseCategory}")]
    public async Task<ActionResult<UserExpenseCategory>> GetUserExpenseCategory(long userId, string expenseCategory)
    {
        // TODO: Проверить
        var userExpenseCategory = await _db.UserExpenseCategories.FirstOrDefaultAsync(e => e.userId == userId && e.expenseCategory == expenseCategory);
        if (userExpenseCategory == null)
            return NotFound();

        return userExpenseCategory;
    }

    // GET: api/UserExpenseCategory/5/
    [HttpGet("{userId}")]
    public async Task<ActionResult<List<UserExpenseCategory>>> GetUserExpenseCategories(long userId)
    {
        if (_db.UserExpenseCategories == null)
        {
            return NotFound();
        }

        return await _db.UserExpenseCategories.Where(user => user.userId == userId).ToListAsync();
    }

    // PUT: api/UserExpenseCategory/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUserExpenseCategory(int id, UserExpenseCategory userExpenseCategory)
    {
        if (id != userExpenseCategory.id)
        {
            return BadRequest();
        }

        var oldUserExpenseCategory = await _db.UserExpenseCategories.FindAsync(id);
        if (oldUserExpenseCategory == null)
            return NotFound();
        
        _db.Entry(userExpenseCategory).State = EntityState.Modified;
        oldUserExpenseCategory.userId = userExpenseCategory.userId;
        oldUserExpenseCategory.expenseCategory = userExpenseCategory.expenseCategory;
        
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExpenseCategoryExists(id))
            {
                return NotFound();
            }

            throw;
        }

        return NoContent();
    }

    // POST: api/UserExpenseCategory
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<UserExpenseCategory>> PostUserExpenseCategory(UserExpenseCategory userExpenseCategory)
    {
        if (_db.UserExpenseCategories == null)
        {
            return Problem("Entity set 'ApplicationContext.UserExpenseCategories'  is null.");
        }
        _db.UserExpenseCategories.Add(userExpenseCategory);
        await _db.SaveChangesAsync();

        return CreatedAtAction("GetUserExpenseCategory", new { id = userExpenseCategory.id }, userExpenseCategory);
    }

    // DELETE: api/UserExpenseCategory/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserExpenseCategory(int id)
    {
        if (_db.UserExpenseCategories == null)
        {
            return NotFound();
        }
        var userExpenseCategory = await _db.UserExpenseCategories.FindAsync(id);
        if (userExpenseCategory == null)
        {
            return NotFound();
        }

        _db.UserExpenseCategories.Remove(userExpenseCategory);
        await _db.SaveChangesAsync();

        return NoContent();
    }
    
    private bool UserExpenseCategoryExists(long id)
    {
        return (_db.UserExpenseCategories?.Any(e => e.id == id)).GetValueOrDefault();
    }

}