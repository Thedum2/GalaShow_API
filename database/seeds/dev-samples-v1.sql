-- Development sample data v1. Run only through seed-dev-samples.
-- The caller owns a transaction and the same advisory lock as DB initialization.
-- Never overwrite existing content. Only empty banner messages and sample video URLs are filled.
-- @assetBaseUrl is a fixed development URL supplied by SampleSeeder.

UPDATE banners SET message = CASE id
    WHEN 1 THEN '[샘플] GalaShow에 오신 것을 환영합니다!'
    WHEN 2 THEN '[샘플] 채팅으로 함께 즐기는 미니게임'
    WHEN 3 THEN '[샘플] 오늘의 주인공은 바로 여러분'
    WHEN 4 THEN '[샘플] 선택하고, 도전하고, 끝까지 살아남으세요'
    WHEN 5 THEN '[샘플] 친구들과 함께하는 즐거운 한 판'
    WHEN 6 THEN '[샘플] 관리자에서 배너 문구를 수정해 보세요'
    WHEN 7 THEN '[샘플] 새로운 게임을 준비하고 있습니다'
    WHEN 8 THEN '[샘플] 서로 배려하며 함께 플레이해요'
    WHEN 9 THEN '[샘플] 개발 환경 기능 확인용 데이터입니다'
    WHEN 10 THEN '[샘플] 여러분의 선택이 결과를 만듭니다'
    ELSE message END
WHERE id BETWEEN 1 AND 10 AND TRIM(message) = '';

INSERT INTO background (id, title, `type`, file_url) VALUES
    (1, '[샘플] 보라색 무대', 'image', CONCAT(@assetBaseUrl, 'background-purple.svg')),
    (2, '[샘플] 파란색 무대', 'image', CONCAT(@assetBaseUrl, 'background-blue.svg')),
    (3, '[샘플] 초록색 무대', 'image', CONCAT(@assetBaseUrl, 'background-green.svg'))
ON DUPLICATE KEY UPDATE id = background.id;

-- A singleton policy row: do not change which existing policy is served.
INSERT INTO policies (id, terms_of_service_url, privacy_policy_url)
SELECT 1, CONCAT(@assetBaseUrl, 'sample-terms.pdf'), CONCAT(@assetBaseUrl, 'sample-privacy.pdf')
WHERE NOT EXISTS (SELECT 1 FROM policies);

INSERT INTO sns_links (id, title, url, icon_url, `order`) VALUES
    (1, '[샘플] YouTube', 'https://www.youtube.com/', CONCAT(@assetBaseUrl, 'youtube.svg'), 1),
    (2, '[샘플] CHZZK', 'https://chzzk.naver.com/', CONCAT(@assetBaseUrl, 'chzzk.svg'), 2),
    (3, '[샘플] SOOP', 'https://www.sooplive.co.kr/', CONCAT(@assetBaseUrl, 'soop.svg'), 3)
ON DUPLICATE KEY UPDATE id = sns_links.id;

INSERT INTO minigames (id, name, description, video_url, logo_url, phase_data, game_data) VALUES
    (1, '[샘플] 좌우 선택', '두 선택지 중 하나를 고르는 미니게임 카탈로그 샘플입니다. 관리자 조회·편집 확인용이며 Unity 실행 게임은 아닙니다.', CONCAT(@assetBaseUrl, 'game-choice.mp4'), CONCAT(@assetBaseUrl, 'game-choice.svg'),
        '{"READY":2000,"SETUP":1500,"PRESENT":800,"INPUT":8000,"WAIT":1500,"EXECUTE":800,"REVEAL":2500,"CLEANUP":1500}', '{"sampleOnly":true,"choices":["왼쪽","오른쪽"]}'),
    (2, '[샘플] 숫자 투표', '1부터 3까지의 숫자 중 하나를 선택하는 카탈로그 샘플입니다. 관리자 조회·편집 확인용이며 Unity 실행 게임은 아닙니다.', CONCAT(@assetBaseUrl, 'game-vote.mp4'), CONCAT(@assetBaseUrl, 'game-vote.svg'),
        '{"READY":2000,"SETUP":1500,"PRESENT":1000,"INPUT":10000,"WAIT":1500,"EXECUTE":800,"REVEAL":2500,"CLEANUP":1500}', '{"sampleOnly":true,"choices":[1,2,3]}'),
    (3, '[샘플] 생존 챌린지', '도전 또는 대기를 선택하는 카탈로그 샘플입니다. 생존 통계는 가상 값이며 Unity 실행 게임은 아닙니다.', CONCAT(@assetBaseUrl, 'game-survival.mp4'), CONCAT(@assetBaseUrl, 'game-survival.svg'),
        '{"READY":2000,"SETUP":1500,"PRESENT":800,"INPUT":6000,"WAIT":1500,"EXECUTE":800,"REVEAL":3000,"CLEANUP":1500}', '{"sampleOnly":true,"choices":["도전","대기"]}')
ON DUPLICATE KEY UPDATE id = minigames.id;

