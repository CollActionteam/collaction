﻿using System;
using System.ComponentModel.DataAnnotations;

namespace CollAction.Models
{
    public class CrowdactionComment
    {
        public CrowdactionComment(string comment, string? userId, int crowdactionId, DateTime commentedAt, CrowdactionCommentStatus status)
        {
            Comment = comment;
            UserId = userId;
            CrowdactionId = crowdactionId;
            CommentedAt = commentedAt;
            Status = status;
        }

        public int Id { get; set; }

        public DateTime CommentedAt { get; set; }

        [Required]
        public string Comment { get; set; } = null!;

        public string? UserId { get; set; } = null;
        
        public ApplicationUser? User { get; set; }

        public int CrowdactionId { get; set; }

        public Crowdaction? Crowdaction { get; set; }

        public CrowdactionCommentStatus Status { get; set; }
    }
}
