using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ClubManagementSystem.App.Models;

namespace ClubManagementSystem.App.Services
{
    public class UserService
    {
        public User Login(string studentId, string password)
        {
            using (var db = new AppDbContext())
            {
                return db.Users
                    .Include(u => u.ClubMembers)
                    .FirstOrDefault(u =>
                        u.StudentId == studentId && u.Password == password);
            }
        }

        public User ReloadWithMemberships(int userId)
        {
            using (var db = new AppDbContext())
            {
                return db.Users
                    .Include(u => u.ClubMembers.Select(m => m.Club))
                    .FirstOrDefault(u => u.UserId == userId);
            }
        }

        public User Register(User user)
        {
            using (var db = new AppDbContext())
            {
                if (db.Users.Any(u => u.StudentId == user.StudentId))
                    return null;

                user.CreatedAt = DateTime.Now;
                user.Role = "用户";
                db.Users.Add(user);
                db.SaveChanges();
                return user;
            }
        }

        public List<User> GetAllUsers()
        {
            using (var db = new AppDbContext())
            {
                return db.Users.OrderByDescending(u => u.CreatedAt).ToList();
            }
        }

        public List<User> SearchUsers(string keyword)
        {
            using (var db = new AppDbContext())
            {
                return db.Users.Where(u =>
                    u.Name.Contains(keyword) || u.StudentId.Contains(keyword))
                    .ToList();
            }
        }

        public bool UpdateUser(User user)
        {
            using (var db = new AppDbContext())
            {
                var existing = db.Users.Find(user.UserId);
                if (existing == null) return false;

                existing.Name = user.Name;
                existing.Phone = user.Phone;
                existing.Email = user.Email;
                existing.Avatar = user.Avatar;
                db.SaveChanges();
                return true;
            }
        }

        public bool ChangePassword(int userId, string oldPwd, string newPwd)
        {
            using (var db = new AppDbContext())
            {
                var user = db.Users.Find(userId);
                if (user == null || user.Password != oldPwd) return false;

                user.Password = newPwd;
                db.SaveChanges();
                return true;
            }
        }

        public bool DeleteUser(int userId)
        {
            using (var db = new AppDbContext())
            {
                var user = db.Users.Find(userId);
                if (user == null) return false;

                db.Users.Remove(user);
                db.SaveChanges();
                return true;
            }
        }
    }
}
