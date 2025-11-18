-- ==============================================
-- 1️⃣ USERS TABLE
-- ==============================================
CREATE TABLE `users` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  `email` VARCHAR(100) NOT NULL,
  `password` VARCHAR(300) NOT NULL,
  `role_id` INT NOT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_users_email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 2️⃣ ACCOUNTS TABLE
-- ==============================================
CREATE TABLE `accounts` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `platform_name` VARCHAR(100) NOT NULL,
  `platform_path` VARCHAR(255) DEFAULT NULL,
  `account_number` INT NOT NULL,
  `broker_name` VARCHAR(100) NOT NULL,
  `server_name` VARCHAR(100) NOT NULL,
  `user_id` INT NOT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_user_id` (`user_id`),
  CONSTRAINT `fk_accounts_user` FOREIGN KEY (`user_id`)
    REFERENCES `users` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 3️⃣ MASTER-SLAVE RELATIONSHIP TABLE
-- ==============================================
CREATE TABLE `master_slave` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  `master_id` INT NOT NULL,
  `slave_id` INT NOT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_master_id` (`master_id`),
  KEY `idx_slave_id` (`slave_id`),
  CONSTRAINT `fk_master_slave_master` FOREIGN KEY (`master_id`)
    REFERENCES `accounts` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_master_slave_slave` FOREIGN KEY (`slave_id`)
    REFERENCES `accounts` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 4️⃣ MASTER-SLAVE CONFIG TABLE
-- ==============================================
CREATE TABLE `master_slave_config` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `master_slave_id` INT NOT NULL,
  `multiplier` DECIMAL(10,4) NOT NULL DEFAULT '1.0000',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_master_slave_id` (`master_slave_id`),
  CONSTRAINT `fk_master_slave_config_master_slave` FOREIGN KEY (`master_slave_id`)
    REFERENCES `master_slave` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 5️⃣ MASTER-SLAVE PAIR TABLE
-- ==============================================
CREATE TABLE `master_slave_pair` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `master_slave_id` INT NOT NULL,
  `master_pair` VARCHAR(10) NOT NULL,
  `slave_pair` VARCHAR(10) NOT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_master_slave_id` (`master_slave_id`),
  CONSTRAINT `fk_master_slave_pair_master_slave` FOREIGN KEY (`master_slave_id`)
    REFERENCES `master_slave` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 6️⃣ ORDERS TABLE (Unified Master + Slave)
-- ==============================================
CREATE TABLE `orders` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `account_id` INT NOT NULL,                      -- Account that placed the trade (master or slave)
  `master_order_id` INT DEFAULT NULL,             -- Reference to parent master order (NULL = master order)
  `order_ticket` INT NOT NULL,
  `close_ticket` INT DEFAULT NULL,
  `order_symbol` VARCHAR(20) NOT NULL,
  `order_type` VARCHAR(20) NOT NULL,
  `order_lot` DECIMAL(13,3) NOT NULL,
  `order_price` DECIMAL(13,6) NOT NULL,
  `actual_price` DECIMAL(13,6) DEFAULT NULL,
  `order_comment` VARCHAR(255) DEFAULT NULL,
  `status` INT NOT NULL DEFAULT 200,              -- Using integer enum (e.g. 200 = Pending)
  `copy_message` VARCHAR(255) DEFAULT NULL,       -- Message for copy error or details
  `order_open_at` DATETIME DEFAULT NULL,
  `order_close_at` DATETIME DEFAULT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_account_id` (`account_id`),
  KEY `idx_master_order_id` (`master_order_id`),
  CONSTRAINT `fk_orders_account` FOREIGN KEY (`account_id`)
    REFERENCES `accounts` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_orders_master_order` FOREIGN KEY (`master_order_id`)
    REFERENCES `orders` (`id`)
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

