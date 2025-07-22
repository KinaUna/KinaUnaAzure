using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients;

public class TasksHttpClient : ITasksHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiTokenInMemoryClient _apiTokenClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TasksHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _apiTokenClient = apiTokenClient;
        string clientUri = configuration.GetValue<string>("ProgenyApiServer");
        if (env.IsDevelopment())
        {
            clientUri = configuration.GetValue<string>("ProgenyApiServerLocal");
        }

        httpClient.BaseAddress = new Uri(clientUri!);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestVersion = new Version(2, 0);

    }

    public async Task<List<KinaUnaBackgroundTask>> GetTasks()
    {
        bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
        _httpClient.SetBearerToken(accessToken);

        const string tasksApiPath = "/api/BackgroundTasks/GetTasks";
        HttpResponseMessage tasksResponseMessage = await _httpClient.GetAsync(tasksApiPath);
        if (!tasksResponseMessage.IsSuccessStatusCode) return new List<KinaUnaBackgroundTask>();

        string tasksListAsString = await tasksResponseMessage.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<List<KinaUnaBackgroundTask>>(tasksListAsString);
    }

    public async Task<List<KinaUnaBackgroundTask>> ResetTasks()
    {
        bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
        _httpClient.SetBearerToken(accessToken);

        const string tasksApiPath = "/api/BackgroundTasks/ResetAllTasks/";
        HttpResponseMessage tasksResponseMessage = await _httpClient.GetAsync(tasksApiPath);
        if (!tasksResponseMessage.IsSuccessStatusCode) return new List<KinaUnaBackgroundTask>();

        string tasksListAsString = await tasksResponseMessage.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<List<KinaUnaBackgroundTask>>(tasksListAsString);
    }

    public async Task<KinaUnaBackgroundTask> ExecuteTask(KinaUnaBackgroundTask task)
    {
        bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
        _httpClient.SetBearerToken(accessToken);

        string tasksApiPath = "/api/RunTasks/" + task.ApiEndpoint;

        try
        {
            HttpResponseMessage tasksResponseMessage = await _httpClient.PostAsync(tasksApiPath, new StringContent(JsonConvert.SerializeObject(task), System.Text.Encoding.UTF8, "application/json"));
            if (!tasksResponseMessage.IsSuccessStatusCode) return new KinaUnaBackgroundTask();

            string tasksAsString = await tasksResponseMessage.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<KinaUnaBackgroundTask>(tasksAsString);
        }
        catch (Exception)
        {
            // Ignore timeouts, the process could be long-running.
            // Todo: Error handling, delete BackgroundTask with obsolete ApiEndpoint.
            return new KinaUnaBackgroundTask();
        }
    }

    public async Task<KinaUnaBackgroundTask> AddTask(KinaUnaBackgroundTask task)
    {
        bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
        _httpClient.SetBearerToken(accessToken);

        string tasksApiPath = "/api/BackgroundTasks/";
        HttpResponseMessage tasksResponseMessage = await _httpClient.PostAsync(tasksApiPath, new StringContent(JsonConvert.SerializeObject(task), System.Text.Encoding.UTF8, "application/json"));
        if (!tasksResponseMessage.IsSuccessStatusCode) return new KinaUnaBackgroundTask();

        string addTaskResponseAsString = await tasksResponseMessage.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<KinaUnaBackgroundTask>(addTaskResponseAsString);
    }

    public async Task<KinaUnaBackgroundTask> UpdateTask(KinaUnaBackgroundTask task)
    {
        bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
        _httpClient.SetBearerToken(accessToken);

        string tasksApiPath = "/api/BackgroundTasks/" + task.TaskId;
        HttpResponseMessage tasksResponseMessage = await _httpClient.PutAsync(tasksApiPath, new StringContent(JsonConvert.SerializeObject(task), System.Text.Encoding.UTF8, "application/json"));
        if (!tasksResponseMessage.IsSuccessStatusCode) return new KinaUnaBackgroundTask();

        string updateTaskResponseAsString = await tasksResponseMessage.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<KinaUnaBackgroundTask>(updateTaskResponseAsString);
    }

    public async Task<bool> DeleteTask(int taskId)
    {
        bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
        _httpClient.SetBearerToken(accessToken);

        string tasksApiPath = "/api/BackgroundTasks/" + taskId;
        HttpResponseMessage tasksResponseMessage = await _httpClient.DeleteAsync(tasksApiPath);
        return tasksResponseMessage.IsSuccessStatusCode;
    }

    public async Task<List<string>> GetCommands()
    {
        bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
        _httpClient.SetBearerToken(accessToken);

        string tasksApiPath = "/api/BackgroundTasks/GetCommands/";

        HttpResponseMessage tasksResponseMessage = await _httpClient.GetAsync(tasksApiPath);
        if (!tasksResponseMessage.IsSuccessStatusCode) return new List<string>();

        string tasksAsString = await tasksResponseMessage.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<List<string>>(tasksAsString);
    }
}