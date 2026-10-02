# Four-minute video script

Target: approximately four minutes, including pauses and a live run. The brief recommends 5–10 minutes; this shorter target follows Allan's preference. Rehearse with a timer; do not omit required content to meet the target.

## 0:00–1:10 — Decisions and weekly plan

Hi, I'm Allan Odhiambo. Here is my delivery plan for Corrigan Peak's Dispatch Exception Triage.

My reasoning sequence was: read everything, identify launch risks and dependencies, assign actions and owners, correct the client update, then execute and verify. I use quick tasks to unblock work after triage; their short duration does not determine their importance.

First is Terminal 3's schema mismatch. Some urgency scores are null, and client IT confirmation is an external dependency. I would initiate that contact immediately and plan the estimated two engineering days after the response, plus verification.

Second is Priya's duplicate-routing risk. A successful queue push followed by a failed database update can duplicate work on retry. Overlapping polls can do the same. I would require verification of both cases before release.

Third, I would schedule the three terminal reviews today. Calling the ticket unblocked does not mean validation is arranged.

I would decline automatic carrier reassignment for this release: it adds operational decisions and safeguards beyond human routing. The blue-shade preference is noise for this week's launch priorities. The first three actions begin concurrently; Priya's implementation work must be sequenced against actual capacity.

## 1:10–1:50 — Client communication

[Show CLIENT-UPDATE.md.]

My update changes the status to Amber. September 8 remains the target, but it is not yet an assured date. I explain the null scores, duplicate risk and unscheduled reviews, request a specific IT response, and make the scope decision clear.

Friday's update predates Priya's flag, so I do not imply the account team concealed that new information. I correct today's picture, assign checkpoints, and commit to escalating date risks as soon as they emerge.

## 1:50–3:10 — Demonstrate the build

[Show input CSV. Run: dotnet run --file NormalizeExceptions.cs -- data/exceptions.csv artifacts]

This .NET 10 file-based app normalizes the supplied CSV. T3 becomes Terminal 3, carrier codes become uppercase, and timestamps use a consistent ISO format.

[Show normalized.csv.]

I retain the missing carrier rather than guessing. Four timestamps have no timezone, so they stay explicitly unspecified; adding Z would falsely claim UTC. The timestamp that already has Z retains UTC.

[Show summary.json.]

There are five exceptions: two missed pickups, two document mismatches and one carrier substitution. Four records require review, with five field notes: four missing timezones and one missing carrier.

[Run: pwsh -File scripts/verify.ps1. Show verification result.]

The checks compare every normalized value and issue against expectations, verify the count totals, and exercise malformed input, duplicate IDs, unknown terminals and invalid timestamps. This is evidence of correct outputs, not just successful execution.

This utility helps inspect the export. It does not repair production scoring or substitute for IT's schema confirmation.

## 3:10–3:30 — AI workflow

I used AI to help draft the plan, update and code. I reviewed the priority calls and checked the outputs. The first code draft used reflection-based JSON serialization and failed. We switched to source generation and reran the output checks. I also questioned relying on a database flag alone to prevent duplicate routing.

[Use this statement only after Allan personally reviews the work.]

## 3:30–4:00 — Personal close

I'm a senior .NET engineer based in Nairobi. [Add one accurate sentence about your own delivery experience.]

This role appeals to me because it combines technical judgment, practical automation and clear client communication. Ajaia's focus on measurable production outcomes matches how I want to work.

My salary expectation for this role is [Allan to supply amount, currency and period]. [Confirm Eastern-time availability only if accurate.]

Thank you.
