-- ============================================
-- 社团管理系统 - 数据库创建脚本
-- 适用于 SQL Server (SSMS 22)
-- ============================================

-- 创建数据库 (使用 SQL Server 默认数据目录)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ClubManagementDB')
BEGIN
    CREATE DATABASE ClubManagementDB
END
GO

USE ClubManagementDB
GO

-- ============================================
-- 1. 社团类别表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Categories' AND xtype='U')
CREATE TABLE Categories (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(50)  NOT NULL,
    Description NVARCHAR(200) NULL
)
GO

-- ============================================
-- 2. 用户表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
CREATE TABLE Users (
    UserId      INT IDENTITY(1,1) PRIMARY KEY,
    StudentId   NVARCHAR(20)  NOT NULL UNIQUE,
    Name        NVARCHAR(50)  NOT NULL,
    Password    NVARCHAR(100) NOT NULL,
    Phone       NVARCHAR(20)  NULL,
    Email       NVARCHAR(100) NULL,
    Avatar      NVARCHAR(200) NULL,
    Role        NVARCHAR(20)  NOT NULL DEFAULT N'普通成员',   -- 系统管理员 / 普通成员
    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE()
)
GO

-- ============================================
-- 3. 社团表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Clubs' AND xtype='U')
CREATE TABLE Clubs (
    ClubId      INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    CategoryId  INT           NULL REFERENCES Categories(Id),
    Logo        NVARCHAR(200) NULL,
    Description NVARCHAR(500) NULL,
    Contact     NVARCHAR(100) NULL,
    FounderId   INT           NOT NULL REFERENCES Users(UserId),
    Status      NVARCHAR(20)  NOT NULL DEFAULT N'正常',       -- 待审核 / 正常 / 已注销
    CanRecruit  BIT           NOT NULL DEFAULT 1,              -- 是否纳新
    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE()
)
GO

-- ============================================
-- 4. 社团成员表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ClubMembers' AND xtype='U')
CREATE TABLE ClubMembers (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClubId      INT           NOT NULL REFERENCES Clubs(ClubId),
    UserId      INT           NOT NULL REFERENCES Users(UserId),
    Role        NVARCHAR(20)  NOT NULL DEFAULT N'成员',        -- 社长 / 干事 / 成员
    Position    NVARCHAR(50)  NULL,                            -- 副社长 / 财务干事 / 宣传干事 等
    JoinedAt    DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_ClubMember UNIQUE (ClubId, UserId)
)
GO

-- ============================================
-- 5. 入团申请表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='JoinApplications' AND xtype='U')
CREATE TABLE JoinApplications (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClubId      INT           NOT NULL REFERENCES Clubs(ClubId),
    UserId      INT           NOT NULL REFERENCES Users(UserId),
    Status      NVARCHAR(20)  NOT NULL DEFAULT N'待审批',      -- 待审批 / 通过 / 拒绝
    ApplyTime   DATETIME      NOT NULL DEFAULT GETDATE(),
    ProcessTime DATETIME      NULL,
    Remark      NVARCHAR(200) NULL
)
GO

-- ============================================
-- 6. 活动表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Activities' AND xtype='U')
CREATE TABLE Activities (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClubId      INT           NOT NULL REFERENCES Clubs(ClubId),
    Title       NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    Location    NVARCHAR(100) NULL,
    StartTime   DATETIME      NOT NULL,
    EndTime     DATETIME      NOT NULL,
    MaxPeople   INT           NULL,
    Status      NVARCHAR(20)  NOT NULL DEFAULT N'报名中',      -- 报名中 / 进行中 / 已结束 / 已取消
    Summary     NVARCHAR(1000) NULL,
    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE()
)
GO

-- ============================================
-- 7. 活动报名表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ActivityRegistrations' AND xtype='U')
CREATE TABLE ActivityRegistrations (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ActivityId  INT           NOT NULL REFERENCES Activities(Id),
    UserId      INT           NOT NULL REFERENCES Users(UserId),
    SignInTime  DATETIME      NULL,
    SignedIn    BIT           NOT NULL DEFAULT 0,
    Status      NVARCHAR(20)  NOT NULL DEFAULT N'待审批',
    RegisteredAt DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_ActivityReg UNIQUE (ActivityId, UserId)
)
GO

