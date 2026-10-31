import argparse
import html
from html.parser import HTMLParser
import http.cookiejar
import json
from pathlib import Path
import re
import ssl
import subprocess
import urllib.error
import urllib.parse
import urllib.request
import uuid


PROJECT = "rpbit-security-test"
APP = Path(__file__).resolve().parents[1]
BASE = "https://localhost:18580"
MAIL = "http://127.0.0.1:18581"
PASSWORD = "A long test passphrase 42!"
CONNECTION = "Host=db;Database=security_test;Username=security_test;Password=temporary-test-only"


class Forms(HTMLParser):
    def __init__(self):
        super().__init__()
        self.forms = []
        self.scripts = []
        self.current = None

    def handle_starttag(self, tag, attributes):
        attributes = dict(attributes)
        if tag == "script":
            self.scripts.append(attributes)
        if tag == "form":
            self.current = {"action": attributes.get("action", ""), "values": {}}
            self.forms.append(self.current)
        if tag == "input" and self.current is not None and attributes.get("name"):
            self.current["values"][attributes["name"]] = attributes.get("value", "")

    def handle_endtag(self, tag):
        if tag == "form":
            self.current = None


class Browser:
    def __init__(self):
        self.cookies = http.cookiejar.CookieJar()
        self.client = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(self.cookies),
            urllib.request.HTTPSHandler(context=ssl.create_default_context()),
        )

    def request(self, path, values=None, headers=None):
        data = None if values is None else urllib.parse.urlencode(values).encode()
        request = urllib.request.Request(BASE + path, data=data, headers=headers or {})
        try:
            response = self.client.open(request, timeout=30)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            return response.status, response.geturl().removeprefix(BASE), html.unescape(response.read().decode()), response.headers

    def post(self, path, values):
        status, _, content, _ = self.request(path)
        assert status == 200, (path, status)
        return self.request(path, dict(values, __RequestVerificationToken=token(content)))


def forms(content):
    parsed = Forms()
    parsed.feed(content)
    return parsed.forms


def token(content):
    return next(form["values"]["__RequestVerificationToken"] for form in forms(content)
                if "__RequestVerificationToken" in form["values"])


def fixture(image):
    environment = {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://+:8443",
        "ASPNETCORE_Kestrel__Certificates__Default__Path": "/https/localhost.pem",
        "ASPNETCORE_Kestrel__Certificates__Default__KeyPath": "/https/localhost-key.pem",
        "ConnectionStrings__DefaultConnection": CONNECTION,
        "AllowedHosts": "localhost;127.0.0.1",
        "ContactMail__Host": "mailpit",
        "ContactMail__Port": "1025",
        "ContactMail__Security": "None",
        "ContactMail__TestMode": "true",
    }
    return {"services": {
        "db": {"image": "postgres:18", "environment": {
            "POSTGRES_DB": "security_test", "POSTGRES_USER": "security_test",
            "POSTGRES_PASSWORD": "temporary-test-only"}, "tmpfs": ["/var/lib/postgresql"],
            "healthcheck": {"test": ["CMD-SHELL", "pg_isready -U security_test -d security_test"],
                            "interval": "1s", "timeout": "3s", "retries": 60}},
        "mailpit": {"image": "axllent/mailpit:v1.27", "ports": ["127.0.0.1:18581:8025"]},
        "app": {"image": image, "environment": environment,
            "ports": ["127.0.0.1:18580:8443"],
            "volumes": [str(APP / "certs") + ":/https:ro", "keys:/var/lib/rpbit/keys"],
            "read_only": True, "cap_drop": ["ALL"],
            "security_opt": ["no-new-privileges:true"], "tmpfs": ["/tmp:rw,noexec,nosuid,size=32m"],
            "healthcheck": {"test": ["CMD", "bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8443"],
                            "interval": "1s", "timeout": "3s", "retries": 60},
            "depends_on": {"db": {"condition": "service_healthy"}}}}, "volumes": {"keys": {}}}


