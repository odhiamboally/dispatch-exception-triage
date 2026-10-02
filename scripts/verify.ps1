$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
function Assert-True($condition, $message) { if (-not $condition) { throw $message } }
function Run-App($inputPath, $outputPath, $expectedExit) {
    & dotnet run --file NormalizeExceptions.cs -- $inputPath $outputPath
    Assert-True ($LASTEXITCODE -eq $expectedExit) "Unexpected exit code for $inputPath"
}
New-Item -ItemType Directory -Force artifacts | Out-Null
Run-App 'data/exceptions.csv' 'artifacts/verification' 0
$rows = @(Get-Content artifacts/verification/normalized.json -Raw | ConvertFrom-Json -DateKind String)
$summary = Get-Content artifacts/verification/summary.json -Raw | ConvertFrom-Json -DateKind String
Assert-True ($rows.Count -eq 5) 'Expected five rows'
$expected = @(
    @('CPX-88213','missed_pickup','SWFT','2026-08-14T09:12:00','Unspecified'),
    @('CPX-88214','doc_mismatch','SWFT','2026-08-14T09:45:00','Unspecified'),
    @('CPX-88215','missed_pickup','SWFT','2026-08-14T10:03:00Z','UTC'),
    @('CPX-88216','doc_mismatch','','2026-08-14T11:47:00','Unspecified'),
    @('CPX-88217','carrier_substitution','RLCX','2026-08-15T08:02:00','Unspecified')
)
for ($i = 0; $i -lt 5; $i++) {
    $r = $rows[$i]; $e = $expected[$i]
    Assert-True ($r.exception_id -eq $e[0] -and $r.terminal -ceq 'Terminal 3' -and
        $r.event_type -ceq $e[1] -and $r.carrier_code -ceq $e[2] -and
        $r.event_timestamp -ceq $e[3] -and $r.timestamp_basis -ceq $e[4]) "Row $i mismatch"
}
Assert-True ($summary.total_records -eq 5 -and $summary.counts_by_event_type.missed_pickup -eq 2 -and
    $summary.counts_by_event_type.doc_mismatch -eq 2 -and $summary.counts_by_event_type.carrier_substitution -eq 1) 'Count mismatch'
Assert-True ($summary.records_requiring_review -eq 4 -and @($summary.issues).Count -eq 5) 'Review count mismatch'
$timeIds = @($summary.issues | Where-Object field -eq 'event_ts' | ForEach-Object exception_id)
Assert-True (($timeIds -join ',') -eq 'CPX-88213,CPX-88214,CPX-88216,CPX-88217') 'Timezone issues mismatch'
$carrierIssues = @($summary.issues | Where-Object field -eq 'carrier_code')
Assert-True ($carrierIssues.Count -eq 1 -and $carrierIssues[0].exception_id -eq 'CPX-88216') 'Carrier issue mismatch'
$csvRows = @(Import-Csv artifacts/verification/normalized.csv)
for ($i = 0; $i -lt 5; $i++) {
    Assert-True ($csvRows[$i].exception_id -eq $rows[$i].exception_id -and
        $csvRows[$i].event_ts -ceq $rows[$i].event_timestamp -and
        $csvRows[$i].carrier_code -ceq $rows[$i].carrier_code -and
        $csvRows[$i].terminal -ceq $rows[$i].terminal -and
        $csvRows[$i].event_type -ceq $rows[$i].event_type -and
        $csvRows[$i].timestamp_basis -ceq $rows[$i].timestamp_basis) 'CSV/JSON mismatch'
}
$header = 'exception_id,terminal,event_type,carrier_code,event_ts'
[IO.File]::WriteAllLines("$PWD/artifacts/malformed.csv", @($header,'X,Terminal 3,missed_pickup,SWFT'))
Run-App 'artifacts/malformed.csv' 'artifacts/malformed-output' 1
Assert-True (-not (Test-Path artifacts/malformed-output/summary.json)) 'Malformed output should not exist'
[IO.File]::WriteAllLines("$PWD/artifacts/duplicate.csv", @($header,'X,T3,missed_pickup,SWFT,2026-08-14T10:03:00Z','X,T3,missed_pickup,SWFT,2026-08-14T10:03:00Z'))
Run-App 'artifacts/duplicate.csv' 'artifacts/duplicate-output' 1
Assert-True (-not (Test-Path artifacts/duplicate-output/summary.json)) 'Duplicate output should not exist'
[IO.File]::WriteAllLines("$PWD/artifacts/uncertain.csv", @($header,'Y,Unknown,missed_pickup,swft,not-a-date'))
Run-App 'artifacts/uncertain.csv' 'artifacts/uncertain-output' 0
$uncertain = Get-Content artifacts/uncertain-output/normalized.json -Raw | ConvertFrom-Json -DateKind String
$uncertainSummary = Get-Content artifacts/uncertain-output/summary.json -Raw | ConvertFrom-Json -DateKind String
Assert-True ($uncertain[0].terminal -eq 'Unknown' -and $uncertain[0].event_timestamp -eq 'not-a-date' -and $uncertain[0].timestamp_basis -eq 'Unparsed') 'Unknown values must be retained'
Assert-True ($uncertainSummary.records_requiring_review -eq 1 -and @($uncertainSummary.issues).Count -eq 2) 'Unknown values must be flagged'
'PASS: supplied records, event counts, CSV/JSON parity, uncertainty notes, malformed rows, duplicate IDs, unknown terminal, invalid timestamp.'

# The HTML must expose the same records and review notes, and escape input data.
$html = Get-Content artifacts/verification/report.html -Raw
foreach ($r in $rows) {
    Assert-True ($html.Contains($r.exception_id) -and $html.Contains($r.event_timestamp)) 'HTML record missing'
}
Assert-True ($html.Contains('Records requiring review') -and $html.Contains('Source timezone') -eq $false) 'HTML labels mismatch'
Assert-True ($html.Contains('source timezone absent') -and $html.Contains('@media print') -and $html.Contains('UTC · Report time')) 'HTML review, print layout or clock missing'
[IO.File]::WriteAllLines("$PWD/artifacts/html-escape.csv", @($header,'<script>alert(1)</script>,T3,missed_pickup,SWFT,2026-08-14T10:03:00Z'))
Run-App 'artifacts/html-escape.csv' 'artifacts/html-escape-output' 0
$escaped = Get-Content artifacts/html-escape-output/report.html -Raw
Assert-True ($escaped.Contains('&lt;script&gt;alert(1)&lt;/script&gt;') -and -not $escaped.Contains('<script>')) 'HTML input must be escaped'
'PASS: HTML record content, uncertainty notes, generation timestamp, print CSS and input escaping.'
