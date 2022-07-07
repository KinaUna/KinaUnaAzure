using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaMediaApi.Services
{
    public interface IVideosService
    {
        Task<Video> GetVideo(int id);
        Task<Video> GetVideoByLink(string link, int progenyId);
        Task<Video> SetVideo(int id);
        Task<Video> AddVideo(Video video);
        Task<Video> UpdateVideo(Video video);
        Task<Video> DeleteVideo(Video video);
        Task RemoveVideo(int videoId, int progenyId);
        Task<List<Video>> GetVideosList(int progenyId);
        Task<List<Video>> SetVideosList(int progenyId);
    }
}
