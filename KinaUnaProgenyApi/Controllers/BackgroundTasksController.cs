using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Helpers;
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
        CustomResult<List<KinaUnaBackgroundTask>> getTasksResult = await backgroundTasksService.GetTasks();

        if (getTasksResult.IsFailure)
        {
            return NotFound(getTasksResult.Error?.Message);
        }

        return Ok(getTasksResult.Value);

    }

    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> ResetAllTasks()
    {
        CustomResult<List<KinaUnaBackgroundTask>> resetTasksResult = await backgroundTasksService.ResetTasks();

        if (resetTasksResult.IsFailure)
        {
            return NotFound(resetTasksResult.Error?.Message);
        }

        return Ok(resetTasksResult.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddTask([FromBody] KinaUnaBackgroundTask task)
    {
        if (task == null || !task.ValidateBackgroundTask()) {
            return BadRequest("Task invalid.");
        }
        
        CustomResult<KinaUnaBackgroundTask> addTaskResult = await backgroundTasksService.AddTask(task);
        if (addTaskResult.IsFailure)
        {
            return BadRequest(addTaskResult.Error?.Message);
        }

        return Ok(addTaskResult.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] KinaUnaBackgroundTask task)
    {
        if (task == null)
        {
            return BadRequest("Task not found.");
        }

        CustomResult<KinaUnaBackgroundTask> existingTaskResult = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTaskResult.IsFailure)
        {
            return NotFound(existingTaskResult.Error?.Message);
        }

        CustomResult<KinaUnaBackgroundTask> updateTaskResult = await backgroundTasksService.UpdateTask(task);
        
        if (updateTaskResult.IsFailure)
        {
            return BadRequest(updateTaskResult.Error?.Message);
        }

        return Ok(updateTaskResult.Value);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        CustomResult<KinaUnaBackgroundTask> existingTask = await backgroundTasksService.GetTask(taskId);
        if (existingTask.IsFailure)
        {
            return NotFound(existingTask.Error?.Message);
        }

        CustomResult<KinaUnaBackgroundTask> result = await backgroundTasksService.DeleteTask(taskId);
        if (!result.IsFailure)
        {
            return BadRequest(result.Error?.Message);
        }

        return Ok(true);
    }
}