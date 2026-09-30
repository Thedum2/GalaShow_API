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
        '{"READY":2000,"SETUP":1500,"PRESENT":800,"INPUT":6000,"WAIT":1500,"EXECUTE":800,"REVEAL":3000,"CLEANUP":1500}', '{"sampleOnly":true,"choices":["도전","대기"]}'),
    (4, '[샘플] 소수의 선택', 'A와 B 중 더 적은 사람이 고른 쪽이 살아남는 카탈로그 샘플입니다. 관리자 조회·편집 확인용이며 Unity 실행 게임은 아닙니다.', CONCAT(@assetBaseUrl, 'game-minority.mp4'), CONCAT(@assetBaseUrl, 'game-minority.svg'),
        '{"READY":2000,"SETUP":1500,"PRESENT":800,"INPUT":7000,"WAIT":1500,"EXECUTE":800,"REVEAL":3000,"CLEANUP":1500}', '{"sampleOnly":true,"choices":["A","B"],"rule":"minority"}')
ON DUPLICATE KEY UPDATE id = minigames.id;

-- Backfill only the original sample rows; preserve user-provided video URLs.
UPDATE minigames m
JOIN (
    SELECT 1 game_id, '[샘플] 좌우 선택' game_name, 'game-choice.mp4' video_file
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 'game-vote.mp4'
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 'game-survival.mp4'
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 'game-minority.mp4'
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
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 'scale', 'xl'
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 'difficulty', '4'
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 'round', '7-8'
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 'type', 'strategy'
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 'survival_rate', 'low'
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 'win_condition', 'goal'
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
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 1, 'A와 B 두 선택지를 확인합니다. 다른 사람들이 무엇을 고를지 예상해 보세요.'
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 2, '제한 시간 안에 채팅으로 A 또는 B를 입력합니다.'
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 3, '더 적은 사람이 고른 쪽이 살아남는 결과 예시를 확인합니다. 이 데이터는 카탈로그 샘플입니다.'
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
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 'A', '["A","a","1"]'
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 'B', '["B","b","2"]'
) c ON m.id = c.game_id AND m.name = c.game_name
WHERE NOT EXISTS (SELECT 1 FROM minigame_controls old WHERE old.minigame_id = m.id AND old.key_name = c.key_name);

INSERT INTO minigame_survival_stats (minigame_id, survival_rate, total_games, total_players, survivors)
SELECT m.id, s.rate, s.games, s.players, s.survivors FROM minigames m
JOIN (
    SELECT 1 game_id, '[샘플] 좌우 선택' game_name, 50 rate, 10 games, 200 players, 100 survivors
    UNION ALL SELECT 2, '[샘플] 숫자 투표', 75, 8, 160, 120
    UNION ALL SELECT 3, '[샘플] 생존 챌린지', 25, 12, 240, 60
    UNION ALL SELECT 4, '[샘플] 소수의 선택', 35, 6, 300, 105
) s ON m.id = s.game_id AND m.name = s.game_name
WHERE NOT EXISTS (SELECT 1 FROM minigame_survival_stats old WHERE old.minigame_id = m.id);

INSERT INTO viewer_avatars (id, `order`, name, gif_url) VALUES
    (1, 1, '[샘플] 보라 친구', CONCAT(@assetBaseUrl, 'avatar-purple.svg')),
    (2, 2, '[샘플] 파랑 친구', CONCAT(@assetBaseUrl, 'avatar-blue.svg')),
    (3, 3, '[샘플] 초록 친구', CONCAT(@assetBaseUrl, 'avatar-green.svg')),
    (4, 4, '[샘플] 분홍 친구', CONCAT(@assetBaseUrl, 'avatar-pink.svg'))
ON DUPLICATE KEY UPDATE id = viewer_avatars.id;

-- ── 트롤리 딜레마 (Unity 플러그인 galashow.trolley, docs/minigame-trolley.md 4·8절). 이름으로 중복을 막는다.

