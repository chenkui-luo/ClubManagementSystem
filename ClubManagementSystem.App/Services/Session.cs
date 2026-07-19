using System.Collections.Generic;
using System.Linq;
using ClubManagementSystem.App.Models;

namespace ClubManagementSystem.App.Services
{
    public static class Session
    {
        public static User CurrentUser { get; set; }

        // 系统角色: "系统管理员" 或 "用户"
        public static string SystemRole => CurrentUser?.Role ?? "用户";

        // 是否有某个社团的身份
        public static bool IsPresidentOf(int clubId) =>
            CurrentUser?.ClubMembers?.Any(m => m.ClubId == clubId && m.Role == "社长") ?? false;

        public static bool IsOfficerOf(int clubId) =>
            CurrentUser?.ClubMembers?.Any(m => m.ClubId == clubId && m.Role == "干事") ?? false;

        public static bool IsMemberOf(int clubId) =>
            CurrentUser?.ClubMembers?.Any(m => m.ClubId == clubId) ?? false;

        // 我管理的社团(社长或干事) ID 列表
        public static List<int> MyManagedClubIds =>
            CurrentUser?.ClubMembers
                ?.Where(m => m.Role == "社长" || m.Role == "干事")
                ?.Select(m => m.ClubId)
                ?.ToList() ?? new List<int>();

        // 我管理的社团ID(社长)列表 — 只有社长的
        public static List<int> MyPresidentClubIds =>
            CurrentUser?.ClubMembers
                ?.Where(m => m.Role == "社长")
                ?.Select(m => m.ClubId)
                ?.ToList() ?? new List<int>();

        // 我参与的所有社团ID
        public static List<int> MyAllClubIds =>
            CurrentUser?.ClubMembers?.Select(m => m.ClubId).ToList() ?? new List<int>();

        // 当前身份标签（显示在右上角）
        public static string EffectiveRoleLabel
        {
            get
            {
                if (SystemRole == "系统管理员") return "系统管理员";
                if (CurrentUser?.ClubMembers?.Any(m => m.Role == "社长") == true) return "社长";
                if (CurrentUser?.ClubMembers?.Any(m => m.Role == "干事") == true) return "干事";
                if (CurrentUser?.ClubMembers?.Any() == true) return "普通成员";
                return "游客";
            }
        }

        public static bool IsAdmin => SystemRole == "系统管理员";
        public static bool IsVisitor => !IsAdmin && (CurrentUser?.ClubMembers?.Count ?? 0) == 0;
    }
}
