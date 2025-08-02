using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.ScheduledTasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers;

[Authorize(Policy = "UserOrClient")]
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class RunTasksController(IBackgroundTasksService backgroundTasksService, ITaskRunnerService taskRunnerService,
    IUserInfoService userInfoService) : ControllerBase
{
    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> CheckPictureExtensions([FromBody] KinaUnaBackgroundTask task)
    {
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

        if (task == null) {
            return BadRequest("Task not found.");
        }

        CustomResult<KinaUnaBackgroundTask> existingTask = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTask.IsFailure)
        {
            return existingTask.ToActionResult();
        }

        if (existingTask.Value.IsRunning) return Ok(task);

        await taskRunnerService.CheckPictureExtensions(task);
        
        return Ok(task);
    }

    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> CheckPictureLinks([FromBody] KinaUnaBackgroundTask task)
    {
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

        if (task == null)
        {
            return BadRequest("Task not found.");
        }

        CustomResult<KinaUnaBackgroundTask> existingTask = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTask.IsFailure)
        {
            return existingTask.ToActionResult();
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
    public async Task<IActionResult> GetTaskList()
    {
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

        List<string> taskList = [];
        foreach (MethodInfo method in typeof(RunTasksController).GetMethods())
        {
            if (method.GetCustomAttributes(typeof(HttpPostAttribute), false).Length > 0)
            {
                taskList.Add(method.Name);
            }
        }

        return Ok(taskList);
    }

    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> SendCalendarReminders([FromBody] KinaUnaBackgroundTask task)
    {
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

        if (task == null)
        {
            return BadRequest("Task not found.");
        }

        CustomResult<KinaUnaBackgroundTask> existingTask = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTask.IsFailure)
        {
            return existingTask.ToActionResult();
        }

        if (existingTask.Value.IsRunning) return Ok(task);

        await taskRunnerService.SendCalendarReminders(task);

        return Ok(task);
    }
}