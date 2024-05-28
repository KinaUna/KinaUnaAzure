using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    public class NotesHttpClient : INotesHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public NotesHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }


        public async Task<Note> GetNote(int noteId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            Note noteItem = new();
            string notesApiPath = "/api/Notes/" + noteId;
            HttpResponseMessage noteResponse = await _httpClient.GetAsync(notesApiPath);
            if (!noteResponse.IsSuccessStatusCode) return noteItem;

            string noteAsString = await noteResponse.Content.ReadAsStringAsync();
            noteItem = JsonConvert.DeserializeObject<Note>(noteAsString);

            return noteItem;
        }

        public async Task<Note> AddNote(Note note)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string notesApiPath = "/api/Notes/";
            HttpResponseMessage notesResponse = await _httpClient.PostAsync(notesApiPath, new StringContent(JsonConvert.SerializeObject(note), System.Text.Encoding.UTF8, "application/json"));
            if (!notesResponse.IsSuccessStatusCode) return new Note();

            string notesAsString = await notesResponse.Content.ReadAsStringAsync();
            note = JsonConvert.DeserializeObject<Note>(notesAsString);
            return note;

        }

        public async Task<Note> UpdateNote(Note note)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateApiPath = "/api/Notes/" + note.NoteId;
            HttpResponseMessage noteResponse = await _httpClient.PutAsync(updateApiPath, new StringContent(JsonConvert.SerializeObject(note), System.Text.Encoding.UTF8, "application/json"));
            if (!noteResponse.IsSuccessStatusCode) return new Note();

            string noteAsString = await noteResponse.Content.ReadAsStringAsync();
            note = JsonConvert.DeserializeObject<Note>(noteAsString);
            return note;

        }

        public async Task<bool> DeleteNote(int noteId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string notesApiPath = "/api/Notes/" + noteId;
            HttpResponseMessage noteResponse = await _httpClient.DeleteAsync(notesApiPath);
            return noteResponse.IsSuccessStatusCode;
        }

        public async Task<List<Note>> GetNotesList(int progenyId, int accessLevel)
        {
            List<Note> progenyNotesList = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string notesApiPath = "/api/Notes/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage notesResponse = await _httpClient.GetAsync(notesApiPath);
            if (!notesResponse.IsSuccessStatusCode) return progenyNotesList;

            string notesAsString = await notesResponse.Content.ReadAsStringAsync();
            progenyNotesList = JsonConvert.DeserializeObject<List<Note>>(notesAsString);

            return progenyNotesList;
        }
    }
}