def compose(config, *arguments):
    subprocess.run(["docker", "compose", "--project-name", PROJECT, "--file", "-", *arguments],
                   input=json.dumps(config), text=True, check=True)


def sql(statement, **variables):
    arguments = ["docker", "exec", "-i", PROJECT + "-db-1", "psql", "-XAt",
                 "-U", "security_test", "-d", "security_test", "-v", "ON_ERROR_STOP=1"]
    for name, value in variables.items():
        arguments += ["-v", name + "=" + value]
    return subprocess.run(arguments, input=statement, text=True, check=True, capture_output=True).stdout.strip()


def mail_link(email, route):
    with urllib.request.urlopen(MAIL + "/api/v1/messages", timeout=20) as response:
        messages = json.load(response)["messages"]
    for message in messages:
        if not any(recipient["Address"] == email for recipient in message["To"]):
            continue
        with urllib.request.urlopen(MAIL + "/api/v1/message/" + message["ID"], timeout=20) as response:
            body = json.load(response)["Text"]
        for link in re.findall(r"https://[^\s]+", body):
            if route in link:
                assert link.startswith(BASE + "/"), "Untrusted confirmation origin"
                return link.removeprefix(BASE)
    raise AssertionError("Expected Identity message not found in isolated Mailpit")


def run_checks(config, image):
    browser = Browser()
    status, _, content, headers = browser.request("/Products")
    assert status == 200 and '<html lang="pl">' in content
    csp = headers["Content-Security-Policy"]
    assert "'unsafe-inline'" not in csp and "'unsafe-eval'" not in csp
    assert "frame-ancestors 'none'" in csp and "object-src 'none'" in csp
    nonce = re.search(r"'nonce-([^']+)'", csp).group(1)
    document = Forms()
    document.feed(content)
    assert all(script.get("src") or script.get("nonce") == nonce for script in document.scripts)
    assert headers["X-Content-Type-Options"] == "nosniff" and headers["Cache-Control"] == "no-store"
    assert "Server" not in headers
    assert browser.request("/Products", headers={"Host": "attacker.invalid"})[0] == 400
    assert browser.request("/Products?page=-1")[0] == 400
    assert browser.request("/Products?page=2147483647")[0] == 400
    assert browser.request("/Identity/Account/Manage")[1].startswith("/Identity/Account/Login")
    for path in ["/Cart/Add", "/Language/Change", "/Contact", "/Identity/Account/Login", "/Identity/Account/Register"]:
        assert browser.request(path, {})[0] == 400, (path, "CSRF")
    print("PASS: CSP/nonce, headers, host validation, pagination bounds, authorization, CSRF")

    for culture in ("pl-PL", "en-GB"):
        status, _, privacy, headers = Browser().request("/Home/Privacy", headers={
            "Cookie": f".AspNetCore.Culture=c={culture}|uic={culture}"})
        assert status == 200 and headers["Content-Language"] == culture
        assert privacy.count("mailto:contact@rp-bit.net") == 2
        assert "Microsoft 365 / Exchange Online" in privacy and "PL9671255358" in privacy
        assert "Google Fonts" in privacy and "Google Workspace" not in privacy
        assert "mailto:contact@rp-bit.com" not in privacy
    print("PASS: PL/EN privacy, Microsoft 365 mail provider and contact@rp-bit.net")

    contact_email = "contact-" + uuid.uuid4().hex + "@example.invalid"
    contact_body = "SMTP routing regression check " + uuid.uuid4().hex
    status, path, _, _ = Browser().post("/Contact", {
        "Name": "SMTP regression test", "Email": contact_email,
        "Topic": "Other", "Message": contact_body})
    assert status == 200 and path == "/Contact/Sent"
    with urllib.request.urlopen(MAIL + "/api/v1/messages", timeout=20) as response:
        messages = json.load(response)["messages"]
    message = next(message for message in messages
                   if message["To"] == [{"Name": "", "Address": "contact@rp-bit.net"}])
    assert message["From"]["Address"] == "contact@rp-bit.net"
    with urllib.request.urlopen(MAIL + "/api/v1/message/" + message["ID"], timeout=20) as response:
        delivery = json.load(response)
    assert delivery["ReplyTo"] == [{"Name": "", "Address": contact_email}]
    assert contact_body in delivery["Text"]
    print("PASS: contact goes to contact@rp-bit.net with visitor Reply-To in isolated Mailpit")

    add = next(form for form in forms(content) if form["action"] == "/Cart/Add")
    for _ in range(21):
        status, _, basket, _ = browser.request("/Cart/Add", add["values"])
        assert status == 200
    assert "Osiągnięto limit koszyka" in basket
    status, path, english, _ = browser.request("/Language/Change", {
        "culture": "en", "returnUrl": "/Cart?test=language", "__RequestVerificationToken": token(basket)})
    assert status == 200 and path == "/Cart?test=language" and "Service basket" in english and "productId" in english
    assert browser.request("/Language/Change", {"culture": "de", "__RequestVerificationToken": token(english)})[0] == 400
    assert browser.request("/Language/Change", {"culture": "pl", "returnUrl": "https://attacker.invalid/", "__RequestVerificationToken": token(english)})[1] == "/Products"
    for cookie in browser.cookies:
        assert cookie.secure, cookie.name
        assert "httponly" in {key.lower() for key in cookie._rest}, cookie.name
    print("PASS: basket cap, PL/EN session retention, Secure/HttpOnly cookies, local redirects")

    email = "security-" + uuid.uuid4().hex + "@example.invalid"
    status, path, confirmation, _ = browser.post("/Identity/Account/Register", {
        "Input.Email": email, "Input.Password": PASSWORD, "Input.ConfirmPassword": PASSWORD})
    assert status == 200 and "RegisterConfirmation" in path
    assert "confirm-link" not in confirmation and "code=" not in confirmation
    assert browser.request("/Identity/Account/RegisterConfirmation?email=unknown@example.invalid")[0] == 200
    status, path, _, _ = browser.post("/Identity/Account/Login", {"Input.Email": email, "Input.Password": PASSWORD})
    assert status == 200 and "/Login" in path, "Unconfirmed account authenticated"
    assert browser.request(mail_link(email, "/ConfirmEmail"))[0] == 200
    for _ in range(5):
        browser.post("/Identity/Account/Login", {"Input.Email": email, "Input.Password": "Incorrect test password"})
    assert sql('SELECT "LockoutEnd" > now() FROM "AspNetUsers" WHERE "Email" = :\'email\';', email=email) == "t"
    assert "/Login" in browser.post("/Identity/Account/Login", {"Input.Email": email, "Input.Password": PASSWORD})[1]
    sql('UPDATE "AspNetUsers" SET "LockoutEnd" = NULL, "AccessFailedCount" = 0 WHERE "Email" = :\'email\';', email=email)
    assert "/Login" not in browser.post("/Identity/Account/Login", {"Input.Email": email, "Input.Password": PASSWORD})[1]
    assert browser.request("/Identity/Account/Manage")[0] == 200
    print("PASS: no confirmation token in HTML, SMTP confirmation, unconfirmed-account rejection, lockout, sign-in")

    compose(config, "up", "--detach", "--force-recreate", "--no-deps", "--wait", "--wait-timeout", "90", "app")
    assert browser.request("/Identity/Account/Manage")[1] == "/Identity/Account/Manage", "Persistent key ring failed"
    print("PASS: authentication survives container recreation with persistent keys")

    recovery = Browser()
    status, _, _, _ = recovery.post("/Identity/Account/ForgotPassword", {"Input.Email": email})
    assert status == 200
    reset_path = mail_link(email, "/ResetPassword")
    reset_page = recovery.request(reset_path)[2]
    reset_form = next(form["values"] for form in forms(reset_page) if "Input.Code" in form["values"])
    new_password = "A different long test passphrase 43!"
    status, path, _, _ = recovery.request(reset_path, dict(reset_form, **{
        "Input.Email": email, "Input.Password": new_password, "Input.ConfirmPassword": new_password}))
    assert status == 200 and "ResetPasswordConfirmation" in path
    assert "/Login" not in recovery.post("/Identity/Account/Login", {"Input.Email": email, "Input.Password": new_password})[1]
    print("PASS: SMTP password recovery and sign-in using the new password")

    invalid = subprocess.run(["docker", "run", "--rm", image], capture_output=True, text=True)
    assert invalid.returncode != 0 and "Production requires an HTTPS" in invalid.stderr + invalid.stdout
    production = ["docker", "run", "--rm", "--network", PROJECT + "_default",
        "-e", "AllowedHosts=localhost", "-e", "Security__PublicOrigin=" + BASE,
        "-e", "ContactMail__Host=mailpit", "-e", "ContactMail__Port=1025",
        "-e", "ContactMail__Security=StartTls", "-e", "ContactMail__TestMode=false",
        "-e", "ConnectionStrings__DefaultConnection=" + CONNECTION]
    invalid = subprocess.run(production + [image], capture_output=True, text=True)
    assert invalid.returncode != 0 and "Production database role must not" in invalid.stderr + invalid.stdout
    print("PASS: Production rejects missing domain/configuration and administrator database role")

    sql("""
        CREATE ROLE security_runtime LOGIN PASSWORD 'runtime-test-only';
        REVOKE CREATE ON SCHEMA public FROM PUBLIC;
        GRANT CONNECT ON DATABASE security_test TO security_runtime;
        GRANT USAGE ON SCHEMA public TO security_runtime;
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO security_runtime;
        GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO security_runtime;
        """)
    runtime = json.loads(json.dumps(config))
    runtime["services"]["app"]["environment"].update({
        "ASPNETCORE_ENVIRONMENT": "Production", "Database__Initialize": "false",
        "AllowedHosts": "localhost;shop.example.invalid", "Security__PublicOrigin": "https://shop.example.invalid",
        "ContactMail__Security": "StartTls", "ContactMail__TestMode": "false",
        "ConnectionStrings__DefaultConnection": "Host=db;Database=security_test;Username=security_runtime;Password=runtime-test-only",
    })
    compose(runtime, "up", "--detach", "--force-recreate", "--no-deps", "--wait", "--wait-timeout", "90", "app")
    status, _, content, headers = Browser().request("/Products")
    assert status == 200 and "RP-bit" in content
    assert "max-age=31536000" in headers["Strict-Transport-Security"]
    print("PASS: Production starts and serves catalogue with least-privilege runtime role and HSTS")

    anonymous = Browser()
    responses = [anonymous.request("/Identity/Account/Login", {}) for _ in range(21)]
    assert responses[0][0] == 400 and responses[-1][0] == 429
    assert responses[-1][3]["Retry-After"] == "600"
    assert anonymous.request("/Identity/Account/Login", {}, headers={
        "X-Forwarded-For": "198.51.100.23", "X-Forwarded-Proto": "https"})[0] == 429
    print("PASS: account POST rate limit and Retry-After")


def main():
    parser = argparse.ArgumentParser(description="Isolated RP-bit security regression checks; requires Docker and trusted local mkcert certificates.")
    parser.add_argument("--image", default="rpbit-security-check")
    parser.add_argument("--no-build", action="store_true")
    parser.add_argument("--keep", action="store_true", help="Keep the isolated environment for browser inspection; clean with --cleanup.")
    parser.add_argument("--cleanup", action="store_true")
    arguments = parser.parse_args()
    config = fixture(arguments.image)
    if arguments.cleanup:
        compose(config, "down", "--volumes", "--remove-orphans")
        return
    if not arguments.no_build:
        subprocess.run(["docker", "build", "--tag", arguments.image, str(APP)], check=True)
    try:
        compose(config, "down", "--volumes", "--remove-orphans")
        compose(config, "up", "--detach", "--wait", "--wait-timeout", "90")
        run_checks(config, arguments.image)
        print("All security regression checks passed. No real data or SMTP credentials were used.")
    finally:
        if not arguments.keep:
            compose(config, "down", "--volumes", "--remove-orphans")


if __name__ == "__main__":
    main()