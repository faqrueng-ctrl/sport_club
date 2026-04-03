using SportClubApp.Data;
using SportClubApp.Models;

namespace SportClubApp.Services
{
    public sealed class DatabaseInitializer
    {
        public void EnsureCreated()
        {
            using (var connection = Db.OpenConnection())
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
IF OBJECT_ID('dbo.Roles','U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles(
      Id INT NOT NULL PRIMARY KEY,
      Name NVARCHAR(30) NOT NULL UNIQUE
    );
END

IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE Id = 1) INSERT INTO dbo.Roles(Id,Name) VALUES(1,N'Пользователь');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE Id = 2) INSERT INTO dbo.Roles(Id,Name) VALUES(2,N'Менеджер');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE Id = 3) INSERT INTO dbo.Roles(Id,Name) VALUES(3,N'Администратор');

IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE dbo.Users(
      Id INT IDENTITY PRIMARY KEY,
      FullName NVARCHAR(120) NOT NULL,
      Email NVARCHAR(120) NOT NULL UNIQUE,
      Phone NVARCHAR(40) NOT NULL UNIQUE,
      [Password] NVARCHAR(100) NOT NULL,
      RoleId INT NOT NULL DEFAULT 1 REFERENCES dbo.Roles(Id),
      CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
END

IF COL_LENGTH('dbo.Users','RoleId') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD RoleId INT NULL;
    UPDATE dbo.Users
    SET RoleId = CASE
        WHEN ISNULL([Role], N'Пользователь') = N'Администратор' THEN 3
        WHEN ISNULL([Role], N'Пользователь') = N'Менеджер' THEN 2
        ELSE 1 END
    WHERE RoleId IS NULL;
END

IF COL_LENGTH('dbo.Users','Password') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD [Password] NVARCHAR(100) NULL;
    UPDATE dbo.Users
    SET [Password] = CASE WHEN ISNULL(RoleId,1) = 3 THEN N'admin' ELSE N'123456' END
    WHERE [Password] IS NULL;
END

IF OBJECT_ID('dbo.Products','U') IS NULL
BEGIN
    CREATE TABLE dbo.Products(
      Id INT IDENTITY PRIMARY KEY,
      Name NVARCHAR(150) NOT NULL,
      Category NVARCHAR(80) NOT NULL,
      Price DECIMAL(18,2) NOT NULL,
      OldPrice DECIMAL(18,2) NULL,
      DiscountPercent INT NULL,
      ImagePath NVARCHAR(260) NULL,
      StockQty INT NOT NULL DEFAULT 0
    );
END

IF OBJECT_ID('dbo.CartItems','U') IS NULL
BEGIN
    CREATE TABLE dbo.CartItems(
      Id INT IDENTITY PRIMARY KEY,
      UserId INT NOT NULL REFERENCES dbo.Users(Id),
      ProductId INT NOT NULL REFERENCES dbo.Products(Id),
      Quantity INT NOT NULL CHECK (Quantity > 0)
    );
END

IF OBJECT_ID('dbo.Orders','U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders(
      Id INT IDENTITY PRIMARY KEY,
      UserId INT NOT NULL REFERENCES dbo.Users(Id),
      CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
      Total DECIMAL(18,2) NOT NULL,
      Status NVARCHAR(30) NOT NULL DEFAULT N'Новый'
    );
END

IF OBJECT_ID('dbo.OrderItems','U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems(
      Id INT IDENTITY PRIMARY KEY,
      OrderId INT NOT NULL REFERENCES dbo.Orders(Id),
      ProductId INT NOT NULL REFERENCES dbo.Products(Id),
      Price DECIMAL(18,2) NOT NULL,
      Quantity INT NOT NULL CHECK (Quantity > 0)
    );
END

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE RoleId = 3)
BEGIN
    INSERT INTO dbo.Users (FullName,Email,Phone,[Password],RoleId)
    VALUES (N'Главный Администратор',N'admin@sportclub.local',N'+70000000000',N'admin',3);
END

IF NOT EXISTS(SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products(Name,Category,Price,OldPrice,DiscountPercent,StockQty)
    VALUES
    (N'Персональная тренировка',N'Услуги',1500,2000,25,100),
    (N'Групповая йога',N'Услуги',800,NULL,NULL,200),
    (N'Спорт-питание',N'Товары',1200,1400,15,50);
END";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
