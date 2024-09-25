using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services.ScheduledTasks;

namespace KinaUnaProgenyApi.Controllers;

[Authorize(AuthenticationSchemes = "Bearer")]
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class BackgroundTasksController(IBackgroundTasksService backgroundTasksService) : ControllerBase
{
    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> GetTasks()
    {
        List<KinaUnaBackgroundTask> tasks = await backgroundTasksService.GetTasks();

        return Ok(tasks);

    }

    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> ResetAllTasks()
    {
        List<KinaUnaBackgroundTask> tasks = await backgroundTasksService.GetTasks();
        foreach (KinaUnaBackgroundTask task in tasks)
        {
            task.IsRunning = false;
            _ = await backgroundTasksService.UpdateTask(task);
        }

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> AddTask([FromBody] KinaUnaBackgroundTask task)
    {
        if (task == null) {
            return BadRequest("Task not found.");
        }

        KinaUnaBackgroundTask existingTask = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTask != null)
        {
            return Conflict("Task already exists.");
        }

        KinaUnaBackgroundTask newTask = await backgroundTasksService.AddTask(task);
        return Ok(newTask ?? task);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] KinaUnaBackgroundTask task)
    {
        if (task == null)
        {
            return BadRequest("Task not found.");
        }

        KinaUnaBackgroundTask existingTask = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTask == null)
        {
            return NotFound("Task not found.");
        }

        KinaUnaBackgroundTask updatedTask = await backgroundTasksService.UpdateTask(task);
        
        return Ok(updatedTask ?? new KinaUnaBackgroundTask());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        KinaUnaBackgroundTask existingTask = await backgroundTasksService.GetTask(taskId);
        if (existingTask == null)
        {
            return NotFound("Task not found.");
        }

        bool result = await backgroundTasksService.DeleteTask(taskId);
        if (!result)
        {
            return BadRequest("Task could not be deleted.");
        }

        return Ok(true);
    }
}