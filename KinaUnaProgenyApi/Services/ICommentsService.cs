﻿using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ICommentsService
    {
        Task<Comment> GetComment(int commentId);
        Task<List<Comment>> GetCommentsList(int commentThreadId);
        Task<List<Comment>> SetCommentsList(int commentThreadId);
        Task RemoveCommentsList(int commentThreadId);
        Task<Comment> AddComment(Comment comment);
        Task<Comment> UpdateComment(Comment comment);
        Task<Comment> DeleteComment(Comment comment);
        Task<CommentThread> GetCommentThread(int commentThreadId);
        Task<CommentThread> AddCommentThread();
        Task<CommentThread> DeleteCommentThread(CommentThread commentThread);
    }
}
