# Asset queue

Place one JSON request per asset in `requests/`. Requests start as `pending_approval`; an agent may change them to `approved` only after presenting the exact generation pipeline and cost for user confirmation.

Validate the queue:

```bash
python tools/asset_queue.py validate
```

Preview the next UTC day's approved work without spending or mutating anything:

```bash
python tools/asset_queue.py plan --date 2026-09-07 --parallelism 4
```

Atomically reserve a reviewed batch before dispatch:

```bash
python tools/asset_queue.py reserve --date 2026-09-07 --request asset-id-1 --request asset-id-2
```

After the integration reports its actual charge, reconcile the reservation:

```bash
python tools/asset_queue.py complete --date 2026-09-07 --request asset-id-1 --actual-credits 20
```

Use `fail` instead of `complete` when generation fails. A failed request can be considered again on a later UTC day; it cannot consume the same day's queue twice.

The queue does not call Meshy. It enforces budgeting and prepares bounded work for the installed Meshy integration.
