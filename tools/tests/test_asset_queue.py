from __future__ import annotations

import json
import tempfile
import unittest
from datetime import date
from pathlib import Path

from tools.asset_queue import (
    MAX_DAILY_CREDITS,
    QueueValidationError,
    build_plan,
    finalize_request,
    load_ledger,
    load_requests,
    reserve_requests,
)


class AssetQueueTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.queue = self.root / "requests"
        self.queue.mkdir()
        self.ledger = self.root / "ledger.json"
        self._write_ledger()

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def test_plan_never_exceeds_budget_and_defers_overflow(self) -> None:
        self._write_request("old-high", credits=120, priority=10, created_hour=1)
        self._write_request("new-high", credits=90, priority=10, created_hour=2)
        self._write_request("small-low", credits=80, priority=30, created_hour=0)

        requests = load_requests(self.queue)
        ledger = load_ledger(self.ledger)
        plan = build_plan(requests, ledger, date(2026, 9, 7), parallelism=2)

        self.assertEqual([request.request_id for request in plan.selected], ["old-high", "small-low"])
        self.assertEqual(plan.selected_credits, MAX_DAILY_CREDITS)
        self.assertEqual(plan.deferred_ids, ("new-high",))
        self.assertEqual(plan.as_mapping()["waves"], [["old-high", "small-low"]])

    def test_existing_commitment_reduces_available_budget(self) -> None:
        self._write_ledger(reserved=75, consumed=25)
        self._write_request("fits", credits=100, priority=10)
        self._write_request("overflow", credits=1, priority=20)

        plan = build_plan(
            load_requests(self.queue),
            load_ledger(self.ledger),
            date(2026, 9, 7),
        )

        self.assertEqual([request.request_id for request in plan.selected], ["fits"])
        self.assertEqual(plan.deferred_ids, ("overflow",))

    def test_unapproved_request_is_never_selected(self) -> None:
        self._write_request("awaiting-user", credits=20, priority=0, status="pending_approval")

        plan = build_plan(
            load_requests(self.queue),
            load_ledger(self.ledger),
            date(2026, 9, 7),
        )

        self.assertEqual(plan.selected, ())

    def test_dependency_blocks_until_ledger_marks_it_completed(self) -> None:
        self._write_request("base-model", credits=20, priority=10, status="cancelled")
        self._write_request(
            "texture-pass", credits=10, priority=20, dependencies=["base-model"]
        )

        plan = build_plan(
            load_requests(self.queue),
            load_ledger(self.ledger),
            date(2026, 9, 7),
        )

        self.assertEqual(plan.blocked_ids, ("texture-pass",))

    def test_reservation_is_atomic_and_cannot_be_repeated(self) -> None:
        self._write_request("approved-model", credits=40, priority=10)

        result = reserve_requests(
            ["approved-model"], date(2026, 9, 7), self.queue, self.ledger
        )

        self.assertEqual(result["reserved_credits"], 40)
        stored = load_ledger(self.ledger)
        self.assertEqual(stored["days"]["2026-09-07"]["reserved"], 40)
        with self.assertRaises(QueueValidationError):
            reserve_requests(
                ["approved-model"], date(2026, 9, 7), self.queue, self.ledger
            )

    def test_reservation_from_previous_day_is_not_dispatched_again(self) -> None:
        self._write_request("slow-model", credits=40, priority=10)
        self._write_ledger(
            day="2026-09-06",
            reserved=40,
            jobs={
                "slow-model": {
                    "estimated_credits": 40,
                    "state": "reserved",
                }
            },
        )

        plan = build_plan(
            load_requests(self.queue),
            load_ledger(self.ledger),
            date(2026, 9, 7),
        )

        self.assertEqual(plan.selected, ())

    def test_completion_reconciles_reserved_and_consumed_totals(self) -> None:
        self._write_request("finished-model", credits=40, priority=10)
        reserve_requests(
            ["finished-model"], date(2026, 9, 7), self.queue, self.ledger
        )

        result = finalize_request(
            "finished-model", date(2026, 9, 7), 35, True, self.ledger
        )

        self.assertEqual(result["state"], "completed")
        stored = load_ledger(self.ledger)
        day_value = stored["days"]["2026-09-07"]
        self.assertEqual(day_value["reserved"], 0)
        self.assertEqual(day_value["consumed"], 35)

    def test_ledger_rejects_aggregate_that_does_not_match_jobs(self) -> None:
        self._write_ledger(
            reserved=40,
            jobs={
                "wrong-total": {
                    "estimated_credits": 20,
                    "state": "reserved",
                }
            },
        )

        with self.assertRaises(QueueValidationError):
            load_ledger(self.ledger)

    def test_duplicate_dependency_cycle_is_rejected(self) -> None:
        self._write_request("asset-one", credits=10, priority=10, dependencies=["asset-two"])
        self._write_request("asset-two", credits=10, priority=10, dependencies=["asset-one"])

        with self.assertRaises(QueueValidationError):
            load_requests(self.queue)

    def _write_request(
        self,
        request_id: str,
        credits: int,
        priority: int,
        created_hour: int = 0,
        status: str = "approved",
        dependencies: list[str] | None = None,
    ) -> None:
        value = {
            "id": request_id,
            "kind": "meshy:text-to-3d",
            "prompt_summary": f"Generated test asset for {request_id}",
            "priority": priority,
            "estimated_credits": credits,
            "status": status,
            "created_at": f"2026-09-06T{created_hour:02d}:00:00Z",
            "not_before": "2026-09-07",
            "dependencies": dependencies or [],
            "slice": "S001",
        }
        (self.queue / f"{request_id}.json").write_text(
            json.dumps(value), encoding="utf-8"
        )

    def _write_ledger(
        self,
        reserved: int = 0,
        consumed: int = 0,
        day: str = "2026-09-07",
        jobs: dict[str, object] | None = None,
    ) -> None:
        days = {}
        if reserved or consumed:
            if jobs is None:
                jobs = {}
                if reserved:
                    jobs["existing-reservation"] = {
                        "estimated_credits": reserved,
                        "state": "reserved",
                    }
                if consumed:
                    jobs["existing-consumption"] = {
                        "estimated_credits": consumed,
                        "actual_credits": consumed,
                        "state": "completed",
                    }
            days[day] = {
                "reserved": reserved,
                "consumed": consumed,
                "jobs": jobs,
            }
        value = {
            "schema_version": 1,
            "timezone": "UTC",
            "daily_budget": MAX_DAILY_CREDITS,
            "days": days,
        }
        self.ledger.write_text(json.dumps(value), encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
