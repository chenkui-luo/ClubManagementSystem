using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using ClubManagementSystem.App.Models;

namespace ClubManagementSystem.App.Services
{
    public class NoticeService
    {
        public List<Notice> GetClubNotices(int? clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.Notices
                    .Where(n => n.ClubId == clubId)
                    .Include(n => n.Publisher)
                    .OrderByDescending(n => n.PublishTime)
                    .ToList();
            }
        }

        public List<Notice> GetAllNotices()
        {
            using (var db = new AppDbContext())
            {
                return db.Notices
                    .Include(n => n.Publisher)
                    .Include(n => n.Club)
                    .OrderByDescending(n => n.PublishTime)
                    .ToList();
            }
        }

        public Notice Publish(Notice notice)
        {
            using (var db = new AppDbContext())
            {
                notice.PublishTime = DateTime.Now;
                db.Notices.Add(notice);
                db.SaveChanges();
                return notice;
            }
        }

        public bool DeleteNotice(int noticeId)
        {
            using (var db = new AppDbContext())
            {
                var notice = db.Notices.Find(noticeId);
                if (notice == null) return false;

                db.Notices.Remove(notice);
                db.SaveChanges();
                return true;
            }
        }
    }
}