-- ============================================
-- 8. 财务记录表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FinanceRecords' AND xtype='U')
CREATE TABLE FinanceRecords (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClubId      INT           NOT NULL REFERENCES Clubs(ClubId),
    Type        NVARCHAR(10)  NOT NULL,                        -- 收入 / 支出
    Amount      DECIMAL(10,2) NOT NULL,
    Description NVARCHAR(200) NULL,
    HandlerId   INT           NULL REFERENCES Users(UserId),
    RecordTime  DATETIME      NOT NULL DEFAULT GETDATE()
)
GO

-- ============================================
-- 9. 通知公告表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Notices' AND xtype='U')
CREATE TABLE Notices (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClubId      INT           NULL REFERENCES Clubs(ClubId),   -- NULL=全局通知
    Title       NVARCHAR(100) NOT NULL,
    Content     NVARCHAR(2000) NOT NULL,
    PublisherId INT           NOT NULL REFERENCES Users(UserId),
    PublishTime DATETIME      NOT NULL DEFAULT GETDATE()
)
GO

-- ============================================
-- 10. 物资表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Assets' AND xtype='U')
CREATE TABLE Assets (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClubId      INT           NOT NULL REFERENCES Clubs(ClubId),
    Name        NVARCHAR(100) NOT NULL,
    Quantity    INT           NOT NULL DEFAULT 0,
    Available   INT           NOT NULL DEFAULT 0
)
GO

-- ============================================
-- 11. 物资借用表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AssetBorrowings' AND xtype='U')
CREATE TABLE AssetBorrowings (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    AssetId     INT           NOT NULL REFERENCES Assets(Id),
    UserId      INT           NOT NULL REFERENCES Users(UserId),
    Quantity    INT           NOT NULL,
    BorrowTime  DATETIME      NOT NULL DEFAULT GETDATE(),
    ReturnTime  DATETIME      NULL,
    Status      NVARCHAR(20)  NOT NULL DEFAULT N'借用中'       -- 借用中 / 已归还
)
GO

-- ============================================
-- 12. 场地申请表
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='VenueApplications' AND xtype='U')
CREATE TABLE VenueApplications (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClubId      INT           NOT NULL REFERENCES Clubs(ClubId),
    Venue       NVARCHAR(100) NOT NULL,
    Purpose     NVARCHAR(200) NULL,
    StartTime   DATETIME      NOT NULL,
    EndTime     DATETIME      NOT NULL,
    Status      NVARCHAR(20)  NOT NULL DEFAULT N'待审批',      -- 待审批 / 通过 / 拒绝
    ApplicantId INT           NOT NULL REFERENCES Users(UserId),
    ApplyTime   DATETIME      NOT NULL DEFAULT GETDATE()
)
GO

-- ============================================
-- 13. 活动文件表（图片和附件）
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ActivityFiles' AND xtype='U')
CREATE TABLE ActivityFiles (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ActivityId  INT           NOT NULL REFERENCES Activities(Id),
    FileType    NVARCHAR(50)  NOT NULL,   -- image / document
    FilePath    NVARCHAR(500) NOT NULL,
    OriginalName NVARCHAR(200) NULL,
    FileSize    BIGINT        NOT NULL DEFAULT 0,
    Description NVARCHAR(200) NULL
)
GO

-- ============================================
-- 14. 公告文件表（图片和附件）
-- ============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NoticeFiles' AND xtype='U')
CREATE TABLE NoticeFiles (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    NoticeId    INT           NOT NULL REFERENCES Notices(Id),
    FileType    NVARCHAR(50)  NOT NULL,
    FilePath    NVARCHAR(500) NOT NULL,
    OriginalName NVARCHAR(200) NULL,
    FileSize    BIGINT        NOT NULL DEFAULT 0,
    Description NVARCHAR(200) NULL
)
GO

