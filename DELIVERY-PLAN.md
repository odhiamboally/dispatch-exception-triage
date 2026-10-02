# Weekly Delivery Plan

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

## Risk and decision checkpoints

- Today: initiate IT escalation and terminal scheduling; review Priya's routing proposal before merge; send Dana the corrected Amber status.
- Within one business day: if IT has not confirmed the schema, escalate through Dana for an accountable IT owner and response date.
- After schema confirmation: reserve the estimated two additional engineering days, then validation time; reconcile with the routing fix estimate and review feedback.
- End of this week: report verified outcomes, remaining work, owners and dates. Confirm whether the September 8 target remains supportable. If capacity or evidence does not support it, present an explicitly agreed scope reduction or revised date; never silently omit Terminal 3.
- Before go-live: require evidence for correct scoring across three terminals, safe retry/concurrency routing, agreed rules, operational fallback, and support ownership.
- After go-live: track null scores, duplicate tasks, routing failures, dispatcher acknowledgements, and unresolved exception age by terminal. Compare with baseline and dispatcher feedback; queue volume alone is not adoption.

## Technical judgment

A database flag alone cannot make the queue push and database write atomic. I would review three complementary controls with Priya, selecting the smallest change compatible with the existing queue and database:

- **Idempotent processing:** use a stable routing-operation identity (exception ID plus an agreed routing version where rerouting is legitimate), backed by a unique database constraint or receiver inbox. Deduplication and dispatcher-task creation must be atomic at the receiver so overlapping consumers cannot both create a task.
- **Transactional outbox:** commit the routing intent and outbox message in the same database transaction, then publish through a retrying relay. A crash after publishing but before marking the outbox message sent can still cause redelivery; outbox does not remove the need for receiver idempotency.
- **Durable delivery/subscription, if the broker supports it:** retain unacknowledged messages across consumer outages, acknowledge only after durable processing, and provide retry/dead-letter handling and monitoring. A durable queue may already provide this; a separate subscription is relevant to a topic-based design. Durability prevents loss during outages, not duplicate effects.

Acceptance evidence should exercise publish success followed by state-update failure, overlapping polls, receiver failure before acknowledgement, and recovery after restart. Each routing operation must create one dispatcher-visible task; intentional reroutes need their own explicit identity. An in-memory lock is insufficient across processes or restarts. The brief does not identify the actual broker, so these are review options, not a claim that we have implemented a new messaging architecture.

The CSV lacks urgency inputs and four timezone offsets. Cleaning it cannot establish the true Terminal 3 scoring contract or repair production. Client IT confirmation remains necessary.
