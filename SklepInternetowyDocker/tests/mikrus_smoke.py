import argparse
import http.client
import json
import os
from pathlib import Path
import re
import subprocess
import uuid


DEPLOYMENT = Path(__file__).resolve().parents[2] / "deploy" / "mikrus"


def main():
    parser = argparse.ArgumentParser(description="Isolated production Compose checks; no real tunnel or SMTP connection.")
    parser.add_argument("--image", default="sklepinternetowydocker-app:latest")
    arguments = parser.parse_args()
    project = "rpbit-mikrus-test-" + uuid.uuid4().hex[:8]
    environment = dict(os.environ, APP_IMAGE=arguments.image,
        POSTGRES_PASSWORD="temporary-admin-test", APP_DB_PASSWORD="temporary-runtime-test",
        MIGRATION_DB_PASSWORD="temporary-migrator-test", CONTACT_SMTP_HOST="smtp.example.invalid",
        CONTACT_SMTP_USERNAME="test", CONTACT_SMTP_PASSWORD="temporary-smtp-test",
        CONTACT_SMTP_FROM="shop@example.invalid", CONTACT_SMTP_TO="shop@example.invalid")
    rendered = subprocess.run(["docker", "compose", "--project-name", project,
        "--env-file", str(DEPLOYMENT / ".env.production.example"),
        "--file", str(DEPLOYMENT / "compose.production.yaml"), "--profile", "tools",
        "config", "--format", "json"], env=environment, capture_output=True, text=True, check=True)
    config = json.loads(rendered.stdout)
    assert not config["services"]["app"].get("ports") and not config["services"]["db"].get("ports")
    assert config["services"]["app"]["environment"]["ASPNETCORE_ENVIRONMENT"] == "Production"
    assert "rpbit_app;" in config["services"]["app"]["environment"]["ConnectionStrings__DefaultConnection"]
    assert config["networks"]["database"]["internal"]
    del config["services"]["cloudflared"]
    config["services"]["db"].pop("volumes")
    config["services"]["db"]["tmpfs"] = ["/var/lib/postgresql"]
    for service in ["app", "db-init"]:
        config["services"][service]["volumes"] = [
            {"type": "volume", "source": "keys_test", "target": "/var/lib/rpbit/keys"}]
    config["volumes"] = {"keys_test": {}}
    config["services"]["nginx"]["ports"] = [
        {"target": 8080, "published": "0", "host_ip": "127.0.0.1", "protocol": "tcp"}]

    def compose(*commands, capture=False):
        return subprocess.run(["docker", "compose", "--project-name", project, "--file", "-", *commands],
            input=json.dumps(config), text=True, check=True, capture_output=capture)

    def sql(statement):
        return subprocess.run(["docker", "exec", "-i", project + "-db-1", "psql", "-XAt",
            "-v", "ON_ERROR_STOP=1", "-U", "rpbit_admin", "-d", "rpbit"],
            input=statement, text=True, check=True, capture_output=True).stdout.strip()

    try:
        compose("pull", "db", "nginx")
        compose("up", "--detach", "--wait", "--wait-timeout", "120", "db")
        sql("""
            CREATE ROLE rpbit_migrator LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD 'temporary-migrator-test';
            CREATE ROLE rpbit_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD 'temporary-runtime-test';
            ALTER DATABASE rpbit OWNER TO rpbit_migrator;
            ALTER SCHEMA public OWNER TO rpbit_migrator;
            REVOKE ALL ON DATABASE rpbit FROM PUBLIC;
            GRANT CONNECT ON DATABASE rpbit TO rpbit_migrator, rpbit_app;
            REVOKE CREATE ON SCHEMA public FROM PUBLIC;
            GRANT USAGE ON SCHEMA public TO rpbit_app;
            ALTER DEFAULT PRIVILEGES FOR ROLE rpbit_migrator IN SCHEMA public
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO rpbit_app;
            ALTER DEFAULT PRIVILEGES FOR ROLE rpbit_migrator IN SCHEMA public
                GRANT USAGE, SELECT ON SEQUENCES TO rpbit_app;
        """)
        compose("run", "--rm", "--no-deps", "-T", "db-init")
        sql('REVOKE ALL ON TABLE public."__EFMigrationsHistory" FROM rpbit_app;')
        compose("up", "--detach", "--wait", "--wait-timeout", "120", "app", "nginx")
        compose("exec", "-T", "nginx", "nginx", "-t")
        port = int(compose("port", "nginx", "8080", capture=True).stdout.strip().rsplit(":", 1)[1])

        def request(path="/Products", method="GET", extra=None):
            connection = http.client.HTTPConnection("127.0.0.1", port, timeout=15)
            connection.request(method, path, body="" if method == "POST" else None,
                headers={"Host": "services.rp-bit.net", **(extra or {})})
            response = connection.getresponse()
            result = response.status, dict(response.getheaders()), response.read()
            connection.close()
            return result

        status, headers, body = request()
        assert status == 200 and b"RP-bit" in body
        assert "max-age=31536000" in headers["Strict-Transport-Security"]
        assert headers["Cache-Control"] == "no-store"
        status, headers, _ = request("/Identity/Account/Manage", extra={
            "X-Forwarded-Host": "attacker.invalid", "X-Forwarded-Proto": "http"})
        assert status == 302 and headers["Location"].startswith(
            "https://services.rp-bit.net/Identity/Account/Login"), headers
        try:
            request(extra={"Host": "attacker.invalid"})
            raise AssertionError("Nginx accepted an unknown Host")
        except http.client.RemoteDisconnected:
            pass
        print("PASS: Production startup, least-privilege database, Nginx, catalogue, HSTS, canonical HTTPS, Host rejection", flush=True)

        probe = """
            for attempt in $(seq 1 21); do
                wget -S -O /dev/null --post-data='' --header='Host: services.rp-bit.net' --header='CF-Connecting-IP: 198.51.100.10' --header="X-Forwarded-For: 203.0.113.$attempt" --header='X-Forwarded-Proto: http' http://nginx:8080/Identity/Account/Login 2>&1 || true
            done
            wget -S -O /dev/null --post-data='' --header='Host: services.rp-bit.net' --header='CF-Connecting-IP: 198.51.100.11' http://nginx:8080/Identity/Account/Login 2>&1 || true
            wget -S -O /dev/null --post-data='' --header='Host: services.rp-bit.net' --header='CF-Connecting-IP: 198.51.100.10' --header='X-Forwarded-For: 203.0.113.200' http://nginx:8080/Identity/Account/Login 2>&1 || true
        """
        result = subprocess.run(["docker", "run", "--rm", "-i", "--network", project + "_edge",
            "--ip", "172.30.51.11", "--entrypoint", "sh", config["services"]["nginx"]["image"]],
            input=probe, text=True, capture_output=True, check=True)
        statuses = [int(value) for value in re.findall(r"(?m)^\s+HTTP/1\.1 (\d{3})", result.stdout)]
        assert statuses == [400] * 20 + [429, 400, 429], statuses
        direct = [request("/Identity/Account/Login", "POST", {
            "CF-Connecting-IP": "203.0.113." + str(attempt),
            "X-Forwarded-For": "203.0.113." + str(attempt)})[0] for attempt in range(1, 22)]
        assert direct == [400] * 20 + [429], direct
        print("PASS: trusted client IP, separate per-client limits, forwarding-header spoof protection", flush=True)

        assert request("/Products?code=private-token-test")[0] == 200
        assert "private-token-test" not in compose("logs", "--no-log-prefix", "nginx", capture=True).stdout
        keys = compose("exec", "-T", "app", "ls", "/var/lib/rpbit/keys", capture=True).stdout
        assert "key-" in keys
        compose("up", "--detach", "--no-deps", "--force-recreate", "--wait", "--wait-timeout", "90", "app")
        assert request()[0] == 200
        assert compose("exec", "-T", "app", "ls", "/var/lib/rpbit/keys", capture=True).stdout == keys
        assert sql("SELECT has_schema_privilege('rpbit_app', 'public', 'CREATE');") == "f"
        print("PASS: no query tokens in access logs, persistent keys after recreation, no runtime schema CREATE", flush=True)
    except Exception:
        compose("logs", "--tail", "30", "app", "nginx")
        raise
    finally:
        compose("down", "--volumes", "--remove-orphans")


if __name__ == "__main__":
    main()