using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Helpers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.ScheduledTasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class BackgroundTasksController(IBackgroundTasksService backgroundTasksService, IUserInfoService userInfoService) : ControllerBase
{
    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> GetTasks()
    {
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

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
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

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
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

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
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

        if (task == null)
        {
            return BadRequest("Task not found.");
        }

        CustomResult<KinaUnaBackgroundTask> existingTaskResult = await backgroundTasksService.GetTask(task.TaskId);
        if (existingTaskResult.IsFailure)
        {
            return NotFound(existingTaskResult.Error?.Message);
        }

        if (task.LastRun < DateTime.UtcNow - TimeSpan.FromDays(30))
        {
            task.LastRun = existingTaskResult.Value.LastRun;
        }

        task.IsRunning = existingTaskResult.Value.IsRunning;
        
        CustomResult<KinaUnaBackgroundTask> updateTaskResult = await backgroundTasksService.UpdateTask(task);
        
        if (updateTaskResult.IsFailure)
        {
            return BadRequest(updateTaskResult.Error?.Message);
        }

        return Ok(updateTaskResult.Value);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

        CustomResult<KinaUnaBackgroundTask> existingTask = await backgroundTasksService.GetTask(id);
        if (existingTask.IsFailure)
        {
            return NotFound(existingTask.Error?.Message);
        }

        CustomResult<KinaUnaBackgroundTask> result = await backgroundTasksService.DeleteTask(id);
        if (!result.IsFailure)
        {
            return BadRequest(result.Error?.Message);
        }

        return Ok(true);
    }

    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> GetCommands()
    {
        UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
        if (userInfo == null || !userInfo.IsKinaUnaAdmin)
        {
            return Unauthorized("User not admin.");
        }

        List<string> commandsList = BackgroundTasksUtilities.GetCommands();

        return Ok(commandsList);
    }
}