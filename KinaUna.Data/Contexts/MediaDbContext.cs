using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.Data.Contexts
{
    public class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
    {
        public DbSet<Picture> PicturesDb { get; set; }
        public DbSet<Video> VideoDb { get; set; }
        public DbSet<CommentThread> CommentThreadsDb { get; set; }
        public DbSet<Comment> CommentsDb { get; set; }
        
    }
}