-- ============================================
-- 插入默认数据
-- ============================================

-- 默认分类
IF NOT EXISTS (SELECT * FROM Categories)
BEGIN
    INSERT INTO Categories (Name, Description) VALUES (N'学术类', N'学术研究、知识分享类社团')
    INSERT INTO Categories (Name, Description) VALUES (N'文艺类', N'文学、艺术、表演类社团')
    INSERT INTO Categories (Name, Description) VALUES (N'体育类', N'体育运动、竞技类社团')
    INSERT INTO Categories (Name, Description) VALUES (N'公益类', N'志愿服务、公益实践类社团')
    INSERT INTO Categories (Name, Description) VALUES (N'科技类', N'科技创新、编程开发类社团')
    INSERT INTO Categories (Name, Description) VALUES (N'其他', N'其他类型社团')
END
GO

-- 默认管理员 (密码: admin123)
IF NOT EXISTS (SELECT * FROM Users WHERE StudentId = 'admin')
BEGIN
    INSERT INTO Users (StudentId, Name, Password, Role)
    VALUES ('admin', N'系统管理员', 'admin123', N'系统管理员')
END
GO

PRINT N'========================================'
PRINT N'数据库 ClubManagementDB 创建完成！'
PRINT N'默认管理员账号: admin / admin123'
PRINT N'========================================'
GO

-- 活动报名表增加审批状态字段（如果数据库已建，单独执行此段）
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ActivityRegistrations' AND COLUMN_NAME = 'Status')
    PRINT N'Status 列已存在。'
ELSE
    ALTER TABLE ActivityRegistrations ADD Status NVARCHAR(20) NOT NULL DEFAULT N'已通过'
GO

-- 活动文件表（如果数据库已建，单独执行此段）
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ActivityFiles' AND xtype='U')
CREATE TABLE ActivityFiles (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ActivityId  INT           NOT NULL REFERENCES Activities(Id),
    FileType    NVARCHAR(50)  NOT NULL,
    FilePath    NVARCHAR(500) NOT NULL,
    OriginalName NVARCHAR(200) NULL,
    FileSize    BIGINT        NOT NULL DEFAULT 0,
    Description NVARCHAR(200) NULL
)
GO

-- 公告文件表（如果数据库已建，单独执行此段）
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NoticeFiles' AND xtype='U')
CREATE TABLE NoticeFiles (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    NoticeId    INT           NOT NULL REFERENCES Notices(Id),
    FileType    NVARCHAR(50)  NOT NULL,
    FilePath    NVARCHAR(500) NOT NULL,
    OriginalName NVARCHAR(200) NULL,
    FileSize    BIGINT        NOT NULL DEFAULT 0,
    Description NVARCHAR(200) NULL
)
GO

-- 社团文件表（如果数据库已建，单独执行此段）
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ClubFiles' AND xtype='U')
CREATE TABLE ClubFiles (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClubId      INT           NOT NULL REFERENCES Clubs(ClubId),
    FileType    NVARCHAR(50)  NOT NULL,
    FilePath    NVARCHAR(500) NOT NULL,
    OriginalName NVARCHAR(200) NULL,
    FileSize    BIGINT        NOT NULL DEFAULT 0,
    Description NVARCHAR(200) NULL
)
GO

-- 社团修改待审批表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ClubPendingEdits' AND xtype='U')
CREATE TABLE ClubPendingEdits (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClubId      INT           NOT NULL REFERENCES Clubs(ClubId),
    SubmittedBy INT           NOT NULL REFERENCES Users(UserId),
    NewName     NVARCHAR(100) NULL,
    CategoryId   INT           NULL,
    NewDescription NVARCHAR(500) NULL,
    NewContact  NVARCHAR(100) NULL,
    Status      NVARCHAR(20)  NOT NULL DEFAULT N'待审批',
    SubmittedAt DATETIME      NOT NULL DEFAULT GETDATE(),
    ProcessedAt DATETIME      NULL
)
GO
