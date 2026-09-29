"""Render silent catalog previews with Playwright and Microsoft Edge.

Requires the Python playwright package and an installed Microsoft Edge browser.
These animations illustrate sample choices, not implemented Unity gameplay.
"""
import base64
from pathlib import Path

from playwright.sync_api import sync_playwright

OUTPUT = Path(__file__).resolve().parents[3] / "Client/public/sample-data/v1"
GAMES = [
    {"kind": "choice", "title": "좌우 선택", "choices": ["왼쪽", "오른쪽"],
     "color": "#7c3aed", "hint": "채팅으로 왼쪽 또는 오른쪽을 입력하세요"},
    {"kind": "vote", "title": "숫자 투표", "choices": ["1", "2", "3"],
     "color": "#2563eb", "hint": "채팅으로 1, 2, 3 중 하나를 입력하세요"},
    {"kind": "survival", "title": "생존 챌린지", "choices": ["도전", "대기"],
     "color": "#059669", "hint": "채팅으로 도전 또는 대기를 입력하세요"},
]

RENDER = r"""async (game) => {
    const mimeType = 'video/mp4;codecs=avc1.42001E';
    if (!MediaRecorder.isTypeSupported(mimeType)) {
        throw new Error('Microsoft Edge must support H.264 MP4 recording');
    }
    const canvas = document.createElement('canvas');
    canvas.width = 640;
    canvas.height = 360;
    document.body.replaceChildren(canvas);
    const ctx = canvas.getContext('2d');
    const label = (value, x, y, size, color = '#ffffff') => {
        ctx.fillStyle = color;
        ctx.font = `bold ${size}px "Malgun Gothic", sans-serif`;
        ctx.textAlign = 'center';
        ctx.fillText(value, x, y);
    };
    const draw = (elapsed) => {
        ctx.fillStyle = '#101322';
        ctx.fillRect(0, 0, 640, 360);
        ctx.fillStyle = game.color;
        ctx.fillRect(0, 0, 640, 7);
        label('GALASHOW  /  SAMPLE PREVIEW', 320, 38, 13, '#aeb7d0');
        label(game.title, 320, 85, 32);
        label(game.hint, 320, 118, 16, '#cbd5e1');
        const count = game.choices.length;
        const width = count === 3 ? 150 : 218;
        const gap = 18;
        const left = (640 - (width * count + gap * (count - 1))) / 2;
        const active = Math.floor(elapsed / 1000) % count;
        game.choices.forEach((choice, index) => {
            const x = left + index * (width + gap);
            ctx.fillStyle = index === active ? game.color : '#242b40';
            ctx.beginPath();
            ctx.roundRect(x, 152, width, 109, 18);
            ctx.fill();
            label(choice, x + width / 2, 220, 32);
            ctx.fillStyle = index === active ? '#ffffff' : '#4b556d';
            ctx.beginPath();
            ctx.arc(x + width / 2, 283, 4, 0, Math.PI * 2);
            ctx.fill();
        });
        label('개발용 샘플 · 실제 게임 플레이 영상이 아닙니다', 320, 331, 13, '#aeb7d0');
    };
    await document.fonts.load('bold 32px "Malgun Gothic"', game.title);
    draw(0);
    const stream = canvas.captureStream(24);
    const recorder = new MediaRecorder(stream, {mimeType, videoBitsPerSecond: 650000});
    const chunks = [];
    const stopped = new Promise((resolve, reject) => {
        recorder.ondataavailable = (event) => chunks.push(event.data);
        recorder.onstop = resolve;
        recorder.onerror = (event) => reject(event.error);
    });
    recorder.start();
    const started = performance.now();
    await new Promise(resolve => {
        const frame = () => {
            const elapsed = performance.now() - started;
            draw(elapsed);
            if (elapsed < 6000) requestAnimationFrame(frame);
            else resolve();
        };
        requestAnimationFrame(frame);
    });
    recorder.stop();
    await stopped;
    stream.getTracks().forEach(track => track.stop());
    const blob = new Blob(chunks, {type: 'video/mp4'});
    return await new Promise(resolve => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result.split(',')[1]);
        reader.readAsDataURL(blob);
    });
}"""


def build():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(channel="msedge", headless=True)
        page = browser.new_page()
        for game in GAMES:
            video = base64.b64decode(page.evaluate(RENDER, game))
            path = OUTPUT / f"game-{game['kind']}.mp4"
            path.write_bytes(video)
            print(f"Built {path.name}: {len(video):,} bytes")
        browser.close()


if __name__ == "__main__":
    build()
