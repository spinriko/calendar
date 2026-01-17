# PowerShell Strict Mode – Additive Rules

What chat instructions are active in this workspace?

These rules apply only when Copilot is generating or modifying PowerShell.

## Requirements
1. Perform a self‑review before returning any PowerShell code.
2. Validate syntax, quoting, escaping, and parameter names.
3. Ensure all cmdlet names and parameter sets actually exist.
4. Fix issues silently before returning the final script.
5. Prefer idempotent logic for configuration and system‑state tasks.
6. Avoid inventing cmdlets, parameters, or modules.
7. When uncertain about intent, ask clarifying questions before generating code.
8. code defensively agains the use of special characters in strings (e.g., quotes, dollar signs, backticks).

## Output Expectations
- Return only the corrected final script unless the user explicitly asks for explanation.
- Do not include speculative constructs or placeholders.
- Ensure the script is runnable as‑is.
