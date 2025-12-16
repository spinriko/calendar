# Skipping Forced HTTPS for a Specific IIS Application (e.g., PTO Track)

If your IIS server has a global rewrite rule to force HTTPS, but you want to allow HTTP traffic to a specific application (such as `/pto-track`) for testing, you can add a condition to the rewrite rule to skip the redirect for that path.

## Example: Standard HTTPS Redirect Rule

```xml
<rule name="Redirect to HTTPS" stopProcessing="true">
  <match url="(.*)" />
  <conditions>
    <add input="{HTTPS}" pattern="off" ignoreCase="true" />
  </conditions>
  <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
</rule>
```

## Modified Rule to Skip /pto-track

```xml
<rule name="Redirect to HTTPS" stopProcessing="true">
  <match url="(.*)" />
  <conditions>
    <add input="{HTTPS}" pattern="off" ignoreCase="true" />
    <add input="{REQUEST_URI}" pattern="^/pto-track" negate="true" />
  </conditions>
  <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
</rule>
```

This will allow HTTP traffic to `/pto-track` while still forcing HTTPS for the rest of the site.

**Note:**
- Place this rule in the web.config at the site root (not inside the PTO Track app folder).
- Remove or update this exception when you are ready to enforce HTTPS everywhere.
