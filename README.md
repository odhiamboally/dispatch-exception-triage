# Dispatch Exception Triage

Small assessment automation and delivery documents for the fictional Corrigan Peak Logistics engagement.

Requires .NET SDK 10.0.401 or a compatible .NET 10 SDK. From this directory:

```powershell
dotnet run --file NormalizeExceptions.cs -- data/exceptions.csv artifacts
pwsh -File scripts/verify.ps1
```

The app produces normalized.csv, normalized.json, and summary.json. Counts include all structurally valid input records, including records flagged for review. Blank carriers remain blank. Timestamp formatting is standardized, but missing timezones remain unspecified: four records need timezone confirmation and one of those also needs a carrier.

This is a local data-cleaning utility, not the production routing service or a fix for DET-121. It supports the exact simple CSV supplied in the brief; quoted input CSV, arbitrary schemas, and operational integrations are outside scope. The script rejects malformed rows and duplicate IDs before writing outputs.

Documents: SUBMISSION.md (paste into the portal), VIDEO-SCRIPT.md (four-minute target), CLIENT-UPDATE.md, and DELIVERY-PLAN.md.
The video URL and personal introduction/salary statements require Allan's completion.
