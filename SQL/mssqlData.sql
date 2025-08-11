-- —оздание таблицы MATERIAL
CREATE TABLE MATERIAL (
    ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY, -- IDENTITY - это автоинкремент в MS SQL
    KODN INT NOT NULL,
    NAME NVARCHAR(100) NOT NULL
);
GO

-- —оздаем уникальный индекс
CREATE UNIQUE INDEX IDX_MATERIAL_KODN ON MATERIAL (KODN);
GO

-- —оздание таблицы MATERIAL_AGGREGATED_CORRECTED
CREATE TABLE MATERIAL_AGGREGATED_CORRECTED (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    KODN INT NOT NULL,
    MASS_CORRECTED DECIMAL(15, 3),
    DAT DATE NOT NULL,
    [COMMENT] NVARCHAR(255), -- —лово COMMENT €вл€етс€ зарезервированным, берем его в []
    USERNAME NVARCHAR(50),
    CORRECTED_AT DATETIME2 DEFAULT GETDATE() -- јвтоматически ставим текущее врем€
);
GO

--------------------------------------------------------------------------------------------------------
-- 1. —оздание таблицы MATERIAL_AGGREGATED_CORRECTED

-- ѕровер€ем, существует ли таблица, и если да - удал€ем (дл€ чистого запуска)
IF OBJECT_ID('dbo.MATERIAL_AGGREGATED_CORRECTED', 'U') IS NOT NULL
  DROP TABLE dbo.MATERIAL_AGGREGATED_CORRECTED;
GO

CREATE TABLE dbo.MATERIAL_AGGREGATED_CORRECTED
(
  ID             BIGINT IDENTITY(1,1) NOT NULL,
  KODN           INT NOT NULL,
  MASS_CORRECTED DECIMAL(15, 3),
  DAT            DATE NOT NULL,
  [COMMENT]      NVARCHAR(255),
  USERNAME       NVARCHAR(50),
  CORRECTED_AT   DATETIME2 DEFAULT GETDATE(),
  CONSTRAINT PK_MATERIAL_AGGREGATED_CORRECTED PRIMARY KEY (ID)
);
GO

-- ¬ыдаем права на выполнение операций пользователю 'WebAppUser'
GRANT SELECT, INSERT, UPDATE, DELETE, REFERENCE ON dbo.MATERIAL TO public;

-----------------------------------------------------------------------------------------------------
-- –екомендуетс€ добавить проверку на существование таблицы, 
-- чтобы скрипт можно было выполн€ть многократно без ошибок.
DROP TABLE IF EXISTS dbo.MATERIAL_DAY_VALUE;
GO

-- —оздание таблицы с использованием синтаксиса MS SQL Server
CREATE TABLE dbo.MATERIAL_DAY_VALUE
(
  ID   BIGINT IDENTITY(1,1) NOT NULL, -- IDENTITY(1,1) замен€ет триггер и генератор
  KODN INT NOT NULL,                   -- INTEGER в Firebird обычно соответствует INT в MS SQL
  MASS DECIMAL(15, 3) NOT NULL,
  DAT  DATE NOT NULL,
  CONSTRAINT PK_MATERIAL_DAY_VALUE PRIMARY KEY (ID) -- явное именование constraint'а - хороша€ практика
);
GO

-- ¬ыдача прав доступа пользователю или роли в базе данных
GRANT SELECT, INSERT, UPDATE, DELETE, REFERENCES 
ON dbo.MATERIAL_DAY_VALUE 
TO [YourDatabaseUser] -- «амените на им€ вашего пользовател€ или роли
WITH GRANT OPTION;
GO