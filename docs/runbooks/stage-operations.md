# Stage operations

## Health
- `curl -f https://stage.example.com/health/live`
- `curl -f https://stage.example.com/health/ready`
- `curl -f https://stage.example.com/api/system/version`

## Service status
- `sudo systemctl status analytics-api`
- `sudo systemctl status analytics-worker`

## Restart
- `sudo systemctl restart analytics-api`
- `sudo systemctl restart analytics-worker`

## Logs
- `sudo journalctl -u analytics-api -n 200 --no-pager`
- `sudo journalctl -u analytics-worker -n 200 --no-pager`
- `sudo tail -n 200 /var/log/nginx/access.log`
- `sudo tail -n 200 /var/log/nginx/error.log`

## Incident hints
- if API readiness is red, check PostgreSQL connectivity and env files
- if Worker lags, inspect worker journal and retry pipeline state
- if Nginx fails, run `sudo nginx -t`