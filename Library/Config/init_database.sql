-- LOA-Server Database Initialization Script
-- Database: mud

-- Server table
CREATE TABLE IF NOT EXISTS Server (
    id VARCHAR(50) PRIMARY KEY,
    name INT DEFAULT 0,
    ip VARCHAR(100),
    port INT DEFAULT 19881
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Device table
CREATE TABLE IF NOT EXISTS Device (
    Id VARCHAR(100) PRIMARY KEY,
    Player VARCHAR(100),
    Platform INT DEFAULT 0,
    PreferredLanguage INT DEFAULT 0,
    Activitys TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Player table
CREATE TABLE IF NOT EXISTS Player (
    Id VARCHAR(100) PRIMARY KEY,
    Pos TEXT,
    Record TEXT,
    Text TEXT,
    Grade TEXT,
    Equipments TEXT,
    Quest TEXT,
    Skills TEXT,
    Payments TEXT,
    Warehouses TEXT,
    Parts TEXT,
    Time TEXT,
    Activitys TEXT,
    Signs TEXT,
    Merchandises TEXT,
    Companions TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Card table
CREATE TABLE IF NOT EXISTS Card (
    id VARCHAR(100) PRIMARY KEY,
    value INT DEFAULT 0,
    utilized DATETIME
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Insert default server for development
INSERT INTO Server (id, name, ip, port) VALUES ('DEV01', 10001, '127.0.0.1', 19881)
ON DUPLICATE KEY UPDATE ip = '127.0.0.1', port = 19881;
