using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients;

public class TasksHttpClient : ITasksHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiTokenInMemoryClient _apiTokenClient;

    public TasksHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
    {
        _httpClient = httpClient;
        _apiTokenClient = apiTokenClient;
        string clientUri = configuration.GetValue<string>("ProgenyApiServer");

        httpClient.BaseAddress = new Uri(clientUri!);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestVersion = new Version(2, 0);

    }

    public async Task<List<KinaUnaBackgroundTask>> GetTasks()
    {
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
        _httpClient.SetBearerToken(accessToken);

        const string tasksApiPath = "/api/BackgroundTasks/";
        HttpResponseMessage tasksResponseMessage = await _httpClient.GetAsync(tasksApiPath);
        if (!tasksResponseMessage.IsSuccessStatusCode) return new List<KinaUnaBackgroundTask>();

        string tasksListAsString = await tasksResponseMessage.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<List<KinaUnaBackgroundTask>>(tasksListAsString);
    }

    public async Task<KinaUnaBackgroundTask> ExecuteTask(string apiEndpoint)
    {
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
        _httpClient.SetBearerToken(accessToken);

        string tasksApiPath = "/api/BackgroundTasks/" + apiEndpoint;
        HttpResponseMessage tasksResponseMessage = await _httpClient.GetAsync(tasksApiPath);
        if (!tasksResponseMessage.IsSuccessStatusCode) return new KinaUnaBackgroundTask();

        string tasksAsString = await tasksResponseMessage.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<KinaUnaBackgroundTask>(tasksAsString);
    }

    public async Task<KinaUnaBackgroundTask> AddTask(KinaUnaBackgroundTask task)
    {
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
        _httpClient.SetBearerToken(accessToken);

        string tasksApiPath = "/api/BackgroundTasks/";
        HttpResponseMessage tasksResponseMessage = await _httpClient.PostAsync(tasksApiPath, new StringContent(JsonConvert.SerializeObject(task), System.Text.Encoding.UTF8, "application/json"));
        if (!tasksResponseMessage.IsSuccessStatusCode) return new KinaUnaBackgroundTask();

        string addTaskResponseAsString = await tasksResponseMessage.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<KinaUnaBackgroundTask>(addTaskResponseAsString);
    }

    public async Task<KinaUnaBackgroundTask> UpdateTask(KinaUnaBackgroundTask task)
    {
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
        _httpClient.SetBearerToken(accessToken);

        string tasksApiPath = "/api/BackgroundTasks/" + task.TaskId;
        HttpResponseMessage tasksResponseMessage = await _httpClient.PutAsync(tasksApiPath, new StringContent(JsonConvert.SerializeObject(task), System.Text.Encoding.UTF8, "application/json"));
        if (!tasksResponseMessage.IsSuccessStatusCode) return new KinaUnaBackgroundTask();

        string updateTaskResponseAsString = await tasksResponseMessage.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<KinaUnaBackgroundTask>(updateTaskResponseAsString);
    }

    public async Task<bool> DeleteTask(int taskId)
    {
        string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
        _httpClient.SetBearerToken(accessToken);

        string tasksApiPath = "/api/BackgroundTasks/" + taskId;
        HttpResponseMessage tasksResponseMessage = await _httpClient.DeleteAsync(tasksApiPath);
        return tasksResponseMessage.IsSuccessStatusCode;
    }
}