-- Backfill only the original sample rows; preserve user-provided video URLs.
UPDATE minigames m
JOIN (
    SELECT 1 game_id, '[샘플] 좌우 선택' game_name, 'game-choice.mp4' video_file
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 'game-vote.mp4'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 'game-survival.mp4'
) v ON m.id = v.game_id AND m.name = v.game_name
SET m.video_url = CONCAT(@assetBaseUrl, v.video_file), m.updated_at = CURRENT_TIMESTAMP
WHERE TRIM(m.video_url) = '';

-- Add related records only to the matching sample parent. Natural keys prevent duplicates.
INSERT INTO minigame_tags (minigame_id, tag_type, tag_value)
SELECT m.id, t.tag_type, t.tag_value FROM minigames m
JOIN (
    SELECT 1 game_id, '[샘플] 좌우 선택' game_name, 'scale' tag_type, 'medium' tag_value
    UNION ALL SELECT 1, '[샘플] 좌우 선택', 'difficulty', '1'
    UNION ALL SELECT 1, '[샘플] 좌우 선택', 'round', '1-2'
    UNION ALL SELECT 1, '[샘플] 좌우 선택', 'type', 'choice'
    UNION ALL SELECT 1, '[샘플] 좌우 선택', 'survival_rate', 'medium'
    UNION ALL SELECT 1, '[샘플] 좌우 선택', 'win_condition', 'goal'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 'scale', 'large'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 'difficulty', '2'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 'round', '3-4'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 'type', 'brain'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 'survival_rate', 'high'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 'win_condition', 'score'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 'scale', 'small'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 'difficulty', '3'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 'round', '5-6'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 'type', 'luck'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 'survival_rate', 'low'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 'win_condition', 'goal'
) t ON m.id = t.game_id AND m.name = t.game_name
WHERE NOT EXISTS (SELECT 1 FROM minigame_tags old WHERE old.minigame_id = m.id AND old.tag_type = t.tag_type);

INSERT INTO minigame_tutorials (minigame_id, step, description)
SELECT m.id, t.step, t.description FROM minigames m
JOIN (
    SELECT 1 game_id, '[샘플] 좌우 선택' game_name, 1 step, '화면에 표시된 왼쪽과 오른쪽 선택지를 확인합니다.' description
    UNION ALL SELECT 1, '[샘플] 좌우 선택', 2, '제한 시간 안에 채팅으로 왼쪽 또는 오른쪽을 입력합니다.'
    UNION ALL SELECT 1, '[샘플] 좌우 선택', 3, '입력 종료 후 결과 화면을 확인합니다. 이 데이터는 카탈로그 샘플입니다.'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 1, '세 개의 숫자 선택지를 확인합니다.'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 2, '채팅으로 1, 2, 3 중 하나를 입력합니다.'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 3, '집계 결과와 점수 표시 예시를 확인합니다. 이 데이터는 카탈로그 샘플입니다.'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 1, '도전과 대기 중 하나를 선택합니다.'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 2, '제한 시간 안에 채팅으로 선택을 입력합니다.'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 3, '생존 결과 표시 예시를 확인합니다. 이 데이터는 카탈로그 샘플입니다.'
) t ON m.id = t.game_id AND m.name = t.game_name
WHERE NOT EXISTS (SELECT 1 FROM minigame_tutorials old WHERE old.minigame_id = m.id AND old.step = t.step);

INSERT INTO minigame_controls (minigame_id, key_name, `keys`)
SELECT m.id, c.key_name, c.key_values FROM minigames m
JOIN (
    SELECT 1 game_id, '[샘플] 좌우 선택' game_name, '왼쪽' key_name, '["왼쪽","좌","1"]' key_values
    UNION ALL SELECT 1, '[샘플] 좌우 선택', '오른쪽', '["오른쪽","우","2"]'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', '1번', '["1"]'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', '2번', '["2"]'
    UNION ALL SELECT 2, '[샘플] 숫자 투표', '3번', '["3"]'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', '도전', '["도전","1"]'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', '대기', '["대기","2"]'
) c ON m.id = c.game_id AND m.name = c.game_name
WHERE NOT EXISTS (SELECT 1 FROM minigame_controls old WHERE old.minigame_id = m.id AND old.key_name = c.key_name);

INSERT INTO minigame_survival_stats (minigame_id, survival_rate, total_games, total_players, survivors)
SELECT m.id, s.rate, s.games, s.players, s.survivors FROM minigames m
JOIN (
    SELECT 1 game_id, '[샘플] 좌우 선택' game_name, 50 rate, 10 games, 200 players, 100 survivors
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 75, 8, 160, 120
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 25, 12, 240, 60
) s ON m.id = s.game_id AND m.name = s.game_name
WHERE NOT EXISTS (SELECT 1 FROM minigame_survival_stats old WHERE old.minigame_id = m.id);

INSERT INTO viewer_avatars (id, `order`, name, gif_url) VALUES
    (1, 1, '[샘플] 보라 친구', CONCAT(@assetBaseUrl, 'avatar-purple.svg')),
    (2, 2, '[샘플] 파랑 친구', CONCAT(@assetBaseUrl, 'avatar-blue.svg')),
    (3, 3, '[샘플] 초록 친구', CONCAT(@assetBaseUrl, 'avatar-green.svg')),
    (4, 4, '[샘플] 분홍 친구', CONCAT(@assetBaseUrl, 'avatar-pink.svg'))
ON DUPLICATE KEY UPDATE id = viewer_avatars.id;
