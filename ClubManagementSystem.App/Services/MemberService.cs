using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using ClubManagementSystem.App.Models;

namespace ClubManagementSystem.App.Services
{
    public class MemberService
    {
        // ===== 社团成员 =====

        public List<ClubMember> GetMembers(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.ClubMembers
                    .Where(m => m.ClubId == clubId)
                    .Include(m => m.User)
                    .OrderBy(m => m.Role == "社长" ? 0 : m.Role == "干事" ? 1 : 2)
                    .ThenBy(m => m.JoinedAt)
                    .ToList();
            }
        }

        public bool AssignRole(int memberId, string role, string position = null)
        {
            using (var db = new AppDbContext())
            {
                var member = db.ClubMembers.Find(memberId);
                if (member == null) return false;

                member.Role = role;
                member.Position = position;
                db.SaveChanges();
                return true;
            }
        }

        public bool TransferPresident(int clubId, int newPresidentUserId)
        {
            using (var db = new AppDbContext())
            {
                var oldPresident = db.ClubMembers
                    .FirstOrDefault(m => m.ClubId == clubId && m.Role == "社长");
                if (oldPresident != null)
                {
                    oldPresident.Role = "成员";
                    oldPresident.Position = null;
                }

                var newPresident = db.ClubMembers
                    .FirstOrDefault(m => m.ClubId == clubId && m.UserId == newPresidentUserId);
                if (newPresident != null)
                {
                    newPresident.Role = "社长";
                }

                db.SaveChanges();
                return true;
            }
        }

        public int GetMemberCount(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.ClubMembers.Count(m => m.ClubId == clubId);
            }
        }

        public bool RemoveMember(int memberId)
        {
            using (var db = new AppDbContext())
            {
                var member = db.ClubMembers.Find(memberId);
                if (member == null) return false;

                // 社长不能被普通方式移除（需管理员走特殊流程）
                if (member.Role == "社长") return false;

                db.ClubMembers.Remove(member);
                db.SaveChanges();
                return true;
            }
        }

        // 管理员强制移除（包括社长），用于删除社团最后一人
        public bool ForceRemoveMember(int memberId)
        {
            using (var db = new AppDbContext())
            {
                var member = db.ClubMembers.Find(memberId);
                if (member == null) return false;

                db.ClubMembers.Remove(member);
                db.SaveChanges();
                return true;
            }
        }

        public bool IsMember(int clubId, int userId)
        {
            using (var db = new AppDbContext())
            {
                return db.ClubMembers.Any(m => m.ClubId == clubId && m.UserId == userId);
            }
        }

        // ===== 入团申请 =====

        public bool DirectJoin(int clubId, int userId)
        {
            using (var db = new AppDbContext())
            {
                if (db.ClubMembers.Any(m => m.ClubId == clubId && m.UserId == userId))
                    return false;

                db.ClubMembers.Add(new ClubMember
                {
                    ClubId = clubId,
                    UserId = userId,
                    Role = "成员",
                    JoinedAt = DateTime.Now
                });
                db.SaveChanges();
                return true;
            }
        }

        public bool ApplyJoin(int clubId, int userId)
        {
            using (var db = new AppDbContext())
            {
                if (db.ClubMembers.Any(m => m.ClubId == clubId && m.UserId == userId))
                    return false;

                if (db.JoinApplications.Any(a => a.ClubId == clubId
                    && a.UserId == userId && a.Status == "待审批"))
                    return false;

                db.JoinApplications.Add(new JoinApplication
                {
                    ClubId = clubId,
                    UserId = userId,
                    Status = "待审批",
                    ApplyTime = DateTime.Now
                });
                db.SaveChanges();
                return true;
            }
        }

        public List<JoinApplication> GetPendingApplications(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.JoinApplications
                    .Where(a => a.ClubId == clubId && a.Status == "待审批")
                    .Include(a => a.User)
                    .OrderByDescending(a => a.ApplyTime)
                    .ToList();
            }
        }

        public List<JoinApplication> GetMyApplications(int userId)
        {
            using (var db = new AppDbContext())
            {
                return db.JoinApplications
                    .Where(a => a.UserId == userId)
                    .Include(a => a.Club)
                    .OrderByDescending(a => a.ApplyTime)
                    .ToList();
            }
        }

        public bool ProcessApplication(int applicationId, bool approved)
        {
            using (var db = new AppDbContext())
            {
                var app = db.JoinApplications.Find(applicationId);
                if (app == null || app.Status != "待审批") return false;

                app.Status = approved ? "通过" : "拒绝";
                app.ProcessTime = DateTime.Now;

                if (approved)
                {
                    if (!db.ClubMembers.Any(m => m.ClubId == app.ClubId && m.UserId == app.UserId))
                    {
                        db.ClubMembers.Add(new ClubMember
                        {
                            ClubId = app.ClubId,
                            UserId = app.UserId,
                            Role = "成员",
                            JoinedAt = DateTime.Now
                        });
                    }
                }

                db.SaveChanges();
                return true;
            }
        }
    }
}
