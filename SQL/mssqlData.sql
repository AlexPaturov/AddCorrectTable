-- �������� ������� MATERIAL
CREATE TABLE MATERIAL (
    ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY, -- IDENTITY - ��� ������������� � MS SQL
    KODN INT NOT NULL,
    NAME NVARCHAR(100) NOT NULL
);
GO

-- ������� ���������� ������
CREATE UNIQUE INDEX IDX_MATERIAL_KODN ON MATERIAL (KODN);
GO

-- �������� ������� MATERIAL_AGGREGATED_CORRECTED
CREATE TABLE MATERIAL_AGGREGATED_CORRECTED (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    KODN INT NOT NULL,
    MASS_CORRECTED DECIMAL(15, 3),
    DAT DATE NOT NULL,
    [COMMENT] NVARCHAR(255), -- ����� COMMENT �������� �����������������, ����� ��� � []
    USERNAME NVARCHAR(50),
    CORRECTED_AT DATETIME2 DEFAULT GETDATE() -- ������������� ������ ������� �����
);
GO

--------------------------------------------------------------------------------------------------------
-- 1. �������� ������� MATERIAL_AGGREGATED_CORRECTED

-- ���������, ���������� �� �������, � ���� �� - ������� (��� ������� �������)
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

-- ������ ����� �� ���������� �������� ������������ 'WebAppUser'
GRANT SELECT, INSERT, UPDATE, DELETE, REFERENCE ON dbo.MATERIAL TO public;

-----------------------------------------------------------------------------------------------------
-- ������������� �������� �������� �� ������������� �������, 
-- ����� ������ ����� ���� ��������� ����������� ��� ������.
DROP TABLE IF EXISTS dbo.MATERIAL_DAY_VALUE;
GO

-- �������� ������� � �������������� ���������� MS SQL Server
CREATE TABLE dbo.MATERIAL_DAY_VALUE
(
  ID   BIGINT IDENTITY(1,1) NOT NULL, -- IDENTITY(1,1) �������� ������� � ���������
  KODN INT NOT NULL,                   -- INTEGER � Firebird ������ ������������� INT � MS SQL
  MASS DECIMAL(15, 3) NOT NULL,
  DAT  DATE NOT NULL,
  CONSTRAINT PK_MATERIAL_DAY_VALUE PRIMARY KEY (ID) -- ����� ���������� constraint'� - ������� ��������
);
GO

-- ������ ���� ������� ������������ ��� ���� � ���� ������
GRANT SELECT, INSERT, UPDATE, DELETE, REFERENCES 
ON dbo.MATERIAL_DAY_VALUE 
TO [YourDatabaseUser] -- �������� �� ��� ������ ������������ ��� ����
WITH GRANT OPTION;
GO