INSERT INTO minigames (name, description, video_url, logo_url, phase_data, game_data)
SELECT '트롤리 딜레마', '호스트가 지킬 선택지를 예상하세요. 트롤리는 반대편으로 갑니다. 호스트와 같은 선택을 한 사람만 살아남습니다.', '', '',
    '{"READY": 2000, "SETUP": 1500, "PRESENT": 6000, "INPUT": 15000, "WAIT": -1, "EXECUTE": 1000, "REVEAL": 8000, "CLEANUP": 1500}',
    '{"schemaVersion": 1, "pluginId": "galashow.trolley", "players": {"min": 2, "max": 50}, "rule": {"type": "match_host", "hostChoice": {"source": "host_ui", "hiddenUntil": "REVEAL", "lockAt": "WAIT_END", "ifMissing": "random"}, "noInput": "random", "inputChange": "last", "allEliminated": "all_survive"}, "content": {"pick": "random_unplayed", "dilemmas": [{"id": "classic", "category": "classic", "title": "고전 트롤리", "description": "트롤리가 다섯 명이 있는 선로로 달려갑니다. 레버를 당기면 한 명이 있는 선로로 바뀝니다.", "choices": [{"id": "A", "label": "레버를 당긴다", "description": ""}, {"id": "B", "label": "그대로 둔다", "description": ""}]}, {"id": "self-switch", "category": "classic", "title": "나 vs 다섯", "description": "선로를 바꾸면 다섯 명은 살지만, 트롤리가 내가 서 있는 쪽으로 옵니다.", "choices": [{"id": "A", "label": "선로를 바꾼다", "description": ""}, {"id": "B", "label": "바꾸지 않는다", "description": ""}]}, {"id": "auto-lever", "category": "classic", "title": "자동 레버", "description": "아무것도 안 하면 10초 뒤 레버가 저절로 당겨집니다. 막으려면 버튼을 눌러야 합니다.", "choices": [{"id": "A", "label": "버튼을 누른다", "description": ""}, {"id": "B", "label": "가만히 있는다", "description": ""}]}, {"id": "chicken-vs-tteokbokki", "category": "food", "title": "야식 트롤리", "description": "트롤리가 방금 도착한 치킨 다섯 마리로 돌진 중! 레버를 당기면 내 최애 떡볶이 한 그릇 쪽으로 갑니다.", "choices": [{"id": "A", "label": "치킨을 살린다", "description": ""}, {"id": "B", "label": "떡볶이를 지킨다", "description": ""}]}, {"id": "mint-choco", "category": "food", "title": "민초 공장", "description": "트롤리가 민트초코 공장으로 달려갑니다. 레버를 당기면 하와이안 피자 공장으로 바뀝니다.", "choices": [{"id": "A", "label": "민초를 지킨다", "description": ""}, {"id": "B", "label": "하와이안을 지킨다", "description": ""}]}, {"id": "last-ramen", "category": "food", "title": "새벽 3시의 라면", "description": "남은 라면은 하나. 트롤리가 끓기 직전의 라면으로 갑니다. 레버를 당기면 내일 아침밥이 사라집니다.", "choices": [{"id": "A", "label": "지금 라면을 먹는다", "description": ""}, {"id": "B", "label": "내일 아침을 지킨다", "description": ""}]}, {"id": "sweet-or-spicy", "category": "food", "title": "평생 금지", "description": "트롤리가 ''평생 매운 음식 금지'' 표지판을 향해 갑니다. 레버를 당기면 ''평생 단 음식 금지'' 쪽으로 바뀝니다.", "choices": [{"id": "A", "label": "매운 걸 포기한다", "description": ""}, {"id": "B", "label": "단 걸 포기한다", "description": ""}]}, {"id": "pour-or-dip", "category": "food", "title": "탕수육 선로", "description": "트롤리가 ''부먹'' 마을로 돌진 중. 레버를 당기면 ''찍먹'' 마을로 갑니다.", "choices": [{"id": "A", "label": "부먹을 지킨다", "description": ""}, {"id": "B", "label": "찍먹을 지킨다", "description": ""}]}, {"id": "donation-split", "category": "stream", "title": "후원 트롤리", "description": "트롤리가 ''천 원 후원 다섯 개''로 달려갑니다. 레버를 당기면 ''만 원 후원 하나''가 사라집니다.", "choices": [{"id": "A", "label": "다섯 개를 지킨다", "description": ""}, {"id": "B", "label": "한 방을 지킨다", "description": ""}]}, {"id": "end-stream-button", "category": "stream", "title": "방종 버튼", "description": "트롤리가 ''방송 종료'' 버튼을 누르러 갑니다. 레버를 당기면 ''마이크 음소거'' 버튼을 누릅니다.", "choices": [{"id": "A", "label": "음소거가 낫다", "description": ""}, {"id": "B", "label": "차라리 방종", "description": ""}]}, {"id": "upload-or-vacation", "category": "stream", "title": "편집자의 휴가", "description": "트롤리가 오늘 올릴 영상으로 돌진 중. 레버를 당기면 편집자의 휴가가 취소됩니다.", "choices": [{"id": "A", "label": "영상을 살린다", "description": ""}, {"id": "B", "label": "휴가를 지킨다", "description": ""}]}, {"id": "chat-freeze", "category": "stream", "title": "채팅창 얼리기", "description": "트롤리가 채팅창을 10분간 얼리러 갑니다. 레버를 당기면 내 카메라가 10분간 꺼집니다.", "choices": [{"id": "A", "label": "채팅을 지킨다", "description": ""}, {"id": "B", "label": "카메라를 지킨다", "description": ""}]}, {"id": "sub-or-viewer", "category": "stream", "title": "숫자 선로", "description": "트롤리가 ''오늘 시청자 수 두 배'' 선로로 갑니다. 레버를 당기면 ''구독자 천 명 증가'' 선로로 바뀝니다.", "choices": [{"id": "A", "label": "시청자를 늘린다", "description": ""}, {"id": "B", "label": "구독자를 늘린다", "description": ""}]}, {"id": "snooze-alarm", "category": "daily", "title": "알람 선로", "description": "트롤리가 ''5분만 더'' 알람 다섯 개로 달려갑니다. 레버를 당기면 한 번에 깨우는 무서운 알람 하나가 사라집니다.", "choices": [{"id": "A", "label": "5분 알람을 지킨다", "description": ""}, {"id": "B", "label": "무서운 알람을 지킨다", "description": ""}]}, {"id": "read-receipt", "category": "daily", "title": "단톡방", "description": "트롤리가 ''읽씹'' 버튼으로 갑니다. 레버를 당기면 ''안읽씹'' 버튼으로 바뀝니다.", "choices": [{"id": "A", "label": "읽고 답 안 하기", "description": ""}, {"id": "B", "label": "아예 안 읽기", "description": ""}]}, {"id": "wifi-or-aircon", "category": "daily", "title": "한여름의 선택", "description": "한여름, 트롤리가 와이파이 공유기로 돌진 중. 레버를 당기면 에어컨이 부서집니다.", "choices": [{"id": "A", "label": "와이파이를 지킨다", "description": ""}, {"id": "B", "label": "에어컨을 지킨다", "description": ""}]}, {"id": "spoiler", "category": "daily", "title": "스포일러 열차", "description": "트롤리에 드라마 결말을 스포하는 사람이 타고 있습니다. 레버를 당기면 대신 다음 화 공개가 한 달 미뤄집니다.", "choices": [{"id": "A", "label": "스포를 듣는다", "description": ""}, {"id": "B", "label": "한 달 기다린다", "description": ""}]}, {"id": "time-machine", "category": "final", "title": "시간 열차", "description": "트롤리가 ''10년 전으로 돌아가기'' 역으로 갑니다. 레버를 당기면 ''10년 뒤로 건너뛰기'' 역으로 바뀝니다.", "choices": [{"id": "A", "label": "과거로 간다", "description": ""}, {"id": "B", "label": "미래로 간다", "description": ""}]}, {"id": "lottery", "category": "final", "title": "복권 선로", "description": "트롤리가 ''지금 당장 1억'' 선로로 갑니다. 레버를 당기면 ''평생 매달 100만 원'' 선로로 바뀝니다.", "choices": [{"id": "A", "label": "지금 1억", "description": ""}, {"id": "B", "label": "평생 100만 원", "description": ""}]}, {"id": "save-file", "category": "final", "title": "세이브 파일", "description": "트롤리가 100시간 플레이한 세이브 파일로 돌진 중. 레버를 당기면 대신 신작 게임 하나를 영영 못 합니다.", "choices": [{"id": "A", "label": "세이브를 살린다", "description": ""}, {"id": "B", "label": "신작을 지킨다", "description": ""}]}, {"id": "superpower", "category": "final", "title": "초능력 열차", "description": "트롤리가 ''순간이동'' 초능력 상자로 갑니다. 레버를 당기면 ''투명인간'' 상자가 부서집니다.", "choices": [{"id": "A", "label": "순간이동", "description": ""}, {"id": "B", "label": "투명인간", "description": ""}]}, {"id": "cat-or-dog", "category": "final", "title": "간식 창고", "description": "트롤리가 고양이 간식 창고로 달려갑니다. 레버를 당기면 강아지 간식 창고로 바뀝니다.", "choices": [{"id": "A", "label": "냥이 간식을 지킨다", "description": ""}, {"id": "B", "label": "멍이 간식을 지킨다", "description": ""}]}]}}'
