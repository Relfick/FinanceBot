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
public class BotUserController : ControllerBase
{
    private readonly ApplicationContext _db;

    public BotUserController(ApplicationContext db)
    {
        this._db = db;
    }

    // GET: api/BotUser
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        if (_db.Users == null)
        {
            return NotFound();
        }
        return await _db.Users.ToListAsync();
    }

    // GET: api/BotUser/5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(long id)
    {
        if (_db.Users == null)
        {
            return NotFound();
        }
        var user = await _db.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return user;
    }

    // PUT: api/BotUser/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(long id, User user)
    {
        if (id != user.id)
        {
            return BadRequest();
        }

        _db.Entry(user).State = EntityState.Modified;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
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

    // POST: api/BotUser
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<User>> PostUser(User user)
    {
        if (_db.Users == null)
        {
            return Problem("Entity set 'ApplicationContext.Users'  is null.");
        }
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction("GetUser", new { id = user.id }, user);
    }

    // DELETE: api/BotUser/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(long id)
    {
        if (_db.Users == null)
        {
            return NotFound();
        }
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private bool UserExists(long id)
    {
        return (_db.Users?.Any(e => e.id == id)).GetValueOrDefault();
    }
}