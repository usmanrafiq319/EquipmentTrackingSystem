# Equipment Tracking System — Original Assignment Requirements

## 1. Assignment Information

- Hiring company: Solvefy
- Position: AI Engineer I (Associate)
- Project: Equipment Tracking System
- Development method: BMAD Method (mandatory)
- Original document deadline: 7 calendar days from receipt
- Submission deadline: 24 hours before scheduled interview
- Separately communicated working deadline: 4 days (to be confirmed with HR)

The employer expects candidates to use AI, direct its work, understand the generated code, and exercise independent engineering judgment. Requirements are deliberately incomplete and contain at least one contradiction.

## 2. Client Background

Client: Horizon Survey Services

Horizon Survey Services is a land-surveying company in Lahore with approximately 25 employees, including around 14 field staff.

Equipment includes:
- Total stations
- GPS rovers
- Tripods
- Drones
- Laptops
- Field tablets

## 3. Original Client Voice Note — Mr Tariq

> So basically the problem is the equipment. We have a lot of equipment, it is expensive, and I never know where any of it is. The boys come in the morning, they pick up what they need, they go to site. Fine. Then somebody else needs the same thing and nobody knows who has it. Last month we bought a second GPS rover for eleven lakh because we thought ours was lost. It was in somebody's car boot for three weeks.
>
> So I want to know who has what. That's it really. And I want it back on time, people keep things for weeks when they needed it for one day.
>
> Anyone in the team can take anything, we don't have a system, it's all on trust, and mostly that's fine, I don't want to make it like the army. But I want to see it on a screen.
>
> Sometimes they take the whole survey kit, sometimes just one piece. Oh and the office needs to know too, Saima in the office gets the calls asking where things are, she is the one suffering.
>
> One more thing — the boys are not going to use anything complicated. Half of them just use WhatsApp, that's it. If it takes more than a few seconds they won't do it, and then we're back where we started.

## 4. Original Follow-Up Email — Mr Tariq

Subject: RE: equipment thing

> Hi,
>
> Forgot to mention a few things.
>
> The drone is different, that needs Hamza's approval before anyone takes it, he is the only licensed operator.
>
> Same for the new rover maybe, I will decide later.
>
> We have 4 tripods and 6 field tablets, those are not individually tracked, we just need to know how many are left.
>
> Also sometimes equipment comes back damaged and nobody says anything until the next person takes it to site and finds out. That has cost us two days of client work this year.
>
> It would be good if the system reminds people. We mostly use WhatsApp for everything here.
>
> And I want to be able to look back at last month and see what was out and who had it.
>
> Thanks Tariq

## 5. Technical Requirements

The assignment requires a working application using:

- Backend: .NET 8 or .NET 9, C#, ASP.NET Core Web API
- Database: Any relational database; SQLite or SQL Server LocalDB is acceptable
- Frontend: Candidate's choice, including plain HTML and JavaScript
- UI must be usable; visual design is not the main assessment criterion
- Application must be runnable using one documented command or a short, accurate set of steps
- Reviewers expect to run the application within five minutes

Not every feature described by the client must be implemented. The developer must prioritize the scope and explain any exclusions.

## 6. Submission Requirements

Submit one private GitHub repository with reviewer access, or a ZIP file, containing:

- /src — application code
- /docs — BMAD artifacts: brief, PRD, epics, stories, architecture
- /conversations — complete and unedited AI conversations
- /DECISIONS.md — assumptions and decision log
- /README.md — running instructions, completed features, incomplete features, deliberate exclusions and reasoning, approximate time spent, and AI tools used

## 7. DECISIONS.md Requirements

Expected: approximately 8–20 meaningful decisions.

Each entry must contain:
1. The gap — unclear, missing, or contradictory requirement, quoting the original
2. What I decided
3. The alternative considered
4. Why I chose my decision
5. What would change my mind

## 8. AI Usage and Interview Rules

- Use the BMAD Method to establish requirements and development context before coding.
- Complete, unedited AI conversation logs must be submitted, including unsuccessful attempts.
- Any AI tools may be used; identify them and their purposes in README.md.
- Candidates must direct the work and understand the resulting code.
- Interviewers may ask candidates to explain or modify any part of their code live.
- Candidates should make reasonable decisions when information is incomplete.
- Do not contact the employer for requirement clarification.
- HR may be contacted only for logistics such as deadline, format, or access issues.
- Report approximate hours spent in README.md.

## 9. BMAD Resources Provided by Employer

- Masterclass: https://www.youtube.com/watch?v=LorEJPrALcg
- Channel: https://www.youtube.com/@BMadCode
- Documentation: https://docs.bmad-method.org/
- Repository: https://github.com/bmad-code-org/BMAD-METHOD

The employer warns that the masterclass may show older BMAD behavior. Use current applicable documentation.

## 10. Developer's Proposed Technology Choices

The following are personal implementation preferences, not additional client requirements:

- Backend: ASP.NET Core 8 Web API
- Frontend: Angular with TypeScript
- Database: SQLite with Entity Framework Core
- IDE: Visual Studio Code
- AI assistant: GitHub Copilot Student
- BMAD installation: v6.12.4

These choices remain subject to justification and architectural review.

## 11. Analysis Status

Requirements have not yet been formally analyzed. No contradictions have been resolved, no assumptions have been finalized, and no feature prioritization has been approved.

Use the BMAD Analyst workflow to perform that analysis before generating implementation code.
