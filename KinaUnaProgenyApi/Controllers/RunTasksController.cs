using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;

namespace KinaUnaProgenyApi.Controllers;

[Authorize(AuthenticationSchemes = "Bearer")]
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class RunTasksController(IBackgroundTasksService backgroundTasksService, IRunTaskService runTaskService) : ControllerBase
{
    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> CheckPictureExtensions([FromBody] KinaUnaBackgroundTask task)
    {
        if (task == null) {
            return BadRequest("Task not found.");
        }

        KinaUnaBackgroundTask existingTask = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTask == null)
        {
            return BadRequest("Task not found.");
        }

        if (existingTask.IsRunning) return Ok(task);

        await runTaskService.CheckPictureExtensions(task);

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