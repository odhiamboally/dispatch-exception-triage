# Dispatch Exception Triage

A .NET 10 command-line utility that cleans the supplied dispatch exception export and reports event counts and data requiring human review.

## Delivery documents

- [Weekly delivery plan and triage](DELIVERY-PLAN.md)
- [Dana's client status update](CLIENT-UPDATE.md)
- [Assessment submission](SUBMISSION.md)
- [Verification evidence](VERIFICATION.md)
- [Build source](NormalizeExceptions.cs)
- [Input CSV](data/exceptions.csv)
- [Example normalized CSV](evidence/normalized.csv)
- [Example summary](evidence/summary.json)
- [Example HTML report](evidence/report.html) (download/open locally to view; GitHub displays its source)

## 1. Install the prerequisites

Install the **.NET 10 SDK**, not only the runtime, from [Microsoft's .NET download page](https://dotnet.microsoft.com/download/dotnet/10.0). This repository selects SDK 10.0.401 or a later 10.0.4xx patch through global.json.

Open a terminal and check:

```powershell
dotnet --version
```

To run the optional automated verification, install [PowerShell 7.5 or later](https://learn.microsoft.com/powershell/scripting/install/installing-powershell). The verifier uses its JSON timestamp-preservation option. Check:

```powershell
pwsh --version
```

Git is needed only for the clone option below. The ZIP option does not require Git. No database, cloud account, API keys, web server, or solution file is needed.

## 2. Get the files

Choose **one** option.

### Option A: Clone with Git

Open a terminal in the parent folder where you want the repository, then run:

```powershell
git clone https://github.com/odhiamboally/dispatch-exception-triage.git
cd dispatch-exception-triage
```

The repository is public. Clone it without requesting access, or use the source ZIP below.

### Option B: Download or receive a ZIP

1. Open the [GitHub repository](https://github.com/odhiamboally/dispatch-exception-triage), select **Code**, then **Download ZIP**. Alternatively, use the source ZIP supplied with the assessment.
2. Extract the ZIP completely into a folder. Do not run files from inside the ZIP viewer.
3. Open the extracted folder containing README.md, NormalizeExceptions.cs, global.json, and the data folder. GitHub's ZIP usually adds a dispatch-exception-triage-main folder; the supplied ZIP places the files directly in the extraction folder.
4. Open a terminal in that folder. On Windows, open the folder in File Explorer, right-click an empty area, and choose **Open in Terminal**.
5. Confirm that this is the correct working directory:

```powershell
Get-ChildItem
```

You should see NormalizeExceptions.cs and the data folder.

## 3. Run the app

From the repository/extracted root, run:

```powershell
dotnet run --file NormalizeExceptions.cs -- data/exceptions.csv artifacts
```

The first run compiles the file-based app. A successful run prints:

```text
Processed 5 exceptions.
carrier_substitution: 1
doc_mismatch: 2
missed_pickup: 2
Review required for 4 records (5 field notes).
Outputs: <your folder>/artifacts
```

The final output path depends on your extraction location.

## 4. Inspect the results

The app creates the artifacts folder automatically and writes:

| File | Contents |
|---|---|
| artifacts/normalized.csv | Cleaned records, including timestamp_basis |
| artifacts/normalized.json | The same normalized records in JSON |
| artifacts/summary.json | Event counts and record-level review notes |
| artifacts/report.html | Styled report with summary, normalized records, review notes, and print layout |

On Windows PowerShell, inspect them with:

```powershell
Import-Csv artifacts/normalized.csv | Format-Table
Get-Content artifacts/summary.json
```

T3 becomes Terminal 3; carrier codes become uppercase; timestamp formatting becomes consistent. The missing carrier stays blank. Four timestamps lack a timezone and stay explicitly unspecified; only the timestamp with Z is UTC. All five records contribute to the counts, including records flagged for review.

To use a different input or output folder, provide both paths:

```powershell
dotnet run --file NormalizeExceptions.cs -- "data/exceptions.csv" "artifacts/another-run"
```

### Open the formatted report or save a PDF

Open artifacts/report.html in your browser (double-click it in File Explorer). The report uses navy, teal, and amber colours, with a separate UTC generation timestamp supplied by TimeProvider.

To export a PDF: press Ctrl+P (Cmd+P on macOS), choose Save as PDF, and use landscape orientation. Enable background graphics if you want the report colours included. PDF saving is a browser action; the app directly generates HTML.

## 5. Verify correctness

From the same root directory, run:

```powershell
pwsh -File scripts/verify.ps1
```

Expected final line:

```text
PASS: supplied records, event counts, CSV/JSON parity, uncertainty notes, malformed rows, duplicate IDs, unknown terminal, invalid timestamp.
```

The malformed and duplicate test cases deliberately print errors; the verifier expects those app runs to exit 1. The verification script itself exits successfully only when all checks pass. Verification files are written under artifacts.

## Troubleshooting and boundaries

- **dotnet is not recognized:** install the .NET SDK and reopen the terminal.
- **Compatible SDK not found:** install SDK 10.0.401 or a later 10.0.4xx patch matching global.json.
- **Input file not found:** run from the root containing the data folder, or supply an absolute input path.
- **DateKind parameter not recognized:** use PowerShell 7.5 or later for verification.
- **GitHub cannot be reached:** use the attached ZIP.
- **App exits with an error:** do not treat older outputs as a successful new run. Check the exit code and use a new output folder when diagnosing input errors.

This utility supports the exact five-column unquoted input supplied in the brief. It rejects malformed rows and duplicate IDs before writing new outputs; unknown terminals and unsupported timestamps are retained with review notes. It does not repair the production routing service or resolve the Terminal 3 EDI scoring contract.

## Repository contents and local preparation

Keep source, sample data, delivery documents, verification script, and checked example outputs in Git. Build caches, generated runs, ZIP packages, editor files, secrets, and the local video rehearsal script are ignored. The rehearsal script is kept locally and is excluded from new repository ZIPs.
