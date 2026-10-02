using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// Deliberately scoped to the supplied simple, unquoted five-column CSV.
// Ambiguous data is retained and reported; this app does not invent a timezone or carrier.
var input = args.Length > 0 ? args[0] : "data/exceptions.csv";
var output = args.Length > 1 ? args[1] : "artifacts";
try
{
    var lines = await File.ReadAllLinesAsync(input);
    const string header = "exception_id,terminal,event_type,carrier_code,event_ts";
    if (lines.Length == 0 || lines[0].Trim().TrimStart('\uFEFF') != header)
        throw new InvalidDataException("Expected the supplied five-column CSV header.");
    var rows = new List<NormalizedRecord>();
    var issues = new List<RecordIssue>();
    var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
    var ids = new HashSet<string>(StringComparer.Ordinal);
    for (var i = 1; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i])) continue;
        var columns = lines[i].Split(',');
        if (columns.Length != 5 || lines[i].Contains('"'))
            throw new InvalidDataException($"Line {i + 1}: expected five unquoted columns; no output published.");
        var id = columns[0].Trim();
        if (id.Length == 0 || !ids.Add(id))
            throw new InvalidDataException($"Line {i + 1}: missing or duplicate exception ID; no output published.");
        var terminal = columns[1].Trim() switch
        {
            "T3" => "Terminal 3",
            "Terminal 3" => "Terminal 3",
            var value => value
        };
        if (terminal != "Terminal 3")
            issues.Add(new(id, "terminal", "Unknown terminal alias retained; confirmation required."));
        var eventType = columns[2].Trim();
        if (eventType.Length == 0)
            throw new InvalidDataException($"Line {i + 1}: missing event type.");
        var carrier = columns[3].Trim().ToUpperInvariant();
        if (carrier.Length == 0)
            issues.Add(new(id, "carrier_code", "Missing carrier retained as blank; confirm with client IT."));
        var rawTime = columns[4].Trim();
        string timestamp;
        string timeBasis;
        if (DateTimeOffset.TryParseExact(rawTime, "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var utc))
        {
            timestamp = utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            timeBasis = "UTC";
        }
        else if (DateTime.TryParseExact(rawTime,
                ["yyyy-MM-dd HH:mm:ss", "MM/dd/yyyy HH:mm"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            timestamp = local.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            timeBasis = "Unspecified";
            issues.Add(new(id, "event_ts", "Formatting normalized; source timezone absent. Do not assume UTC."));
        }
        else
        {
            timestamp = rawTime;
            timeBasis = "Unparsed";
            issues.Add(new(id, "event_ts", "Unsupported timestamp retained; manual review required."));
        }
        rows.Add(new(id, terminal, eventType, carrier, timestamp, timeBasis));
        counts[eventType] = counts.GetValueOrDefault(eventType) + 1;
    }
    Directory.CreateDirectory(output);
    static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    var clean = new List<string> { header + ",timestamp_basis" };
    clean.AddRange(rows.Select(r => string.Join(",", new[]
        { r.ExceptionId, r.Terminal, r.EventType, r.CarrierCode, r.EventTimestamp, r.TimestampBasis }.Select(Csv))));
    await File.WriteAllLinesAsync(Path.Combine(output, "normalized.csv"), clean);
    var summary = new Summary(rows.Count, counts, issues.Select(x => x.ExceptionId).Distinct().Count(), issues);
    await File.WriteAllTextAsync(Path.Combine(output, "summary.json"),
        JsonSerializer.Serialize(summary, AppJsonContext.Default.Summary));
    await File.WriteAllTextAsync(Path.Combine(output, "normalized.json"), JsonSerializer.Serialize(rows, AppJsonContext.Default.ListNormalizedRecord));
    await File.WriteAllTextAsync(Path.Combine(output, "report.html"), HtmlReport.Render(rows, summary, TimeProvider.System));
    Console.WriteLine($"Processed {rows.Count} exceptions.");
    foreach (var entry in counts) Console.WriteLine($"{entry.Key}: {entry.Value}");
    Console.WriteLine($"Review required for {issues.Select(x => x.ExceptionId).Distinct().Count()} records ({issues.Count} field notes).");
    Console.WriteLine($"Outputs: {Path.GetFullPath(output)}");
    return 0;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

record NormalizedRecord(string ExceptionId, string Terminal, string EventType, string CarrierCode,
    string EventTimestamp, string TimestampBasis);
record RecordIssue(string ExceptionId, string Field, string Reason);

record Summary(int TotalRecords, SortedDictionary<string, int> CountsByEventType, int RecordsRequiringReview, List<RecordIssue> Issues);
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(Summary))]
[JsonSerializable(typeof(List<NormalizedRecord>))]
internal partial class AppJsonContext : JsonSerializerContext { }

static class HtmlReport
{
    public static string Render(IReadOnlyList<NormalizedRecord> rows, Summary summary, TimeProvider clock)
    {
        static string E(string text) => System.Net.WebUtility.HtmlEncode(text);
        var generated = clock.GetUtcNow().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
        var countRows = string.Concat(summary.CountsByEventType.Select(x =>
            $"<tr><td>{E(x.Key)}</td><td>{x.Value}</td></tr>"));
        var recordRows = string.Concat(rows.Select(r =>
            $"<tr><td>{E(r.ExceptionId)}</td><td>{E(r.Terminal)}</td><td>{E(r.EventType)}</td><td>{(r.CarrierCode.Length == 0 ? "<strong class=\"review\">Missing</strong>" : E(r.CarrierCode))}</td><td>{E(r.EventTimestamp)}</td><td>{E(r.TimestampBasis)}</td></tr>"));
        var issueRows = string.Concat(summary.Issues.Select(i =>
            $"<tr><td>{E(i.ExceptionId)}</td><td>{E(i.Field)}</td><td>{E(i.Reason)}</td></tr>"));
        if (issueRows.Length == 0) issueRows = "<tr><td colspan=\"3\">No records require review.</td></tr>";
        return $$"""
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Dispatch Exception Triage — Data Quality Report</title>
<style>
:root { --ink:#16324b; --teal:#006d77; --amber:#805400; --paper:#f2f6f8; }
* { box-sizing:border-box; }
body { margin:0; background:var(--paper); color:var(--ink); font:15px/1.5 system-ui, sans-serif; }
main { max-width:1150px; margin:32px auto; padding:32px; background:white; border-radius:12px; }
header { border-top:6px solid var(--teal); padding-top:18px; }
h1 { margin:4px 0; font-size:30px; } h2 { margin-top:28px; font-size:21px; }
.subtitle { color:#455c70; } .metrics { display:flex; gap:16px; margin:24px 0; flex-wrap:wrap; }
.metric { padding:16px 22px; background:#e9f5f4; border-left:4px solid var(--teal); flex:1; min-width:180px; }
.metric strong { display:block; font-size:30px; } .notice { padding:16px; background:#fff5da; border-left:4px solid var(--amber); }
table { width:100%; border-collapse:collapse; font-size:13px; }
th { background:var(--ink); color:white; text-align:left; }
th, td { padding:10px; border-bottom:1px solid #d8e2e8; vertical-align:top; overflow-wrap:anywhere; }
tr:nth-child(even) { background:#f4f8fa; } .review { color:var(--amber); }
.table-wrap { overflow-x:auto; } footer { margin-top:28px; color:#455c70; font-size:13px; }
@page { size:A4 landscape; margin:12mm; }
@media print {
 body { background:white; font-size:11px; } main { max-width:none; margin:0; padding:0; }
 h1 { font-size:24px; } h2 { font-size:17px; } th,td { padding:6px; }
 .table-wrap { overflow:visible; } thead { display:table-header-group; }
 tr { break-inside:avoid; } h2 { break-after:avoid; } .metrics,.notice { break-inside:avoid; }
}
</style>
</head>
<body><main>
<header><div class="subtitle">Corrigan Peak Logistics · Dispatch Exception Triage</div>
<h1>Exception Data Quality Report</h1><p class="subtitle">Generated {{E(generated)}} · Report time is separate from source event times.</p></header>
<section class="metrics" aria-label="Summary">
<div class="metric"><strong>{{summary.TotalRecords}}</strong>Total exceptions</div>
<div class="metric"><strong>{{summary.RecordsRequiringReview}}</strong>Records requiring review</div>
<div class="metric"><strong>{{summary.Issues.Count}}</strong>Field review notes</div>
</section>
<div class="notice"><strong>Human review required where flagged.</strong> Missing carriers remain unknown. Source timestamps without timezone information are not labelled UTC. Counts include records awaiting review.</div>
<h2>Exceptions by event type</h2>
<table><thead><tr><th scope="col">Event type</th><th scope="col">Count</th></tr></thead><tbody>{{countRows}}</tbody></table>
<h2>Normalized records</h2>
<div class="table-wrap"><table><thead><tr><th scope="col">Exception ID</th><th scope="col">Terminal</th><th scope="col">Event type</th><th scope="col">Carrier</th><th scope="col">Event timestamp</th><th scope="col">Time basis</th></tr></thead><tbody>{{recordRows}}</tbody></table></div>
<h2>Records requiring confirmation</h2>
<table><thead><tr><th scope="col">Exception ID</th><th scope="col">Field</th><th scope="col">Reason</th></tr></thead><tbody>{{issueRows}}</tbody></table>
<footer>Local export-normalization report. This does not confirm production scoring or routing readiness. Open your browser's Print dialog and choose Save as PDF to export this report.</footer>
</main></body></html>
""";
    }
}
