# Verification evidence

Executed locally with .NET SDK 10.0.401 on October 2, 2026.

`pwsh -File scripts/verify.ps1` exited 0.

- All five normalized records matched expected IDs, terminals, event types, carrier values, timestamp strings and timestamp basis.
- Counts: missed_pickup 2; doc_mismatch 2; carrier_substitution 1; total 5.
- Four distinct review records, five field issues. CPX-88216 retains its empty carrier; only CPX-88215 is UTC.
- CSV and JSON values agree.
- Malformed input and duplicate IDs exit 1 without publishing new outputs to fresh test directories.
- Unknown terminal and invalid timestamp remain unchanged and carry review notes.
- Initial reflection-based JSON serialization failed; source generation corrected it. PowerShell's automatic JSON date conversion initially caused a comparison failure; preserving strings in the verification reader corrected that check.
- No production routing service, integration, client review, or deployment has been performed.

Generated example outputs are retained under evidence/. To reproduce fresh outputs, follow README.md.

- Styled HTML verified for record values, review notes, UTC generation timestamp, print stylesheet, and HTML escaping of input. Browser print-to-PDF output has not been exported or visually verified.
- TimeProvider supplies generation time only; the four missing source timezones remain unspecified.
