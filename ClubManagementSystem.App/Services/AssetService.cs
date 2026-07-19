using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using ClubManagementSystem.App.Models;

namespace ClubManagementSystem.App.Services
{
    public class AssetService
    {
        // ===== 物资管理 =====

        public List<Asset> GetAssets(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.Assets
                    .Where(a => a.ClubId == clubId)
                    .OrderBy(a => a.Name)
                    .ToList();
            }
        }

        public Asset AddAsset(Asset asset)
        {
            using (var db = new AppDbContext())
            {
                asset.Available = asset.Quantity;
                db.Assets.Add(asset);
                db.SaveChanges();
                return asset;
            }
        }

        public bool UpdateAssetQuantity(int assetId, int delta)
        {
            using (var db = new AppDbContext())
            {
                var asset = db.Assets.Find(assetId);
                if (asset == null) return false;

                asset.Quantity += delta;
                asset.Available += delta;
                if (asset.Available < 0) asset.Available = 0;
                if (asset.Quantity < 0) return false;

                db.SaveChanges();
                return true;
            }
        }

        public bool DeleteAsset(int assetId)
        {
            using (var db = new AppDbContext())
            {
                var asset = db.Assets.Find(assetId);
                if (asset == null) return false;

                db.Assets.Remove(asset);
                db.SaveChanges();
                return true;
            }
        }

        // ===== 物资借用 =====

        public List<AssetBorrowing> GetBorrowings(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.AssetBorrowings
                    .Where(b => b.Asset.ClubId == clubId)
                    .Include(b => b.Asset)
                    .Include(b => b.User)
                    .OrderByDescending(b => b.BorrowTime)
                    .ToList();
            }
        }

        public AssetBorrowing BorrowAsset(int assetId, int userId, int quantity)
        {
            using (var db = new AppDbContext())
            {
                var asset = db.Assets.Find(assetId);
                if (asset == null || asset.Available < quantity) return null;

                asset.Available -= quantity;

                var borrowing = new AssetBorrowing
                {
                    AssetId = assetId,
                    UserId = userId,
                    Quantity = quantity,
                    BorrowTime = DateTime.Now,
                    Status = "借用中"
                };
                db.AssetBorrowings.Add(borrowing);
                db.SaveChanges();
                return borrowing;
            }
        }

        public bool ReturnAsset(int borrowingId)
        {
            using (var db = new AppDbContext())
            {
                var borrowing = db.AssetBorrowings
                    .Include(b => b.Asset)
                    .FirstOrDefault(b => b.Id == borrowingId);

                if (borrowing == null || borrowing.Status == "已归还") return false;

                borrowing.Status = "已归还";
                borrowing.ReturnTime = DateTime.Now;
                borrowing.Asset.Available += borrowing.Quantity;

                db.SaveChanges();
                return true;
            }
        }

        // ===== 场地申请 =====

        public List<VenueApplication> GetVenueApplications(int clubId)
        {
            using (var db = new AppDbContext())
            {
                return db.VenueApplications
                    .Where(v => v.ClubId == clubId)
                    .Include(v => v.Applicant)
                    .Include(v => v.Club)
                    .OrderByDescending(v => v.ApplyTime)
                    .ToList();
            }
        }

        public VenueApplication ApplyVenue(VenueApplication application)
        {
            using (var db = new AppDbContext())
            {
                application.ApplyTime = DateTime.Now;
                application.Status = "待审批";
                db.VenueApplications.Add(application);
                db.SaveChanges();
                return application;
            }
        }

        public bool ProcessVenueApplication(int applicationId, bool approved)
        {
            using (var db = new AppDbContext())
            {
                var app = db.VenueApplications.Find(applicationId);
                if (app == null) return false;

                app.Status = approved ? "通过" : "拒绝";
                db.SaveChanges();
                return true;
            }
        }
    }
}
