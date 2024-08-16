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
    /// <summary>
    /// Provides methods to interact with the Notes API.
    /// </summary>
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

        /// <summary>
        /// Gets the Note with the given NoteId.
        /// </summary>
        /// <param name="noteId">The Id of the Note to get.</param>
        /// <returns>The Note object with the given NoteId. If the Note cannot be found, a Note with NoteId = 0 is returned.</returns>
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

        /// <summary>
        /// Adds a new Note.
        /// </summary>
        /// <param name="note">The new Note object to add.</param>
        /// <returns>The added Note object.</returns>
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

        /// <summary>
        /// Updates a Note. The Note with the same NoteId will be updated.
        /// </summary>
        /// <param name="note">The Note with the updated properties.</param>
        /// <returns>The updated Note object.</returns>
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

        /// <summary>
        /// Deletes the Note with a given NoteId.
        /// </summary>
        /// <param name="noteId">The NoteId of the Note to remove.</param>
        /// <returns>bool: True if the Note was successfully removed.</returns>
        public async Task<bool> DeleteNote(int noteId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string notesApiPath = "/api/Notes/" + noteId;
            HttpResponseMessage noteResponse = await _httpClient.DeleteAsync(notesApiPath);
            return noteResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the list of all Notes for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of Note objects.</returns>
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
