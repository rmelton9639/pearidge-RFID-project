-- ============================================================================
-- PEA RIDGE DOG GROOMING - DATABASE SETUP SCRIPT
-- MySQL Database Initialization
-- Version: 1.0
-- ============================================================================

-- ============================================================================
-- STEP 1: CREATE DATABASE
-- ============================================================================

CREATE DATABASE IF NOT EXISTS rfid_database 
    CHARACTER SET utf8mb4 
    COLLATE utf8mb4_unicode_ci;

USE rfid_database;

-- ============================================================================
-- STEP 2: CREATE CUSTOMER/TAG TABLE
-- ============================================================================

CREATE TABLE IF NOT EXISTS rfid_tags (
    id INT AUTO_INCREMENT PRIMARY KEY,
    tag_id VARCHAR(255) UNIQUE NOT NULL,
    customer_name VARCHAR(255),
    dogs_name VARCHAR(255),
    dogs_breed VARCHAR(255),
    vehicle_make_color TEXT,
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_seen TIMESTAMP NULL,
    
    -- Indexes for performance
    INDEX idx_customer_name (customer_name),
    INDEX idx_dogs_name (dogs_name),
    INDEX idx_last_seen (last_seen)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- STEP 3: CREATE ZONE TRACKING TABLE
-- ============================================================================

CREATE TABLE IF NOT EXISTS tag_reads (
    id INT AUTO_INCREMENT PRIMARY KEY,
    tag_id VARCHAR(255) NOT NULL,
    zone_number INT NOT NULL,
    zone_name VARCHAR(100) NOT NULL,
    pi_id VARCHAR(50) NOT NULL,
    pi_ip VARCHAR(15) NOT NULL,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Indexes for performance
    INDEX idx_tag_id (tag_id),
    INDEX idx_timestamp (timestamp),
    INDEX idx_zone (zone_number),
    INDEX idx_tag_zone (tag_id, zone_number),
    INDEX idx_tag_time (tag_id, timestamp)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- STEP 4: CREATE USEFUL VIEWS
-- ============================================================================

-- View: Latest location for each tag
CREATE OR REPLACE VIEW latest_tag_locations AS
SELECT 
    t1.tag_id,
    t1.zone_number,
    t1.zone_name,
    t1.pi_id,
    t1.timestamp AS last_read_time,
    rt.customer_name,
    rt.dogs_name,
    rt.dogs_breed,
    rt.vehicle_make_color
FROM tag_reads t1
INNER JOIN (
    SELECT tag_id, MAX(timestamp) as max_timestamp
    FROM tag_reads
    GROUP BY tag_id
) t2 ON t1.tag_id = t2.tag_id AND t1.timestamp = t2.max_timestamp
LEFT JOIN rfid_tags rt ON t1.tag_id = rt.tag_id
ORDER BY t1.timestamp DESC;

-- View: Dogs currently in facility (read within last 8 hours)
CREATE OR REPLACE VIEW current_dogs_in_facility AS
SELECT 
    rt.tag_id,
    rt.customer_name,
    rt.dogs_name,
    rt.dogs_breed,
    rt.vehicle_make_color,
    t1.zone_number,
    t1.zone_name,
    t1.timestamp AS current_zone_entry_time,
    TIMESTAMPDIFF(MINUTE, t1.timestamp, NOW()) AS minutes_in_current_zone
FROM tag_reads t1
INNER JOIN (
    SELECT tag_id, MAX(timestamp) as max_timestamp
    FROM tag_reads
    WHERE timestamp >= DATE_SUB(NOW(), INTERVAL 8 HOUR)
    GROUP BY tag_id
) t2 ON t1.tag_id = t2.tag_id AND t1.timestamp = t2.max_timestamp
LEFT JOIN rfid_tags rt ON t1.tag_id = rt.tag_id
ORDER BY t1.timestamp DESC;

-- View: Zone activity summary
CREATE OR REPLACE VIEW zone_activity_summary AS
SELECT 
    zone_number,
    zone_name,
    COUNT(*) AS total_reads,
    COUNT(DISTINCT tag_id) AS unique_dogs,
    MAX(timestamp) AS last_activity,
    DATE(timestamp) AS activity_date
FROM tag_reads
WHERE DATE(timestamp) = CURDATE()
GROUP BY zone_number, zone_name, DATE(timestamp)
ORDER BY zone_number;

-- ============================================================================
-- STEP 5: INSERT SAMPLE DATA (OPTIONAL - REMOVE BEFORE PRODUCTION)
-- ============================================================================

-- Sample customers (for testing only)
INSERT IGNORE INTO rfid_tags 
(tag_id, customer_name, dogs_name, dogs_breed, vehicle_make_color, notes)
VALUES 
('E20034120000001D00000000', 'Scott Melton', 'Buddy', 'Golden Retriever', 'Black Toyota Camry', 'VIP Customer - Likes extra brushing'),
('E20034120000001D00000001', 'Whitney Kinnaman', 'Luna', 'Border Collie', 'White Ford F-150', 'Premium Member - Sensitive skin'),
('E20034120000001D00000002', 'Mike Davis', 'Max', 'German Shepherd', 'Blue Dodge Ram', 'Regular Customer - Good with other dogs');

-- ⚠️ REMOVE SAMPLE DATA BEFORE PRODUCTION DEPLOYMENT ⚠️
-- Run this command to remove sample data:
-- DELETE FROM rfid_tags WHERE tag_id LIKE 'E20034120000001D%';

-- ============================================================================
-- STEP 6: CREATE STORED PROCEDURES (OPTIONAL - FOR ADVANCED USE)
-- ============================================================================

-- Procedure: Get dog movement history
DELIMITER //
CREATE PROCEDURE IF NOT EXISTS GetDogMovementHistory(IN input_tag_id VARCHAR(255))
BEGIN
    SELECT 
        tr.zone_number,
        tr.zone_name,
        tr.timestamp AS entered_time,
        LEAD(tr.timestamp) OVER (PARTITION BY tr.tag_id ORDER BY tr.timestamp) AS exited_time,
        TIMESTAMPDIFF(MINUTE, 
            tr.timestamp, 
            LEAD(tr.timestamp) OVER (PARTITION BY tr.tag_id ORDER BY tr.timestamp)
        ) AS minutes_in_zone
    FROM tag_reads tr
    WHERE tr.tag_id = input_tag_id
    ORDER BY tr.timestamp;
END //
DELIMITER ;

-- Procedure: Get zone statistics for date range
DELIMITER //
CREATE PROCEDURE IF NOT EXISTS GetZoneStatistics(
    IN start_date DATE,
    IN end_date DATE
)
BEGIN
    SELECT 
        zone_number,
        zone_name,
        COUNT(*) AS total_reads,
        COUNT(DISTINCT tag_id) AS unique_dogs,
        MIN(timestamp) AS first_read,
        MAX(timestamp) AS last_read,
        ROUND(COUNT(*) / COUNT(DISTINCT DATE(timestamp)), 2) AS avg_reads_per_day
    FROM tag_reads
    WHERE DATE(timestamp) BETWEEN start_date AND end_date
    GROUP BY zone_number, zone_name
    ORDER BY zone_number;
END //
DELIMITER ;

-- ============================================================================
-- STEP 7: USEFUL QUERIES FOR REPORTING
-- ============================================================================

-- Query: Most active customers (top 10)
-- SELECT 
--     rt.customer_name,
--     rt.dogs_name,
--     COUNT(*) AS total_visits,
--     MAX(tr.timestamp) AS last_visit
-- FROM tag_reads tr
-- JOIN rfid_tags rt ON tr.tag_id = rt.tag_id
-- GROUP BY rt.customer_name, rt.dogs_name
-- ORDER BY total_visits DESC
-- LIMIT 10;

-- Query: Average time spent in each zone
-- SELECT 
--     zone_name,
--     AVG(minutes_in_zone) AS avg_minutes
-- FROM (
--     SELECT 
--         zone_name,
--         tag_id,
--         timestamp AS entry_time,
--         LEAD(timestamp) OVER (PARTITION BY tag_id ORDER BY timestamp) AS exit_time,
--         TIMESTAMPDIFF(MINUTE, 
--             timestamp,
--             LEAD(timestamp) OVER (PARTITION BY tag_id ORDER BY timestamp)
--         ) AS minutes_in_zone
--     FROM tag_reads
-- ) AS zone_durations
-- WHERE minutes_in_zone IS NOT NULL
-- GROUP BY zone_name;

-- Query: Daily customer count
-- SELECT 
--     DATE(timestamp) AS visit_date,
--     COUNT(DISTINCT tag_id) AS unique_customers
-- FROM tag_reads
-- WHERE timestamp >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)
-- GROUP BY DATE(timestamp)
-- ORDER BY visit_date DESC;

-- ============================================================================
-- STEP 8: CREATE DATABASE USER (OPTIONAL - FOR SECURITY)
-- ============================================================================

-- Create dedicated user for the application (more secure than using root)
-- CREATE USER IF NOT EXISTS 'rfid_app'@'localhost' IDENTIFIED BY 'YourSecurePassword123!';
-- GRANT SELECT, INSERT, UPDATE ON rfid_database.* TO 'rfid_app'@'localhost';
-- FLUSH PRIVILEGES;

-- Then update connection string in C# application:
-- Server=localhost;Database=rfid_database;Uid=rfid_app;Pwd=YourSecurePassword123!;

-- ============================================================================
-- STEP 9: MAINTENANCE TASKS
-- ============================================================================

-- Archive old tag reads (keep last 90 days)
-- Run this monthly to keep database size manageable
-- CREATE EVENT IF NOT EXISTS archive_old_reads
-- ON SCHEDULE EVERY 1 MONTH
-- DO
--     DELETE FROM tag_reads 
--     WHERE timestamp < DATE_SUB(NOW(), INTERVAL 90 DAY);

-- Optimize tables (run monthly)
-- OPTIMIZE TABLE rfid_tags;
-- OPTIMIZE TABLE tag_reads;

-- ============================================================================
-- STEP 10: BACKUP COMMANDS
-- ============================================================================

-- To backup database (run from command line):
-- mysqldump -u root -p rfid_database > backup_YYYYMMDD.sql

-- To restore database (run from command line):
-- mysql -u root -p rfid_database < backup_YYYYMMDD.sql

-- To backup only structure (no data):
-- mysqldump -u root -p --no-data rfid_database > structure_only.sql

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- Verify tables created
SHOW TABLES;

-- Verify rfid_tags table structure
DESCRIBE rfid_tags;

-- Verify tag_reads table structure
DESCRIBE tag_reads;

-- Verify views created
SHOW FULL TABLES WHERE Table_type = 'VIEW';

-- Check sample data (if inserted)
SELECT COUNT(*) AS customer_count FROM rfid_tags;

-- ============================================================================
-- DATABASE SETUP COMPLETE!
-- ============================================================================

SELECT 'Database setup completed successfully!' AS Status;
SELECT 'Ready for Pea Ridge RFID System' AS Message;

-- ============================================================================
-- NOTES
-- ============================================================================

-- This script creates:
--   ✓ Database: rfid_database
--   ✓ Table: rfid_tags (customer information)
--   ✓ Table: tag_reads (zone tracking history)
--   ✓ Views: latest_tag_locations, current_dogs_in_facility, zone_activity_summary
--   ✓ Stored procedures: GetDogMovementHistory, GetZoneStatistics
--   ✓ Indexes for optimal query performance

-- Connection string for C# application:
--   Server=localhost;Database=rfid_database;Uid=root;Pwd=Sc0ttyM3lt0n!;

-- Zone mapping:
--   Zone 1: Bathing (pr-rpi001, port 5001)
--   Zone 2: Drying (pr-rpi001, port 5002)
--   Zone 3: Grooming Room 1 (pr-rpi001, port 5003)
--   Zone 4: Grooming Room 2 (pr-rpi001, port 5004)
--   Zone 5: Entryway (pr-rpi002, port 5005)
--   Zone 6: Waiting Kennels (pr-rpi002, port 5006)
--   Zone 7: Play Yard (pr-rpi002, port 5007)
--   Zone 8: Pickup Kennel (pr-rpi002, port 5008)

-- ============================================================================
