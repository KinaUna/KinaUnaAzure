using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.Data.Contexts
{
    public class MediaDbContext : DbContext
    {
        public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
        {

        }

        public DbSet<Picture> PicturesDb { get; set; }
        public DbSet<Video> VideoDb { get; set; }
        public DbSet<CommentThread> CommentThreadsDb { get; set; }
        public DbSet<Comment> CommentsDb { get; set; }

    }
}
