using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using ClubManagementSystem.App.Models;

namespace ClubManagementSystem.App.Services
{
    public class ClubService
    {
        public List<Club> GetAllClubs()
        {
            using (var db = new AppDbContext())
            {
                return db.Clubs
                    .Include(c => c.Category)
                    .Include(c => c.Founder)
                    .Include(c => c.ClubMembers.Select(m => m.User))
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();
            }
        }

        public Club GetClubById(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.Clubs
                    .Include(c => c.Category)
                    .Include(c => c.Founder)
                    .Include(c => c.ClubMembers.Select(m => m.User))
                    .FirstOrDefault(c => c.ClubId == clubId);
            }
        }

        public List<Club> SearchClubs(string keyword, int? categoryId = null)
        {
            using (var db = new AppDbContext())
            {
                var query = db.Clubs
                    .Include(c => c.Category)
                    .Include(c => c.Founder)
                    .Include(c => c.ClubMembers.Select(m => m.User))
                    .AsQueryable();

                if (!string.IsNullOrEmpty(keyword))
                    query = query.Where(c => c.Name.Contains(keyword) || c.Description.Contains(keyword));

                if (categoryId.HasValue && categoryId.Value > 0)
                    query = query.Where(c => c.CategoryId == categoryId.Value);

                return query.OrderByDescending(c => c.CreatedAt).ToList();
            }
        }

        public Club CreateClub(Club club, int founderId, string founderRole)
        {
            using (var db = new AppDbContext())
            {
                club.FounderId = founderId;
                club.Status = "正常";
                club.CreatedAt = DateTime.Now;
                db.Clubs.Add(club);
                db.SaveChanges();

                // 只有普通用户创建社团时自动设为社长，管理员不加入
                if (founderRole != "系统管理员")
                {
                    db.ClubMembers.Add(new ClubMember
                    {
                        ClubId = club.ClubId,
                        UserId = founderId,
                        Role = "社长",
                        JoinedAt = DateTime.Now
                    });
                    db.SaveChanges();
                }

                return club;
            }
        }

        public bool UpdateClub(Club club)
        {
            using (var db = new AppDbContext())
            {
                var existing = db.Clubs.Find(club.ClubId);
                if (existing == null) return false;

                existing.Name = club.Name;
                existing.CategoryId = club.CategoryId;
                existing.Logo = club.Logo;
                existing.Description = club.Description;
                existing.Contact = club.Contact;
                existing.CanRecruit = club.CanRecruit;
                db.SaveChanges();
                return true;
            }
        }

        public bool DisbandClub(int clubId)
        {
            using (var db = new AppDbContext())
            {
                var club = db.Clubs.Find(clubId);
                if (club == null) return false;

                club.Status = "已注销";
                club.CanRecruit = false;
                db.SaveChanges();
                return true;
            }
        }

        public bool DeleteClub(int clubId)
        {
            using (var db = new AppDbContext())
            {
                var club = db.Clubs.Find(clubId);
                if (club == null) return false;

                // 级联删除相关数据
                var members = db.ClubMembers.Where(m => m.ClubId == clubId);
                db.ClubMembers.RemoveRange(members);

                var applications = db.JoinApplications.Where(a => a.ClubId == clubId);
                db.JoinApplications.RemoveRange(applications);

                db.Clubs.Remove(club);
                db.SaveChanges();
                return true;
            }
        }

        public List<Category> GetAllCategories()
        {
            using (var db = new AppDbContext())
            {
                return db.Categories.OrderBy(c => c.Name).ToList();
            }
        }

        public Category AddCategory(string name, string description)
        {
            using (var db = new AppDbContext())
            {
                var category = new Category { Name = name, Description = description };
                db.Categories.Add(category);
                db.SaveChanges();
                return category;
            }
        }

        public bool DeleteCategory(int categoryId)
        {
            using (var db = new AppDbContext())
            {
                var category = db.Categories.Find(categoryId);
                if (category == null) return false;
                db.Categories.Remove(category);
                db.SaveChanges();
                return true;
            }
        }

        public int GetClubMemberCount(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.ClubMembers.Count(m => m.ClubId == clubId);
            }
        }

        public List<Club> GetMyClubs(int userId)
        {
            using (var db = new AppDbContext())
            {
                return db.ClubMembers
                    .Where(m => m.UserId == userId)
                    .Include(m => m.Club.Category)
                    .Select(m => m.Club)
                    .ToList();
            }
        }
    }
}