WHERE NOT EXISTS (SELECT 1 FROM minigames WHERE name = '트롤리 딜레마');

SET @trolley_id = (SELECT id FROM minigames WHERE name = '트롤리 딜레마');

INSERT INTO minigame_tags (minigame_id, tag_type, tag_value)
SELECT @trolley_id, t.tag_type, t.tag_value FROM (
    SELECT 'scale' tag_type, 'large' tag_value
    UNION ALL SELECT 'difficulty', '2'
    UNION ALL SELECT 'round', '1-2'
    UNION ALL SELECT 'type', 'choice'
    UNION ALL SELECT 'survival_rate', 'medium'
    UNION ALL SELECT 'win_condition', 'goal'
) t
WHERE NOT EXISTS (SELECT 1 FROM minigame_tags old WHERE old.minigame_id = @trolley_id AND old.tag_type = t.tag_type);

INSERT INTO minigame_tutorials (minigame_id, step, description)
SELECT @trolley_id, t.step, t.description FROM (
    SELECT 1 step, '화면의 딜레마와 두 선택지를 읽으세요.' description
    UNION ALL SELECT 2, '호스트가 지킬 선택지를 예상해 채팅으로 1 또는 2를 입력하세요. 캐릭터가 그 선로에 눕습니다.'
    UNION ALL SELECT 3, '트롤리는 호스트가 고른 반대편 선로로 달려갑니다. 호스트와 같은 선택을 한 사람만 살아남습니다.'
) t
WHERE NOT EXISTS (SELECT 1 FROM minigame_tutorials old WHERE old.minigame_id = @trolley_id AND old.step = t.step);

INSERT INTO minigame_controls (minigame_id, key_name, `keys`)
SELECT @trolley_id, c.key_name, c.key_values FROM (
    SELECT '1번' key_name, '["1"]' key_values
    UNION ALL SELECT '2번', '["2"]'
) c
WHERE NOT EXISTS (SELECT 1 FROM minigame_controls old WHERE old.minigame_id = @trolley_id AND old.key_name = c.key_name);
