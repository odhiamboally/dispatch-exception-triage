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
