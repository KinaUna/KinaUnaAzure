using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ICommentsService
    {
        /// <summary>
        /// Gets a Comment from the cache.
        /// If it isn't in the cache gets it from the database.
        /// </summary>
        /// <param name="commentId">The CommentId of the Comment to get.</param>
        /// <returns>Comment.</returns>
        Task<Comment> GetComment(int commentId);

        /// <summary>
        /// Gets a List of Comments for a CommentThread from the cache.
        /// If the list isn't found in the cache, gets it from the database and sets it in the cache.
        /// </summary>
        /// <param name="commentThreadId">The CommentThreadId to get Comments for.</param>
        /// <returns>List of Comments.</returns>
        Task<List<Comment>> GetCommentsList(int commentThreadId);

        /// <summary>
        /// Gets and sets a List of Comments for a CommentThread in the cache.
        /// </summary>
        /// <param name="commentThreadId">The CommentThreadId to get Comments for.</param>
        /// <returns>List of Comments.</returns>
        Task<List<Comment>> SetCommentsList(int commentThreadId);

        /// <summary>
        /// Removes a List of Comments for a CommentThread from the cache.
        /// </summary>
        /// <param name="commentThreadId">The CommentThreadId of the cached list to remove.</param>
        /// <returns></returns>
        Task RemoveCommentsList(int commentThreadId);

        /// <summary>
        /// Adds a new Comment to the database.
        /// Then updates the CommentThread and the cache for the CommentThread.
        /// </summary>
        /// <param name="comment">The Comment to add.</param>
        /// <returns>The added Comment.</returns>
        Task<Comment> AddComment(Comment comment);

        /// <summary>
        /// Updates a Comment in the database and cache.
        /// Then updates the CommentThread and the cache for the CommentThread.
        /// </summary>
        /// <param name="comment">The Comment with updated properties.</param>
        /// <returns>The updated Comment.</returns>
        Task<Comment> UpdateComment(Comment comment);

        /// <summary>
        /// Deletes a Comment from the database and removes it from the cache.
        /// Then updates the CommentThread and the cache for the CommentThread.
        /// </summary>
        /// <param name="comment">The Comment to delete.</param>
        /// <returns>The deleted Comment.</returns>
        Task<Comment> DeleteComment(Comment comment);

        /// <summary>
        /// Gets a CommentThread entity from the database.
        /// </summary>
        /// <param name="commentThreadId">The CommentThreadId of the CommentThread entity to get.</param>
        /// <returns>CommentThread object.</returns>
        Task<CommentThread> GetCommentThread(int commentThreadId);

        /// <summary>
        /// Adds a new CommentThread entity to the database.
        /// </summary>
        /// <returns>The added CommentThread object.</returns>
        Task<CommentThread> AddCommentThread();

        /// <summary>
        /// Deletes a CommentThread entity from the database.
        /// </summary>
        /// <param name="commentThread">The CommentThread entity to delete.</param>
        /// <returns>The deleted CommentThread entity.</returns>
        Task<CommentThread> DeleteCommentThread(CommentThread commentThread);
    }
}
