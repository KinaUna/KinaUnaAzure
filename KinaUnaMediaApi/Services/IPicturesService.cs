using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaMediaApi.Services
{
    public interface IPicturesService
    {
        Task<Picture> GetPicture(int id);
        Task<Picture> GetPictureByLink(string link);
        Task<Picture> AddPicture(Picture picture);
        Task<Picture> SetPicture(int id);
        Task<Picture> UpdatePicture(Picture picture);
        Task<Picture> DeletePicture(Picture picture);
        Task RemovePicture(int pictureId, int progenyId);
        Task<List<Picture>> GetPicturesList(int progenyId);
        Task<List<Picture>> SetPicturesList(int progenyId);
    }
}
