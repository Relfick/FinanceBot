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
public class UserWorkModeController : ControllerBase
{
    private readonly ApplicationContext _db;

    public UserWorkModeController(ApplicationContext db)
    {
        _db = db;
    }

    // GET: api/UserWorkmode
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserWorkMode>>> GetUserWorkModes()
    {
        if (_db.UserWorkModes == null)
        {
            return NotFound();
        }
        return await _db.UserWorkModes.ToListAsync();
    }

    // GET: api/UserWorkmode/5
    [HttpGet("{userId}")]
    public async Task<ActionResult<WorkMode>> GetUserWorkMode(int userId)
    {
        if (_db.UserWorkModes == null)
        {
            return NotFound();
        }
        var userWorkMode = await _db.UserWorkModes.FirstOrDefaultAsync(u => u.userId == userId);

        if (userWorkMode == null)
        {
            return NotFound();
        }

        return userWorkMode.workMode;
    }

    // PUT: api/UserWorkmode/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{userId}")]
    public async Task<IActionResult> PutUserWorkMode(long userId, UserWorkMode userWorkMode)
    {
        if (userId != userWorkMode.userId)
            return BadRequest();

        var existingUserWorkmode = await _db.UserWorkModes.FirstOrDefaultAsync(u => u.userId == userId);
        if (existingUserWorkmode == null)
            return NotFound();

        // _db.Entry(userWorkMode).State = EntityState.Modified;
        existingUserWorkmode.workMode = userWorkMode.workMode;
        
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // if (!UserWorkModeExists(userId))
            // {
            //     return NotFound();
            // }
            // else
            // {
            //     throw;
            // }
            throw;
        }

        return NoContent();
    }

    // POST: api/UserWorkmode
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<UserWorkMode>> PostUserWorkMode(UserWorkMode userWorkMode)
    {
        if (_db.UserWorkModes == null)
        {
            return Problem("Entity set 'ApplicationContext.UserWorkModes'  is null.");
        }
        _db.UserWorkModes.Add(userWorkMode);
        await _db.SaveChangesAsync();

        return CreatedAtAction("GetUserWorkMode", new { id = userWorkMode.id }, userWorkMode);
    }

    // DELETE: api/UserWorkmode/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserWorkMode(int id)
    {
        if (_db.UserWorkModes == null)
        {
            return NotFound();
        }
        var userWorkMode = await _db.UserWorkModes.FindAsync(id);
        if (userWorkMode == null)
        {
            return NotFound();
        }

        _db.UserWorkModes.Remove(userWorkMode);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private bool UserWorkModeExists(int id)
    {
        return (_db.UserWorkModes?.Any(e => e.id == id)).GetValueOrDefault();
    }
}