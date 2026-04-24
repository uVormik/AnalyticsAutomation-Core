START HERE - Coordination Readiness

Before any new work step, any new chat, any resumed work after a pause, or any continuation after someone else merged into main:

1. If the working tree is dirty, commit or stash first.
2. Run git fetch origin --prune.
3. Run git switch main.
4. Run git pull --ff-only origin main.
5. Read the latest TEAM COORDINATION LOG entries.
6. Open every referenced handoff doc and task card.
7. Only then return to the feature/docs branch and continue.

Remember:
- GitHub main + TEAM COORDINATION LOG are the source of truth.
- Telegram is notification only.
- Do not continue from stale local main.
- Do not continue from Telegram retellings, screenshots, or oral summaries alone.