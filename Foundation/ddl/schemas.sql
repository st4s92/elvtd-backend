-- ==============================================
-- 1️⃣ USERS TABLE
-- ==============================================
-- elvtd_core.users definition
CREATE TABLE `users` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password` varchar(300) NOT NULL,
  `role_id` bigint NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_users_email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 2️⃣ ACCOUNTS TABLE
-- ==============================================
-- elvtd_core.accounts definition

CREATE TABLE `accounts` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `platform_name` varchar(100) NOT NULL,
  `account_number` bigint NOT NULL,
  `account_password` varchar(100) NOT NULL,
  `broker_name` varchar(100) NOT NULL,
  `server_name` varchar(100) NOT NULL,
  `user_id` bigint NOT NULL,
  `equity` decimal(13,2) NOT NULL DEFAULT '0.00',
  `balance` decimal(13,2) NOT NULL DEFAULT '0.00',
  `status` int NOT NULL DEFAULT '100',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_user_id` (`user_id`),
  CONSTRAINT `fk_accounts_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 3️⃣ MASTER-SLAVE RELATIONSHIP TABLE
-- ==============================================
-- elvtd_core.master_slave definition
CREATE TABLE `master_slave` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `master_id` bigint NOT NULL,
  `slave_id` bigint NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_master_id` (`master_id`),
  KEY `idx_slave_id` (`slave_id`),
  CONSTRAINT `fk_master_slave_master` FOREIGN KEY (`master_id`) REFERENCES `accounts` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_master_slave_slave` FOREIGN KEY (`slave_id`) REFERENCES `accounts` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 4️⃣ MASTER-SLAVE CONFIG TABLE
-- ==============================================
-- elvtd_core.master_slave_config definition

CREATE TABLE `master_slave_config` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `master_slave_id` bigint NOT NULL,
  `multiplier` decimal(10,4) NOT NULL DEFAULT '1.0000',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_master_slave_id` (`master_slave_id`),
  CONSTRAINT `fk_master_slave_config_master_slave` FOREIGN KEY (`master_slave_id`) REFERENCES `master_slave` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 5️⃣ MASTER-SLAVE PAIR TABLE
-- ==============================================
-- elvtd_core.master_slave_pair definition
CREATE TABLE `master_slave_pair` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `master_slave_id` bigint NOT NULL,
  `master_pair` varchar(10) NOT NULL,
  `slave_pair` varchar(10) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_master_slave_id` (`master_slave_id`),
  CONSTRAINT `fk_master_slave_pair_master_slave` FOREIGN KEY (`master_slave_id`) REFERENCES `master_slave` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- 6️⃣ ORDERS TABLE (Unified Master + Slave)
-- ==============================================
-- elvtd_core.orders definition
-- elvtd_core.orders definition

CREATE TABLE `orders` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `account_id` bigint NOT NULL,
  `master_order_id` bigint DEFAULT NULL,
  `order_ticket` bigint NOT NULL,
  `close_ticket` bigint DEFAULT NULL,
  `order_symbol` varchar(20) NOT NULL,
  `order_type` varchar(20) NOT NULL,
  `order_lot` decimal(13,3) NOT NULL,
  `order_price` decimal(13,6) DEFAULT NULL,
  `order_magic` int DEFAULT NULL,
  `close_price` decimal(13,6) DEFAULT NULL,
  `status` int NOT NULL DEFAULT '200',
  `copy_message` varchar(255) DEFAULT NULL,
  `order_copied_at` datetime DEFAULT NULL,
  `order_open_at` datetime DEFAULT NULL,
  `order_close_at` datetime DEFAULT NULL,
  `order_profit` decimal(13,2) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_account_id` (`account_id`),
  KEY `idx_master_order_id` (`master_order_id`),
  CONSTRAINT `fk_orders_account` FOREIGN KEY (`account_id`) REFERENCES `accounts` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_orders_master_order` FOREIGN KEY (`master_order_id`) REFERENCES `orders` (`id`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- SERVERS TABLE
-- ==============================================
CREATE TABLE `servers` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `server_name` VARCHAR(100) NOT NULL,
  `server_ip` VARCHAR(100) NOT NULL,
  `status` INT NOT NULL DEFAULT 100,
  `server_os` VARCHAR(300) NOT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_server_ip` (`server_ip`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci;

-- ==============================================
-- SERVER_ACCOUNT TABLE
-- ==============================================
CREATE TABLE `server_account` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `server_id` BIGINT NOT NULL,
  `account_id` BIGINT NOT NULL,
  `installation_path` VARCHAR(300) NOT NULL,
  `platform_pid` INT NULL,
  `status` INT NOT NULL DEFAULT 100,
  `message` VARCHAR(100) NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uk_server_account` (`server_id`, `account_id`),
  KEY `idx_server_id` (`server_id`),
  KEY `idx_account_id` (`account_id`),
  CONSTRAINT `fk_server_account_server`
    FOREIGN KEY (`server_id`) REFERENCES `servers` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_server_account_account`
    FOREIGN KEY (`account_id`) REFERENCES `accounts` (`id`)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci;

-- elvtd_core.active_orders definition

CREATE TABLE `active_orders` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `account_id` bigint NOT NULL,
  `account_number` bigint NOT NULL,
  `server_name` varchar(100) DEFAULT NULL,
  `master_order_id` bigint NOT NULL,
  `order_ticket` int DEFAULT '0',
  `order_symbol` varchar(20) NOT NULL,
  `order_magic` bigint DEFAULT NULL,
  `order_type` varchar(20) NOT NULL,
  `order_lot` decimal(13,3) NOT NULL,
  `order_price` decimal(13,6) NOT NULL,
  `order_profit` decimal(13,2) DEFAULT NULL,
  `status` int DEFAULT '200',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_account_magic` (`account_id`,`order_magic`),
  KEY `idx_account` (`account_id`),
  KEY `idx_master_order` (`master_order_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

