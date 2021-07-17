using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; //ControllerBase
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Controllers
{
    [Route("api/TodoItems")] // by convention is the controller class name minus the "Controller" suffix
    [ApiController]
    public class TodoItemsController : ControllerBase //without view support
    {
        private readonly TodoContext _context;

        public TodoItemsController(TodoContext context)
        {
            _context = context;
        }

        // GET: api/TodoItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItemDTO>>> GetTodoItems()
        {
            List<TodoItemDTO> allCurrentTodoItems = await _context.TodoItems // .ToListAsync()
                .Select(x => ItemToDTO(x))
                .ToListAsync();

            return allCurrentTodoItems;
            // returns a list of Tasks (Threadable.Task) that represent the async operations
        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItemDTO>> GetTodoItem(long id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            // null if item is not found

            bool ItemNotFound = (todoItem == null);
            if (ItemNotFound)
            {
                return NotFound();
            }

            return ItemToDTO(todoItem);
        }

        // PUT: api/TodoItems/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodoItem(long id, TodoItemDTO updatedTodoItemDTO)
        {
            if (id != updatedTodoItemDTO.Id)
            {
                return BadRequest();
            }

            // check context then database for a TodoItem with id==id (null if not found)
            var existingTodoItem = await _context.TodoItems.FindAsync(id);
            bool EntityNotFound = (existingTodoItem == null);
            if (EntityNotFound)
            {
                return NotFound();
            }

            // else: entity was found
            // update the entity with the new values
            existingTodoItem.Name = updatedTodoItemDTO.Name;
            existingTodoItem.IsComplete = updatedTodoItemDTO.IsComplete;

            try
            {   // save changes to the database
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) when (!TodoItemExists(id))
            {   // when unexpected number of rows would be affected by the change
                return NotFound(); // the NotFoundResult
            }

            return NoContent(); // 204 - No content result (update success)
        }

        // POST: api/TodoItems
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        // TODO prevent over-posting
        [HttpPost("/orig")]
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
        {
            _ = _context.TodoItems.Add(todoItem);
            _ = await _context.SaveChangesAsync();

            string UrlActionToUse = nameof(GetTodoItem);
            object RouteDataURLValue = new { id = todoItem.Id };
            object EntityBodyContent = todoItem;

            CreatedAtActionResult CreatedAction = CreatedAtAction(UrlActionToUse, RouteDataURLValue, EntityBodyContent);

            return CreatedAction; // AspNetCore.Mvc.CreatedAtActionResult

            /* Returns an HTTP 201 status code if successful. HTTP 201 is the standard response for an HTTP POST method that creates a new resource on the server.
             * Adds a Location header to the response. The Location header specifies the URI of the newly created to-do item. For more information, see 10.2.2 201 Created.
             * References the GetTodoItem action to create the Location header's URI. The C# nameof keyword is used to avoid hard-coding the action name in the CreatedAtAction call.
             */
        }

        [HttpPost]
        public async Task<ActionResult<TodoItemDTO>> CreateTodoItem(TodoItemDTO newTodoItemDTO)
        {
            TodoItem NewTodoItem = new TodoItem
            {
                IsComplete = newTodoItemDTO.IsComplete,
                Name = newTodoItemDTO.Name
            };

            _context.TodoItems.Add(NewTodoItem); // Id is automatically generated
            await _context.SaveChangesAsync();

            var result = CreatedAtAction(
                nameof(GetTodoItem),
                new { id = NewTodoItem.Id },
                ItemToDTO(NewTodoItem));

            return result;
        }


        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<TodoItem>> DeleteTodoItem(long id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            //return todoItem;
            return NoContent();
        }
        
        private static TodoItemDTO ItemToDTO(TodoItem todoItem) =>
            new TodoItemDTO
            {
                Id = todoItem.Id,
                Name = todoItem.Name,
                IsComplete = todoItem.IsComplete
            };

        private bool TodoItemExists(long id)
        {
            return _context.TodoItems.Any(e => e.Id == id);
        }
    }
}
