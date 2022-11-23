using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IVideosService
    {
        Task<Video> GetVideo(int id);
        Task<Video> GetVideoByLink(string link, int progenyId);
        Task<Video> SetVideoInCache(int id);
        Task<Video> AddVideo(Video video);
        Task<Video> UpdateVideo(Video video);
        Task<Video> DeleteVideo(Video video);
        Task RemoveVideoFromCache(int videoId, int progenyId);
        Task<List<Video>> GetVideosList(int progenyId);
        Task<List<Video>> SetVideosListInCache(int progenyId);
    }
}
