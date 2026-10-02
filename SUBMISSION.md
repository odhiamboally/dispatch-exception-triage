Video: **[ADD PUBLIC VIDEO LINK BEFORE SUBMISSION]**

# Dispatch Exception Triage — Allan Odhiambo

Prepared for Ajaia: https://ajaia.ai

Repository: [Dispatch Exception Triage](https://github.com/odhiamboally/dispatch-exception-triage)
- [Build source](https://github.com/odhiamboally/dispatch-exception-triage/blob/main/NormalizeExceptions.cs)
- [Run instructions](https://github.com/odhiamboally/dispatch-exception-triage/blob/main/README.md)
- [Delivery plan](https://github.com/odhiamboally/dispatch-exception-triage/blob/main/DELIVERY-PLAN.md)
- [Client status update](https://github.com/odhiamboally/dispatch-exception-triage/blob/main/CLIENT-UPDATE.md)
- [Verification evidence](https://github.com/odhiamboally/dispatch-exception-triage/blob/main/VERIFICATION.md)

Access: the repository is public. The source ZIP/build file is also available for a runnable attachment.

## Task 1 — Triage and weekly plan

Reasoning: read everything → identify launch risks and dependencies → assign actions and owners → correct the client update → execute and verify.

Read all five requests before acting. Use impact, launch dependency, and external lead time to rank work; use effort to sequence actions afterward. A two-minute contact starts a dependency but does not close the task.

These are proposed assignments and checkpoints, not commitments already agreed with the fictional team. Scenario dates are used as supplied; September 8 is not today's real-world deadline.

| Rank | Item | This week | Proposed owner | Dependency and completion evidence |
|---|---|---|---|---|
| 1 | C: DET-121, Terminal 3 null scores | Work immediately; contact IT today and request field confirmation within one business day | TPM coordinates client IT; Priya implements | Actual schema and representative payloads confirmed; allow the estimated two engineering days after response, then verify representative Terminal 3 scoring and explicit handling of missing/invalid fields |
| 2 | B: duplicate routing | Work before merge and launch | Priya; TPM reviews evidence | Agree durable deduplication/idempotency contract across queue delivery and state updates; verify successful push followed by failed database update, retry, and concurrent poll cases result in one dispatcher-visible task |
| 3 | D: DET-118 routing review | Contact all three leads today; schedule reviews this week | TPM schedules; terminal leads validate; Priya resolves findings | Validate routing ownership and rules with every terminal; record agreement, unresolved issues, and retest evidence |
| 4 | A: auto-reassign carrier | Decline inclusion in September 8 scope; defer to separate discovery | TPM explains decision to Dana; product/client owners confirm future scope | Establish carrier eligibility, authorization, audit, failure handling and estimates before any commitment |
| 5 | E: UI blue shade | Defer; this week's noise | TPM records preference | No evidenced functional/accessibility defect; revisit after launch readiness |

C, B, and D start concurrently. C is first because the external response must be initiated immediately; B is equally a launch correctness gate. Do not assume Priya can absorb every task concurrently: after IT replies, explicitly sequence her work using the actual estimates.

### Risk and decision checkpoints

- Today: initiate IT escalation and terminal scheduling; review Priya's routing proposal before merge; send Dana the corrected Amber status.
- Within one business day: if IT has not confirmed the schema, escalate through Dana for an accountable IT owner and response date.
- After schema confirmation: reserve the estimated two additional engineering days, then validation time; reconcile with the routing fix estimate and review feedback.
- End of this week: report verified outcomes, remaining work, owners and dates. Confirm whether the September 8 target remains supportable. If capacity or evidence does not support it, present an explicitly agreed scope reduction or revised date; never silently omit Terminal 3.
- Before go-live: require evidence for correct scoring across three terminals, safe retry/concurrency routing, agreed rules, operational fallback, and support ownership.
- After go-live: track null scores, duplicate tasks, routing failures, dispatcher acknowledgements, and unresolved exception age by terminal. Compare with baseline and dispatcher feedback; queue volume alone is not adoption.

### Technical judgment

A database flag alone cannot make the queue push and database write atomic. I would review three complementary controls with Priya, selecting the smallest change compatible with the existing queue and database:

- **Idempotent processing:** use a stable routing-operation identity (exception ID plus an agreed routing version where rerouting is legitimate), backed by a unique database constraint or receiver inbox. Deduplication and dispatcher-task creation must be atomic at the receiver so overlapping consumers cannot both create a task.
- **Transactional outbox:** commit the routing intent and outbox message in the same database transaction, then publish through a retrying relay. A crash after publishing but before marking the outbox message sent can still cause redelivery; outbox does not remove the need for receiver idempotency.
- **Durable delivery/subscription, if the broker supports it:** retain unacknowledged messages across consumer outages, acknowledge only after durable processing, and provide retry/dead-letter handling and monitoring. A durable queue may already provide this; a separate subscription is relevant to a topic-based design. Durability prevents loss during outages, not duplicate effects.

Acceptance evidence should exercise publish success followed by state-update failure, overlapping polls, receiver failure before acknowledgement, and recovery after restart. Each routing operation must create one dispatcher-visible task; intentional reroutes need their own explicit identity. An in-memory lock is insufficient across processes or restarts. The brief does not identify the actual broker, so these are review options, not a claim that we have implemented a new messaging architecture.

The CSV lacks urgency inputs and four timezone offsets. Cleaning it cannot establish the true Terminal 3 scoring contract or repair production. Client IT confirmation remains necessary.


## Task 2 — Working data-normalization app

Build file: `NormalizeExceptions.cs`. Input: `data/exceptions.csv`. Requires .NET 10 SDK; verified using 10.0.401.

Run from the repository root:

```powershell
dotnet run --file NormalizeExceptions.cs -- data/exceptions.csv artifacts
pwsh -File scripts/verify.ps1
```

The app reads the supplied simple CSV, maps T3 to Terminal 3, uppercases carrier codes, and standardizes timestamps to ISO formatting while preserving whether a timezone is known. It writes normalized CSV/JSON plus summary JSON with counts and record-level review notes, and a styled HTML report with a print layout for browser PDF export. After generation, the app requests opening the HTML report in the default browser; --no-open disables that action for unattended runs. TimeProvider supplies the report-generation time; it does not infer source event timezones. The missing carrier stays blank; four timestamps stay explicitly timezone-unspecified rather than being falsely labelled UTC. I checked each expected normalized row, every event count, the exact review IDs and fields, and agreement between CSV and JSON outputs. Additional cases verify rejection of malformed rows and duplicate IDs before output and preservation/reporting of unknown terminals and invalid timestamps. All these automated checks passed. The utility does not resolve the production EDI scoring contract; IT confirmation remains required.

| Event type | Count |
|---|---:|
| missed_pickup | 2 |
| doc_mismatch | 2 |
| carrier_substitution | 1 |
| Total | 5 |

| Record | Review needed |
|---|---|
| CPX-88213 | Source timezone missing |
| CPX-88214 | Source timezone missing |
| CPX-88216 | Carrier missing; source timezone missing |
| CPX-88217 | Source timezone missing |

CPX-88215 explicitly provides UTC and needs no uncertainty note. All five records contribute to the event counts; review flags do not silently remove events.

Assumptions: MM/dd/yyyy is the supplied slash-date format (08/14 cannot be day/month). T3 is an alias for Terminal 3. Carrier casing is normalized, not validated against a carrier registry. The utility supports the supplied five-column unquoted input, not a general CSV ingestion pipeline. Outputs include a timestamp_basis column so an unspecified time cannot be mistaken for UTC. A failed run may leave older outputs from a previous run; rely on the exit code and use a fresh output directory for verification.

## Task 3 — Client status update

Subject: Dispatch Exception Triage — launch risks and this week's plan

Hi Dana,

I need to correct the picture in last Friday's update. The project is Amber: September 8 remains our target, but there are unresolved launch risks and dependencies. We should not currently describe the work as Green or say there are no blockers.

Terminal 3's EDI schema mismatch is producing null urgency scores for some exceptions. This is more than a minor validation task. We need your IT contact to confirm the fields Terminal 3 actually sends; the ticket estimates two additional engineering days after that response, followed by verification. I am requesting confirmation within one business day. Could you help identify the accountable IT contact and secure that response?

Priya has also identified a routing risk before merge: a successful queue push followed by a failed database update, or overlapping polls, could create duplicate dispatcher tasks. We are addressing this before release and will verify the failure and concurrent-processing cases. We still need to confirm the effort and its effect on the remaining plan.

The routing-rules review with the three terminal leads has not been scheduled. I am contacting the leads today to arrange reviews this week and capture their agreement and any required changes.

On the COO's request, I do not recommend adding automatic carrier reassignment to the September 8 release. The current scope routes exceptions to a dispatcher for a decision. Executing carrier changes adds operational authority, eligibility rules, audit and recovery requirements that we have not estimated or validated. I recommend keeping dispatcher decisions for launch and scoping reassignment separately with you afterward.

We are prioritizing scoring, reliable routing and terminal validation; the proposed UI colour change will wait. I will send a checkpoint update at the end of this week with verified progress, remaining effort and the implications for September 8. If the IT response or technical work threatens the date sooner, I will escalate immediately with the scope and schedule options rather than wait for that update.

Allan


## Task 4 — AI workflow note

AI assisted with reading the materials, drafting the triage and client update, scaffolding the .NET utility, and writing automated checks. The priority decisions, tone of the client commitment, and personal introduction/salary statements require my judgment and review. One AI-generated first draft used reflection-based System.Text.Json serialization, which failed under the .NET file-based app defaults. We replaced it with source-generated serialization and reran the checks successfully. I also challenged the idea that a database “routed” flag alone prevents duplicate queue deliveries: queue delivery and database writes can fail independently, so actual receiver idempotency and retry/concurrency evidence matter.

---
Submission check: replace the video placeholder with an accessible URL; attach the actual .cs build file and source CSV (or the repository ZIP); check links in a private browser window. Review the AI note personally before submitting it in my voice.
