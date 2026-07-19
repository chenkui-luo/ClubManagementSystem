using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using ClubManagementSystem.App.Models;

namespace ClubManagementSystem.App.Services
{
    public class FinanceService
    {
        public List<FinanceRecord> GetRecords(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.FinanceRecords
                    .Where(r => r.ClubId == clubId)
                    .Include(r => r.Handler)
                    .OrderByDescending(r => r.RecordTime)
                    .ToList();
            }
        }

        public FinanceRecord AddRecord(FinanceRecord record)
        {
            using (var db = new AppDbContext())
            {
                record.RecordTime = DateTime.Now;
                db.FinanceRecords.Add(record);
                db.SaveChanges();
                return record;
            }
        }

        public bool DeleteRecord(int recordId)
        {
            using (var db = new AppDbContext())
            {
                var record = db.FinanceRecords.Find(recordId);
                if (record == null) return false;

                db.FinanceRecords.Remove(record);
                db.SaveChanges();
                return true;
            }
        }

        public decimal GetBalance(int clubId)
        {
            using (var db = new AppDbContext())
            {
                var income = db.FinanceRecords
                    .Where(r => r.ClubId == clubId && r.Type == "收入")
                    .Sum(r => (decimal?)r.Amount) ?? 0;

                var expense = db.FinanceRecords
                    .Where(r => r.ClubId == clubId && r.Type == "支出")
                    .Sum(r => (decimal?)r.Amount) ?? 0;

                return income - expense;
            }
        }

        public (decimal income, decimal expense, decimal balance) GetSummary(int clubId, DateTime? start, DateTime? end)
        {
            using (var db = new AppDbContext())
            {
                var query = db.FinanceRecords.Where(r => r.ClubId == clubId);
                if (start.HasValue)
                    query = query.Where(r => r.RecordTime >= start.Value);
                if (end.HasValue)
                    query = query.Where(r => r.RecordTime <= end.Value);

                var income = query.Where(r => r.Type == "收入").Sum(r => (decimal?)r.Amount) ?? 0;
                var expense = query.Where(r => r.Type == "支出").Sum(r => (decimal?)r.Amount) ?? 0;

                return (income, expense, income - expense);
            }
        }
    }
}
