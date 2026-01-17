# PowerShell Strict Mode

Before generating PowerShell:
1. Perform a self-review for syntax, quoting, escaping, and parameter names.
2. Validate cmdlet names and ensure they exist.
3. Ensure the script is runnable as-is.
4. Fix issues silently before returning the final script.
5. Prefer idempotent logic for configuration tasks.

Return only the corrected final script.