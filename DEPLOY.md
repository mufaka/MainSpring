# Deploying MainSpringTwo.Web with systemd on Linux

This document explains how to publish and run `MainSpringTwo.Web` as a Linux service with `systemd`.

## Prerequisites

Install the following on the target machine:

- .NET 10 ASP.NET Core runtime (or .NET 10 SDK if you want to publish on the server)
- `systemd`
- `node` and `npm` if you plan to run `dotnet publish` on the server

> `MainSpringTwo.Web.csproj` runs `npm install` and `npm run css:build` during build/publish, so Node.js is required anywhere you publish the app.

## 1. Publish the application

From the repository root:

```bash
dotnet publish MainSpringTwo.Web/MainSpringTwo.Web.csproj -c Release -o ./publish/MainSpringTwo.Web
```

This produces a deployable output directory containing `MainSpringTwo.Web.dll`.

## 2. Copy the published files to the server

Example destination:

```bash
sudo mkdir -p /opt/mainspring
sudo cp -R ./publish/MainSpringTwo.Web/* /opt/mainspring/
```

## 3. Create a service account

Run the app under a dedicated non-login user:

```bash
sudo useradd --system --home /opt/mainspring --shell /usr/sbin/nologin mainspring
sudo chown -R mainspring:mainspring /opt/mainspring
```

## 4. Configure the application

The default configuration uses SQLite:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=mainspring.db"
}
```

Because the database path is relative, the service must use the app folder as its working directory. The `systemd` unit below does that.

If you need environment-specific settings, create `/opt/mainspring/appsettings.Production.json` and override values there.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/opt/mainspring/data/mainspring.db"
  }
}
```

If you change the database path to another folder, make sure the `mainspring` user can write to it.

## 5. Create the systemd unit

`MainSpringTwo.Web` is configured to integrate with `systemd` notifications and graceful shutdown, so use `Type=notify` and let `systemd` send its default stop signal.

Create `/etc/systemd/system/mainspring.service`:

```ini
[Unit]
Description=MainSpringTwo.Web
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/mainspring
ExecStart=/usr/bin/dotnet /opt/mainspring/MainSpringTwo.Web.dll
Restart=always
RestartSec=10
TimeoutStopSec=90
SyslogIdentifier=mainspring
User=mainspring
Group=mainspring
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
```

Notes:

- `WorkingDirectory=/opt/mainspring` is important because the app uses a relative SQLite path by default.
- `Type=notify` allows `systemd` to track when the app has fully started and when it is stopping.
- `TimeoutStopSec=90` gives the app time to finish in-flight requests before `systemd` forces termination.
- The application applies EF Core migrations on startup.
- The app enables HTTPS redirection outside Development, so in production it is usually best to run it behind a reverse proxy that terminates TLS.

## 6. Enable and start the service

```bash
sudo systemctl daemon-reload
sudo systemctl enable mainspring
sudo systemctl start mainspring
```

Check status:

```bash
sudo systemctl status mainspring
```

View logs:

```bash
journalctl -u mainspring -f
```

## 7. Optional: run behind Nginx

A common production setup is:

- `MainSpringTwo.Web` listening on `http://127.0.0.1:5000`
- Nginx handling HTTPS and forwarding requests to the app

If you do that, update the service file to:

```ini
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
```

## 8. Updating the deployment

When deploying a new version:

```bash
sudo systemctl stop mainspring
sudo cp -R /path/to/new/publish/* /opt/mainspring/
sudo chown -R mainspring:mainspring /opt/mainspring
sudo systemctl start mainspring
```

## Troubleshooting

### Service will not start

Check:

```bash
sudo systemctl status mainspring
journalctl -u mainspring -n 200 --no-pager
```

### Database errors

Make sure the configured SQLite file path exists and is writable by the `mainspring` user.

### Port binding issues

Make sure the configured port is open and not already in use.

## Recommended production notes

- Use a reverse proxy such as Nginx for TLS termination.
- Store persistent SQLite data outside the deployment folder if you want simpler upgrades and backups.
- Back up the SQLite database before replacing application files.
