Subject: Dispatch Exception Triage — launch risks and this week's plan

Hi Dana,

I need to correct the picture in last Friday's update. The project is Amber: September 8 remains our target, but there are unresolved launch risks and dependencies. We should not currently describe the work as Green or say there are no blockers.

Terminal 3's EDI schema mismatch is producing null urgency scores for some exceptions. This is more than a minor validation task. We need your IT contact to confirm the fields Terminal 3 actually sends; the ticket estimates two additional engineering days after that response, followed by verification. I am requesting confirmation within one business day. Could you help identify the accountable IT contact and secure that response?

Priya has also identified a routing risk before merge: a successful queue push followed by a failed database update, or overlapping polls, could create duplicate dispatcher tasks. We are addressing this before release and will verify the failure and concurrent-processing cases. We still need to confirm the effort and its effect on the remaining plan.

The routing-rules review with the three terminal leads has not been scheduled. I am contacting the leads today to arrange reviews this week and capture their agreement and any required changes.

On the COO's request, I do not recommend adding automatic carrier reassignment to the September 8 release. The current scope routes exceptions to a dispatcher for a decision. Executing carrier changes adds operational authority, eligibility rules, audit and recovery requirements that we have not estimated or validated. I recommend keeping dispatcher decisions for launch and scoping reassignment separately with you afterward.

We are prioritizing scoring, reliable routing and terminal validation; the proposed UI colour change will wait. I will send a checkpoint update at the end of this week with verified progress, remaining effort and the implications for September 8. If the IT response or technical work threatens the date sooner, I will escalate immediately with the scope and schedule options rather than wait for that update.

Allan
