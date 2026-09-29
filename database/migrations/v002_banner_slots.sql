-- Required editable banner slots for the existing Admin UI (IDs 1-10).
-- Empty messages are intentional: deployment must not invent public content.
-- Re-running this migration preserves every existing message and display order.
INSERT INTO `banners` (`id`, `message`, `order`) VALUES
    (1, '', 1), (2, '', 2), (3, '', 3), (4, '', 4), (5, '', 5),
    (6, '', 6), (7, '', 7), (8, '', 8), (9, '', 9), (10, '', 10)
ON DUPLICATE KEY UPDATE `id` = `banners`.`id`;
