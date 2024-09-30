using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.ScheduledTasks;

namespace KinaUnaProgenyApi.Controllers;

[Authorize(AuthenticationSchemes = "Bearer")]
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class RunTasksController(IBackgroundTasksService backgroundTasksService, ITaskRunnerService taskRunnerService) : ControllerBase
{
    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> CheckPictureExtensions([FromBody] KinaUnaBackgroundTask task)
    {
        if (task == null) {
            return BadRequest("Task not found.");
        }

        CustomResult<KinaUnaBackgroundTask> existingTask = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTask.IsFailure)
        {
            return BadRequest("Task not found.");
        }

        if (existingTask.Value.IsRunning) return Ok(task);

        await taskRunnerService.CheckPictureExtensions(task);
        
        return Ok(task);
    }

    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> CheckPictureLinks([FromBody] KinaUnaBackgroundTask task)
    {
        if (task == null)
        {
            return BadRequest("Task not found.");
        }

        CustomResult<KinaUnaBackgroundTask> existingTask = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTask.IsFailure)
        {
            return BadRequest("Task not found.");
        }

        if (existingTask.Value.IsRunning) return Ok(task);

        await taskRunnerService.CheckPictureLinks(task);
        

        return Ok(task);
    }

    /// <summary>
    /// Gets a list of all tasks that can be run in this controller.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("[action]")]
    public IActionResult GetTaskList()
    {
        List<string> taskList = new List<string>();
        foreach (MethodInfo method in typeof(RunTasksController).GetMethods())
        {
            if (method.GetCustomAttributes(typeof(HttpPostAttribute), false).Length > 0)
            {
                taskList.Add(method.Name);
            }
        }

        return Ok(taskList);
    }
}