"""Offline allow/deny regression checks for the GitHub deployment IAM boundary.

These checks cover only the policy constructs used here; AWS Access Analyzer and
an actual GitHub deployment remain the authoritative integration checks.
"""
import fnmatch
import json
from pathlib import Path
import re
import unittest


TEMPLATE = Path(__file__).resolve().parents[1] / "github-actions.json"
ACCOUNT = "251113431583"
REGION = "ap-northeast-2"


def resolve(value, parameters):
    if isinstance(value, list):
        return [resolve(item, parameters) for item in value]
    if not isinstance(value, dict):
        return value
    if set(value) == {"Ref"}:
        return parameters[value["Ref"]]
    if set(value) == {"Fn::Sub"}:
        return re.sub(r"\$\{([^}]+)\}", lambda m: parameters[m[1]], value["Fn::Sub"])
    return {key: resolve(item, parameters) for key, item in value.items()}


def values(value):
    return value if isinstance(value, list) else [value]


def matches_conditions(conditions, context):
    for operator, entries in conditions.items():
        for key, expected in entries.items():
            present = key in context
            actual = values(context.get(key, []))
            expected = values(expected)
            if operator == "Null":
                if str(not present).lower() not in expected:
                    return False
                continue
            if not present:
                return False
            if operator in ("StringEquals", "ForAllValues:StringEquals"):
                matches = [item in expected for item in actual]
            elif operator in ("StringLike", "ForAllValues:StringLike"):
                matches = [any(fnmatch.fnmatchcase(item, pattern) for pattern in expected) for item in actual]
            else:
                raise AssertionError(f"Unsupported test condition: {operator}")
            if not (all(matches) if operator.startswith("ForAllValues:") else any(matches)):
                return False
    return True


def allows(policy, action, resource, context=None):
    granted = False
    for statement in policy["Statement"]:
        if not any(fnmatch.fnmatchcase(action.lower(), p.lower()) for p in values(statement["Action"])):
            continue
        if "Resource" in statement and not any(fnmatch.fnmatchcase(resource, p) for p in values(statement["Resource"])):
            continue
        if not matches_conditions(statement.get("Condition", {}), context or {}):
            continue
        if statement["Effect"] == "Deny":
            return False
        granted = True
    return granted


