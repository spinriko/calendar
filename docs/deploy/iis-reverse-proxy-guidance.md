# IIS Reverse Proxy and Rewrite Rules for PTO Track Deployment

## Key Points for IIS Configuration

- **Do NOT duplicate rewrite/proxy rules in both the Default Web Site and the PTO Track application.**
- Only one location should contain the rewrite/proxy rules, depending on your routing needs.

## Standard Scenario: PTO Track App at /pto-track

If you want all traffic to `http://yourserver/pto-track` to be handled by the PTO Track app, and you have set up an IIS application at `/pto-track` with its own physical path (e.g., `C:\standalone\pto-track`):

- **Place the rewrite/proxy rules only in the web.config inside the PTO Track application folder** (e.g., `C:\standalone\pto-track\web.config`).
- **Do NOT add rewrite/proxy rules for /pto-track in the Default Web Site’s root web.config.** IIS will automatically route `/pto-track` requests to the PTO Track app.

This ensures:
- Static files (like CSS, JS, images) are served directly by IIS from the PTO Track app folder.
- Only non-static requests are proxied to the backend (Kestrel).

## Alternate Scenario: Proxying from Root

If you want to proxy all traffic from the root (`/`) to `/pto-track`, then you would use rewrite/proxy rules in the Default Web Site’s root web.config. This is a different scenario and not needed for standard PTO Track deployments.

## Summary

- Place rewrite/proxy rules **only** in the PTO Track app’s web.config (`C:\standalone\pto-track\web.config`).
- Do **not** duplicate these rules in the Default Web Site’s root web.config.
- This setup ensures static files and reverse proxying work as expected.
