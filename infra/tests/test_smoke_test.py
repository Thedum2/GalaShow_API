import contextlib
import importlib.util
import io
import json
from pathlib import Path
import unittest
from unittest.mock import patch
from urllib.parse import urlsplit


spec = importlib.util.spec_from_file_location("smoke_test", Path(__file__).parents[1] / "smoke-test.py")
smoke_test = importlib.util.module_from_spec(spec)
spec.loader.exec_module(smoke_test)


class ApiSmokeTests(unittest.TestCase):
    def api_response(self, stage, *, deny_localhost=False, allow_untrusted=False, empty_background=False, missing_banners=False):
        api_host = "api-dev.galashow.cloud" if stage == "dev" else "api.galashow.cloud"
        allowed = {"https://dev.galashow.cloud", "https://admin-dev.galashow.cloud"} if stage == "dev" else {
            "https://galashow.cloud", "https://admin.galashow.cloud"
        }
        if stage == "dev" and not deny_localhost:
            allowed.update({"http://localhost:8080", "https://localhost:8443"})

        def respond(url, method="GET", headers=None):
            parts = urlsplit(url)
            self.assertEqual(api_host, parts.netloc, "API-only checks must not depend on frontend deployment")
            origin = (headers or {}).get("Origin")
            response_headers = {"Vary": "Origin"}
            if origin in allowed or allow_untrusted:
                response_headers.update({
                    "Access-Control-Allow-Origin": origin,
                    "Access-Control-Allow-Credentials": "true",
                    "Access-Control-Allow-Methods": "GET,POST,PUT,DELETE,OPTIONS",
                    "Access-Control-Allow-Headers": "Authorization,Content-Type",
                })
            if method == "OPTIONS":
                return 200, response_headers, b""
            if parts.path == "/background" and empty_background:
                return 451, response_headers, b'{"error":{"code":"451"}}'
            rows = [{"id": number} for number in range(1, 11)] if parts.path == "/banners" else []
            if missing_banners:
                rows = []
            return 200, response_headers, json.dumps({"status": "200", "data": rows}).encode()

        return respond

    def check_api(self, stage, **options):
        with contextlib.redirect_stdout(io.StringIO()):
            smoke_test.check(stage, api_only=True, **options)

    def test_api_only_checks_dev_without_requesting_frontend(self):
        with patch.object(smoke_test, "request", side_effect=self.api_response("dev")):
            self.check_api("dev")

    def test_api_only_checks_prod_with_localhost_denied(self):
        with patch.object(smoke_test, "request", side_effect=self.api_response("prod")):
            self.check_api("prod")

    def test_dev_fails_when_arbitrary_localhost_ports_are_denied(self):
        with patch.object(smoke_test, "request", side_effect=self.api_response("dev", deny_localhost=True)):
            with self.assertRaises(AssertionError):
                self.check_api("dev")

    def test_fails_when_untrusted_origins_are_allowed(self):
        for stage in ("dev", "prod"):
            with self.subTest(stage=stage), patch.object(smoke_test, "request", side_effect=self.api_response(stage, allow_untrusted=True)):
                with self.assertRaises(AssertionError):
                    self.check_api(stage)

    def test_allow_empty_accepts_unregistered_background(self):
        with patch.object(smoke_test, "request", side_effect=self.api_response("dev", empty_background=True)):
            self.check_api("dev", allow_empty=True)

    def test_allow_empty_still_requires_banner_slots(self):
        with patch.object(smoke_test, "request", side_effect=self.api_response("dev", missing_banners=True)):
            with self.assertRaises(AssertionError):
                self.check_api("dev", allow_empty=True)

    def test_full_check_still_rejects_unavailable_frontend(self):
        with patch.object(smoke_test, "request", return_value=(503, {}, b"")):
            with self.assertRaises(AssertionError):
                smoke_test.check("dev")
