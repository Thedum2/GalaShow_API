-- GalaShow initial schema v001 (2026-09-29), MySQL 8.0.
-- Target: a NEW, EMPTY database explicitly selected by the operator.
-- See README.md before execution. No database creation, USE, seed rows, or grants.
-- IF NOT EXISTS preserves existing tables; it does NOT validate/upgrade them.

SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;
SET SESSION time_zone = '+00:00';

CREATE TABLE IF NOT EXISTS `background` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `title` VARCHAR(255) NOT NULL,
    `type` VARCHAR(64) NOT NULL,
    `file_url` TEXT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `banners` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `message` TEXT NOT NULL,
    `order` INT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_banners_order` (`order`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `policies` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `terms_of_service_url` TEXT NOT NULL,
    `privacy_policy_url` TEXT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- IDs are supplied by SnsLinkRepository.ReplaceAllAsync, including zero.
CREATE TABLE IF NOT EXISTS `sns_links` (
    `id` INT NOT NULL,
    `title` VARCHAR(255) NOT NULL,
    `url` TEXT NOT NULL,
    `icon_url` TEXT NOT NULL,
    `order` INT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_sns_links_order_id` (`order`, `id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `refresh_tokens` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `user_id` VARCHAR(255) NOT NULL,
    `token_hash` CHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `expires_at` DATETIME NOT NULL,
    `revoked_at` DATETIME NULL DEFAULT NULL,
    `user_agent` TEXT NULL,
    `ip_address` VARCHAR(45) CHARACTER SET ascii COLLATE ascii_bin NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_refresh_tokens_hash` (`token_hash`),
    KEY `ix_refresh_tokens_expires_at` (`expires_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `minigames` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(255) NOT NULL,
    `description` TEXT NOT NULL,
    `video_url` TEXT NOT NULL,
    `logo_url` TEXT NOT NULL,
    `phase_data` JSON NULL,
    `game_data` JSON NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_minigames_name` (`name`),
    KEY `ix_minigames_created_at` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Duplicate tag values/steps/control names are accepted by current requests.
CREATE TABLE IF NOT EXISTS `minigame_tags` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `minigame_id` INT NOT NULL,
    `tag_type` VARCHAR(64) NOT NULL,
    `tag_value` VARCHAR(255) NOT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_minigame_tags_game` (`minigame_id`),
    KEY `ix_minigame_tags_filter` (`tag_type`, `tag_value`, `minigame_id`),
    CONSTRAINT `fk_minigame_tags_game` FOREIGN KEY (`minigame_id`)
        REFERENCES `minigames` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `minigame_tutorials` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `minigame_id` INT NOT NULL,
    `step` INT NOT NULL,
    `description` TEXT NOT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_minigame_tutorials_game_step` (`minigame_id`, `step`),
    CONSTRAINT `fk_minigame_tutorials_game` FOREIGN KEY (`minigame_id`)
        REFERENCES `minigames` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `minigame_controls` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `minigame_id` INT NOT NULL,
    `key_name` VARCHAR(255) NOT NULL,
    `keys` JSON NOT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_minigame_controls_game` (`minigame_id`),
    CONSTRAINT `fk_minigame_controls_game` FOREIGN KEY (`minigame_id`)
        REFERENCES `minigames` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `minigame_survival_stats` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `minigame_id` INT NOT NULL,
    `survival_rate` DECIMAL(18,6) NOT NULL DEFAULT 0,
    `total_games` INT NOT NULL DEFAULT 0,
    `total_players` INT NOT NULL DEFAULT 0,
    `survivors` INT NOT NULL DEFAULT 0,
    `last_updated` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_minigame_survival_stats_game` (`minigame_id`),
    CONSTRAINT `fk_minigame_survival_stats_game` FOREIGN KEY (`minigame_id`)
        REFERENCES `minigames` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Explicit IDs preserve ViewerAvatarRepository.UpdateAllAsync input values.
CREATE TABLE IF NOT EXISTS `viewer_avatars` (
    `id` INT NOT NULL,
    `order` INT NOT NULL,
    `name` VARCHAR(255) NOT NULL,
    `gif_url` TEXT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_viewer_avatars_order` (`order`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