class GitHubDeploymentPolicies(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.template = json.loads(TEMPLATE.read_text(encoding="utf-8"))
        cls.parameters = {key: value["Default"] for key, value in cls.template["Parameters"].items()}
        cls.parameters.update({"AWS::Partition": "aws", "AWS::AccountId": ACCOUNT, "AWS::Region": REGION,
                               "GitHubOidcProvider": f"arn:aws:iam::{ACCOUNT}:oidc-provider/token.actions.githubusercontent.com"})
        cls.roles = {name: resolve(resource["Properties"], cls.parameters)
                     for name, resource in cls.template["Resources"].items() if resource["Type"] == "AWS::IAM::Role"}

    def policy(self, application, stage):
        return self.roles[f"{application}{stage.title()}Role"]["Policies"][0]["PolicyDocument"]

    def test_template_fits_direct_cloudformation_upload_and_exports_all_roles(self):
        self.assertLessEqual(TEMPLATE.stat().st_size, 51200)
        provider = self.template["Resources"]["GitHubOidcProvider"]["Properties"]
        self.assertEqual(provider["Url"], "https://token.actions.githubusercontent.com")
        self.assertEqual(provider["ClientIdList"], ["sts.amazonaws.com"])
        for name in self.roles:
            self.assertEqual(self.template["Outputs"][name + "Arn"]["Value"], {"Fn::GetAtt": [name, "Arn"]})

    def test_six_roles_trust_only_their_repository_environment_and_audience(self):
        self.assertEqual(len(self.roles), 6)
        for app in ("Admin", "Client", "Api"):
            repo = "API" if app == "Api" else app
            for stage in ("dev", "prod"):
                role = self.roles[f"{app}{stage.title()}Role"]
                trust = role["AssumeRolePolicyDocument"]
                good = {"token.actions.githubusercontent.com:aud": "sts.amazonaws.com",
                        "token.actions.githubusercontent.com:sub": f"repo:Thedum2/GalaShow_{repo}:environment:{stage}"}
                self.assertTrue(allows(trust, "sts:AssumeRoleWithWebIdentity", "*", good))
                for sub in (f"repo:attacker/GalaShow_{repo}:environment:{stage}",
                            f"repo:Thedum2/GalaShow_{repo}:environment:{'prod' if stage == 'dev' else 'dev'}",
                            f"repo:Thedum2/GalaShow_{repo}:ref:refs/heads/develop",
                            f"repo:Thedum2/GalaShow_{repo}:pull_request"):
                    self.assertFalse(allows(trust, "sts:AssumeRoleWithWebIdentity", "*", {**good, "token.actions.githubusercontent.com:sub": sub}))
                self.assertFalse(allows(trust, "sts:AssumeRoleWithWebIdentity", "*", {**good, "token.actions.githubusercontent.com:aud": "attacker"}))
                self.assertFalse(allows(trust, "sts:AssumeRoleWithWebIdentity", "*", {}))
                self.assertEqual(trust["Statement"][0]["Principal"], {"Federated": self.parameters["GitHubOidcProvider"]})

    def test_web_publish_and_invalidation_cannot_cross_application_or_environment(self):
        for app in ("Admin", "Client"):
            for stage in ("dev", "prod"):
                policy = self.policy(app, stage)
                for candidate_app in ("admin", "client"):
                    for candidate_stage in ("dev", "prod"):
                        expected = (candidate_app, candidate_stage) == (app.lower(), stage)
                        bucket = f"arn:aws:s3:::galashow-{ACCOUNT}-{candidate_stage}-{candidate_app}"
                        for action, resource in (("s3:ListBucket", bucket), ("s3:GetBucketLocation", bucket),
                                                 ("s3:PutObject", bucket + "/index.html"), ("s3:DeleteObject", bucket + "/old.js")):
                            self.assertEqual(allows(policy, action, resource), expected)
                        distribution = self.parameters[f"{candidate_app.title()}{candidate_stage.title()}DistributionId"]
                        self.assertEqual(allows(policy, "cloudfront:CreateInvalidation", f"arn:aws:cloudfront::{ACCOUNT}:distribution/{distribution}"), expected)
                stack = f"arn:aws:cloudformation:{REGION}:{ACCOUNT}:stack/galashow-cloud-web-{stage}/id"
                self.assertTrue(allows(policy, "cloudformation:DescribeStacks", stack))
                self.assertFalse(allows(policy, "cloudformation:ExecuteChangeSet", stack))

    def test_api_artifacts_are_limited_to_stage_prefix(self):
        bucket = f"arn:aws:s3:::galashow-{ACCOUNT}-{REGION}-artifacts"
        for stage in ("dev", "prod"):
            policy = self.policy("Api", stage)
            for candidate in ("dev", "prod", "other"):
                self.assertEqual(allows(policy, "s3:PutObject", f"{bucket}/api/{candidate}/package.zip"), candidate == stage)
                self.assertEqual(allows(policy, "s3:ListBucket", bucket, {"s3:prefix": f"api/{candidate}/"}), candidate == stage)
            self.assertFalse(allows(policy, "s3:PutObject", bucket + "/unscoped.template"))

    def test_api_updates_only_own_stack_and_reads_dependencies(self):
        for stage in ("dev", "prod"):
            policy = self.policy("Api", stage)
            for candidate in ("dev", "prod"):
                stack = f"arn:aws:cloudformation:{REGION}:{ACCOUNT}:stack/galashow-cloud-api-{candidate}/id"
                self.assertEqual(allows(policy, "cloudformation:ExecuteChangeSet", stack), candidate == stage)
            for name in (f"galashow-cloud-database-{stage}", "galashow-cloud-artifacts"):
                stack = f"arn:aws:cloudformation:{REGION}:{ACCOUNT}:stack/{name}/id"
                self.assertTrue(allows(policy, "cloudformation:DescribeStacks", stack))
                self.assertFalse(allows(policy, "cloudformation:CreateChangeSet", stack))
            transform = f"arn:aws:cloudformation:{REGION}:aws:transform/Serverless-2016-10-31"
            self.assertTrue(allows(policy, "cloudformation:CreateChangeSet", transform))

    def test_api_function_and_gateway_updates_cannot_cross_environment(self):
        for stage in ("dev", "prod"):
            policy = self.policy("Api", stage)
            for candidate in ("dev", "prod"):
                for name in (f"{candidate.title()}BannerFunction", f"galashow-cloud-{candidate}-database-initializer"):
                    arn = f"arn:aws:lambda:{REGION}:{ACCOUNT}:function:{name}"
                    self.assertEqual(allows(policy, "lambda:UpdateFunctionCode", arn), candidate == stage)
                    self.assertFalse(allows(policy, "lambda:InvokeFunction", arn))
                api_id = self.parameters[f"Api{candidate.title()}RestApiId"]
                self.assertEqual(allows(policy, "apigateway:POST", f"arn:aws:apigateway:{REGION}::/restapis/{api_id}/deployments"), candidate == stage)

    def test_role_pass_and_managed_policy_attachment_are_restricted(self):
        for stage in ("dev", "prod"):
            policy = self.policy("Api", stage)
            role = f"arn:aws:iam::{ACCOUNT}:role/galashow-cloud-api-{stage}-DevBannerFunctionRole-random"
            self.assertTrue(allows(policy, "iam:PassRole", role, {"iam:PassedToService": "lambda.amazonaws.com"}))
            self.assertFalse(allows(policy, "iam:PassRole", role, {"iam:PassedToService": "ec2.amazonaws.com"}))
            self.assertFalse(allows(policy, "iam:PassRole", f"arn:aws:iam::{ACCOUNT}:role/Administrator", {"iam:PassedToService": "lambda.amazonaws.com"}))
            self.assertTrue(allows(policy, "iam:AttachRolePolicy", role, {"iam:PolicyARN": "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"}))
            self.assertFalse(allows(policy, "iam:AttachRolePolicy", role, {"iam:PolicyARN": "arn:aws:iam::aws:policy/AdministratorAccess"}))

    def test_dns_and_certificate_changes_are_scoped(self):
        for stage in ("dev", "prod"):
            policy = self.policy("Api", stage)
            domain = "api-dev.galashow.cloud" if stage == "dev" else "api.galashow.cloud"
            zone = f"arn:aws:route53:::hostedzone/{self.parameters['HostedZoneId']}"
            condition = {"route53:ChangeResourceRecordSetsNormalizedRecordNames": [domain], "route53:ChangeResourceRecordSetsRecordTypes": ["A"]}
            self.assertTrue(allows(policy, "route53:ChangeResourceRecordSets", zone, condition))
            self.assertFalse(allows(policy, "route53:ChangeResourceRecordSets", zone, {**condition, "route53:ChangeResourceRecordSetsNormalizedRecordNames": ["galashow.cloud"]}))
            self.assertFalse(allows(policy, "route53:ChangeResourceRecordSets", zone, {}))
            cert = f"arn:aws:acm:{REGION}:{ACCOUNT}:certificate/example"
            self.assertTrue(allows(policy, "acm:DeleteCertificate", cert, {"aws:ResourceTag/aws:cloudformation:stack-name": f"galashow-cloud-api-{stage}"}))
            self.assertFalse(allows(policy, "acm:DeleteCertificate", cert, {"aws:ResourceTag/aws:cloudformation:stack-name": "unrelated"}))

    def test_no_deployer_reads_secrets_or_manages_identity_provider(self):
        for role in self.roles.values():
            policy = role["Policies"][0]["PolicyDocument"]
            for action in ("secretsmanager:GetSecretValue", "iam:CreateUser", "iam:CreateAccessKey", "iam:UpdateOpenIDConnectProviderThumbprint"):
                self.assertFalse(allows(policy, action, "*"))
            self.assertLessEqual(len(json.dumps(policy, separators=(",", ":"))), 10240)


if __name__ == "__main__":
    unittest.main()
