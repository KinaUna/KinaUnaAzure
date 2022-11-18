using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IPicturesService
    {
        Task<Picture> GetPicture(int id);
        Task<Picture> GetPictureByLink(string link);
        Task<Picture> AddPicture(Picture picture);
        Task<Picture> SetPictureInCache(int id);
        Task<Picture> UpdatePicture(Picture picture);
        Task<Picture> DeletePicture(Picture picture);
        Task RemovePictureFromCache(int pictureId, int progenyId);
        Task<List<Picture>> GetPicturesList(int progenyId);
        Task<List<Picture>> SetPicturesListInCache(int progenyId);
        Task UpdateAllPictures();
    }
}
