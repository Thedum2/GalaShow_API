"""Read-only checks for a deployed GalaShow environment (Python 3, no packages)."""
import argparse
import json
from urllib.error import HTTPError
from urllib.request import Request, urlopen


def request(url, method="GET", headers=None):
    try:
        response = urlopen(Request(url, method=method, headers=headers or {}), timeout=35)
    except HTTPError as error:
        response = error
    with response:
        return response.status, response.headers, response.read()


def check(stage, allow_empty=False, api_only=False):
    origin = "https://dev.galashow.cloud" if stage == "dev" else "https://galashow.cloud"
    api = "https://api-dev.galashow.cloud" if stage == "dev" else "https://api.galashow.cloud"
    admin = "https://admin-dev.galashow.cloud" if stage == "dev" else "https://admin.galashow.cloud"
    other_origin = "https://galashow.cloud" if stage == "dev" else "https://dev.galashow.cloud"

    if not api_only:
        for url in [origin, origin + "/lobby", admin, admin + "/login"]:
            status, headers, body = request(url)
            assert status == 200 and "text/html" in headers.get("Content-Type", ""), (url, status)
            assert b'<div id="root"' in body, url

        status, headers, _ = request(origin + "/build/unity/WebGL.wasm", "HEAD")
        assert status == 200 and headers.get("Content-Type") == "application/wasm", (status, dict(headers))
        status, headers, _ = request(origin + "/assets/does-not-exist.js")
        assert status in (403, 404) and "text/html" not in headers.get("Content-Type", ""), status

    local_origins = ["http://localhost:8080", "https://localhost:8443"]
    allowed_origins = [origin, admin] + local_origins
    rejected_origins = [other_origin, "https://untrusted.example", "http://localhost.untrusted.example:8080"]

    for allowed in allowed_origins:
        status, headers, _ = request(api + "/banners", "OPTIONS", {
            "Origin": allowed, "Access-Control-Request-Method": "GET",
            "Access-Control-Request-Headers": "authorization,content-type",
        })
        assert status == 200 and headers.get("Access-Control-Allow-Origin") == allowed, (allowed, status)
        assert headers.get("Access-Control-Allow-Credentials") == "true", allowed
        assert "GET" in headers.get("Access-Control-Allow-Methods", "").split(","), allowed
        allowed_headers = {value.strip().lower() for value in headers.get("Access-Control-Allow-Headers", "").split(",")}
        assert {"authorization", "content-type"}.issubset(allowed_headers), allowed
        assert "Origin" in headers.get("Vary", ""), dict(headers)
    for rejected in rejected_origins:
        _, headers, _ = request(api + "/banners", "OPTIONS", {"Origin": rejected})
        assert headers.get("Access-Control-Allow-Origin") is None, rejected

    # These endpoints read only. No tokens, login attempts, or database changes.
    empty_content = []
    for path, empty_status in (("/banners", None), ("/background", 451), ("/sns-links", None)):
        status, headers, body = request(api + path, headers={"Origin": origin})
        payload = json.loads(body)
        if allow_empty and empty_status is not None and status == empty_status:
            assert payload.get("error", {}).get("code") == str(empty_status), payload
            empty_content.append(path)
        else:
            assert status == 200 and payload.get("status") == "200" and "data" in payload, (path, status, payload)
            if path == "/banners":
                assert set(range(1, 11)).issubset({row["id"] for row in payload["data"]}), payload
        assert headers.get("Access-Control-Allow-Origin") == origin, path
    print(json.dumps({"stage": stage, "result": "passed", "scope": "api" if api_only else "all", "web": None if api_only else origin, "api": api, "emptyContent": empty_content}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stage", choices=("dev", "prod", "all"), default="all")
    parser.add_argument("--allow-empty", action="store_true", help="Accept background error 451 before content is registered; banners must always load")
    parser.add_argument("--api-only", action="store_true", help="Check API and CORS without depending on Client/Admin deployments")
    options = parser.parse_args()
    for environment in (("dev", "prod") if options.stage == "all" else (options.stage,)):
        check(environment, options.allow_empty, options.api_only)
