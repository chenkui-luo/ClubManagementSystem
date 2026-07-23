using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using ClubManagementSystem.App.Models;

namespace ClubManagementSystem.App.Services
{
    public class ActivityService
    {
        public List<Activity> GetClubActivities(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.Activities
                    .Where(a => a.ClubId == clubId)
                    .Include(a => a.Registrations)
                    .OrderByDescending(a => a.StartTime)
                    .ToList();
            }
        }

        public List<Activity> GetAllActivities()
        {
            using (var db = new AppDbContext())
            {
                return db.Activities
                    .Include(a => a.Club)
                    .Include(a => a.Registrations)
                    .OrderByDescending(a => a.StartTime)
                    .ToList();
            }
        }

        public Activity CreateActivity(Activity activity)
        {
            using (var db = new AppDbContext())
            {
                activity.Status = "报名中";
                activity.CreatedAt = DateTime.Now;
                db.Activities.Add(activity);
                db.SaveChanges();
                return activity;
            }
        }

        public bool CancelActivity(int activityId)
        {
            using (var db = new AppDbContext())
            {
                var activity = db.Activities.Find(activityId);
                if (activity == null) return false;

                activity.Status = "已取消";
                db.SaveChanges();
                return true;
            }
        }

        public bool DeleteActivity(int activityId)
        {
            using (var db = new AppDbContext())
            {
                var activity = db.Activities.Find(activityId);
                if (activity == null) return false;

                var registrations = db.ActivityRegistrations.Where(r => r.ActivityId == activityId);
                db.ActivityRegistrations.RemoveRange(registrations);

                var files = db.ActivityFiles.Where(f => f.ActivityId == activityId);
                db.ActivityFiles.RemoveRange(files);

                db.Activities.Remove(activity);
                db.SaveChanges();
                return true;
            }
        }

        public bool UpdateActivity(Activity activity)
        {
            using (var db = new AppDbContext())
            {
                var existing = db.Activities.Find(activity.Id);
                if (existing == null) return false;

                existing.Title = activity.Title;
                existing.Description = activity.Description;
                existing.Location = activity.Location;
                existing.StartTime = activity.StartTime;
                existing.EndTime = activity.EndTime;
                existing.MaxPeople = activity.MaxPeople;
                db.SaveChanges();
                return true;
            }
        }

        public bool FinishActivity(int activityId, string summary)
        {
            using (var db = new AppDbContext())
            {
                var activity = db.Activities.Find(activityId);
                if (activity == null) return false;

                activity.Status = "已结束";
                activity.Summary = summary;
                db.SaveChanges();
                return true;
            }
        }

        public bool RegisterForActivity(int activityId, int userId)
        {
            using (var db = new AppDbContext())
            {
                if (db.ActivityRegistrations.Any(r => r.ActivityId == activityId && r.UserId == userId))
                    return false;

                var activity = db.Activities.Find(activityId);
                if (activity == null || activity.Status != "报名中") return false;
                if (activity.StartTime <= DateTime.Now) return false;

                if (activity.MaxPeople.HasValue)
                {
                    var count = db.ActivityRegistrations.Count(r => r.ActivityId == activityId && r.Status == "已通过");
                    if (count >= activity.MaxPeople.Value) return false;
                }

                db.ActivityRegistrations.Add(new ActivityRegistration
                {
                    ActivityId = activityId,
                    UserId = userId,
                    Status = "待审批",
                    SignedIn = false,
                    RegisteredAt = DateTime.Now
                });
                db.SaveChanges();
                return true;
            }
        }

        // 获取某个活动的待审批报名
        public List<ActivityRegistration> GetPendingRegistrations(int activityId)
        {
            using (var db = new AppDbContext())
            {
                return db.ActivityRegistrations
                    .Where(r => r.ActivityId == activityId && r.Status == "待审批")
                    .Include(r => r.User)
                    .Include(r => r.Activity)
                    .ToList();
            }
        }

        // 获取所有待审批报名（管理员）或指定社团列表的待审批报名
        public List<ActivityRegistration> GetAllPendingRegistrations(List<int> clubIds = null)
        {
            using (var db = new AppDbContext())
            {
                var query = db.ActivityRegistrations
                    .Where(r => r.Status == "待审批")
                    .Include(r => r.User)
                    .Include(r => r.Activity)
                    .AsQueryable();

                if (clubIds != null && clubIds.Count > 0)
                    query = query.Where(r => clubIds.Contains(r.Activity.ClubId));

                return query.OrderByDescending(r => r.RegisteredAt).ToList();
            }
        }

        // 审批报名
        public bool ProcessRegistration(int registrationId, bool approved)
        {
            using (var db = new AppDbContext())
            {
                var reg = db.ActivityRegistrations.Find(registrationId);
                if (reg == null || reg.Status != "待审批") return false;

                reg.Status = approved ? "已通过" : "已拒绝";
                db.SaveChanges();
                return true;
            }
        }

        // 获取某个用户的所有报名
        public List<ActivityRegistration> GetMyRegistrations(int userId)
        {
            using (var db = new AppDbContext())
            {
                return db.ActivityRegistrations
                    .Where(r => r.UserId == userId)
                    .Include(r => r.Activity.Club)
                    .OrderByDescending(r => r.RegisteredAt)
                    .ToList();
            }
        }

        public bool SignIn(int registrationId)
        {
            using (var db = new AppDbContext())
            {
                var reg = db.ActivityRegistrations.Find(registrationId);
                if (reg == null || reg.Status != "已通过") return false;

                reg.SignedIn = true;
                reg.SignInTime = DateTime.Now;
                db.SaveChanges();
                return true;
            }
        }

        public List<ActivityRegistration> GetRegistrations(int activityId)
        {
            using (var db = new AppDbContext())
            {
                return db.ActivityRegistrations
                    .Where(r => r.ActivityId == activityId)
                    .Include(r => r.User)
                    .ToList();
            }
        }
    }
}
