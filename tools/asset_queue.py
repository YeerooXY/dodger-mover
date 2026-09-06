#!/usr/bin/env python3
"""Validate and budget approved asset-generation work.

This module never calls an external generation API. It prepares an approved,
bounded batch for the installed asset-generation integration.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import tempfile
import time
from contextlib import contextmanager
from dataclasses import dataclass
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any, Iterator, Sequence


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_QUEUE_DIR = ROOT / "asset_queue" / "requests"
DEFAULT_LEDGER_PATH = ROOT / "asset_queue" / "ledger.json"
MAX_DAILY_CREDITS = 200
VALID_STATUSES = {"draft", "pending_approval", "approved", "cancelled"}
REQUEST_ID_PATTERN = re.compile(r"^[a-z0-9][a-z0-9-]{2,79}$")
SLICE_PATTERN = re.compile(r"^S[0-9]{3}$")


class QueueValidationError(ValueError):
    """Raised when queue data violates the asset contract."""


@dataclass(frozen=True)
class AssetRequest:
    request_id: str
    kind: str
    prompt_summary: str
    priority: int
    estimated_credits: int
    status: str
    created_at: datetime
    not_before: date | None
    dependencies: tuple[str, ...]
    slice_id: str

    @classmethod
    def from_mapping(cls, value: dict[str, Any], source: Path) -> "AssetRequest":
        required = {
            "id",
            "kind",
            "prompt_summary",
            "priority",
            "estimated_credits",
            "status",
            "created_at",
            "dependencies",
            "slice",
        }
        optional = {"$schema", "not_before"}
        missing = required - value.keys()
        unknown = value.keys() - required - optional

        if missing:
            raise QueueValidationError(f"{source}: missing fields {sorted(missing)}")
        if unknown:
            raise QueueValidationError(f"{source}: unknown fields {sorted(unknown)}")

        request_id = value["id"]
        if not isinstance(request_id, str) or not REQUEST_ID_PATTERN.fullmatch(request_id):
            raise QueueValidationError(f"{source}: invalid id {request_id!r}")

        kind = value["kind"]
        if not isinstance(kind, str) or len(kind.strip()) < 3:
            raise QueueValidationError(f"{source}: kind must be a descriptive string")

        prompt_summary = value["prompt_summary"]
        if not isinstance(prompt_summary, str) or len(prompt_summary.strip()) < 8:
            raise QueueValidationError(f"{source}: prompt_summary is too short")

        priority = _bounded_integer(value["priority"], 0, 100, source, "priority")
        estimated_credits = _bounded_integer(
            value["estimated_credits"], 1, MAX_DAILY_CREDITS, source, "estimated_credits"
        )

        status = value["status"]
        if status not in VALID_STATUSES:
            raise QueueValidationError(f"{source}: invalid status {status!r}")

        created_at = _parse_datetime(value["created_at"], source)
        not_before = _parse_optional_date(value.get("not_before"), source)

        raw_dependencies = value["dependencies"]
        if not isinstance(raw_dependencies, list) or not all(
            isinstance(item, str) and REQUEST_ID_PATTERN.fullmatch(item)
            for item in raw_dependencies
        ):
            raise QueueValidationError(f"{source}: dependencies must contain valid request ids")
        if len(raw_dependencies) != len(set(raw_dependencies)):
            raise QueueValidationError(f"{source}: dependencies must be unique")
        if request_id in raw_dependencies:
            raise QueueValidationError(f"{source}: request cannot depend on itself")

        slice_id = value["slice"]
        if not isinstance(slice_id, str) or not SLICE_PATTERN.fullmatch(slice_id):
            raise QueueValidationError(f"{source}: invalid slice {slice_id!r}")

        return cls(
            request_id=request_id,
            kind=kind.strip(),
            prompt_summary=prompt_summary.strip(),
            priority=priority,
            estimated_credits=estimated_credits,
            status=status,
            created_at=created_at,
            not_before=not_before,
            dependencies=tuple(raw_dependencies),
            slice_id=slice_id,
        )


@dataclass(frozen=True)
class DailyPlan:
    target_date: date
    budget: int
    already_committed: int
    selected: tuple[AssetRequest, ...]
    deferred_ids: tuple[str, ...]
    blocked_ids: tuple[str, ...]
    parallelism: int

    @property
    def selected_credits(self) -> int:
        return sum(request.estimated_credits for request in self.selected)

    def as_mapping(self) -> dict[str, Any]:
        waves = [
            [request.request_id for request in self.selected[index : index + self.parallelism]]
            for index in range(0, len(self.selected), self.parallelism)
        ]
        return {
            "date": self.target_date.isoformat(),
            "budget": self.budget,
            "already_committed": self.already_committed,
            "selected_credits": self.selected_credits,
            "remaining_credits": self.budget
            - self.already_committed
            - self.selected_credits,
            "waves": waves,
            "selected": [request.request_id for request in self.selected],
            "deferred": list(self.deferred_ids),
            "blocked_by_dependency": list(self.blocked_ids),
        }


def _bounded_integer(
    value: Any, minimum: int, maximum: int, source: Path, field: str
) -> int:
    if isinstance(value, bool) or not isinstance(value, int):
        raise QueueValidationError(f"{source}: {field} must be an integer")
    if value < minimum or value > maximum:
        raise QueueValidationError(
            f"{source}: {field} must be between {minimum} and {maximum}"
        )
    return value


def _parse_datetime(value: Any, source: Path) -> datetime:
    if not isinstance(value, str):
        raise QueueValidationError(f"{source}: created_at must be an ISO-8601 string")
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as exc:
        raise QueueValidationError(f"{source}: invalid created_at {value!r}") from exc
    if parsed.tzinfo is None:
        raise QueueValidationError(f"{source}: created_at must include a timezone")
    return parsed.astimezone(timezone.utc)


def _parse_optional_date(value: Any, source: Path) -> date | None:
    if value is None:
        return None
    if not isinstance(value, str):
        raise QueueValidationError(f"{source}: not_before must be an ISO date")
    try:
        return date.fromisoformat(value)
    except ValueError as exc:
        raise QueueValidationError(f"{source}: invalid not_before {value!r}") from exc


def load_requests(queue_dir: Path = DEFAULT_QUEUE_DIR) -> tuple[AssetRequest, ...]:
    requests: list[AssetRequest] = []
    seen: set[str] = set()

    for path in sorted(queue_dir.glob("*.json")):
        try:
            raw = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            raise QueueValidationError(f"{path}: cannot read valid JSON") from exc
        if not isinstance(raw, dict):
            raise QueueValidationError(f"{path}: request root must be an object")

        request = AssetRequest.from_mapping(raw, path)
        if request.request_id in seen:
            raise QueueValidationError(f"duplicate request id {request.request_id!r}")
        seen.add(request.request_id)
        requests.append(request)

    known_ids = {request.request_id for request in requests}
    for request in requests:
        unknown_dependencies = set(request.dependencies) - known_ids
        if unknown_dependencies:
            raise QueueValidationError(
                f"{request.request_id}: unknown dependencies {sorted(unknown_dependencies)}"
            )

    _detect_dependency_cycles(requests)
    return tuple(requests)


def _detect_dependency_cycles(requests: Sequence[AssetRequest]) -> None:
    dependencies = {request.request_id: request.dependencies for request in requests}
    visiting: set[str] = set()
    visited: set[str] = set()

    def visit(request_id: str) -> None:
        if request_id in visiting:
            raise QueueValidationError(f"dependency cycle contains {request_id!r}")
        if request_id in visited:
            return
        visiting.add(request_id)
        for dependency in dependencies[request_id]:
            visit(dependency)
        visiting.remove(request_id)
        visited.add(request_id)

    for identifier in dependencies:
        visit(identifier)


def load_ledger(path: Path = DEFAULT_LEDGER_PATH) -> dict[str, Any]:
    try:
        ledger = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise QueueValidationError(f"{path}: cannot read valid ledger JSON") from exc

    if not isinstance(ledger, dict):
        raise QueueValidationError(f"{path}: ledger root must be an object")
    if ledger.get("schema_version") != 1:
        raise QueueValidationError(f"{path}: unsupported schema_version")
    if ledger.get("timezone") != "UTC":
        raise QueueValidationError(f"{path}: timezone must be UTC")

    budget = ledger.get("daily_budget")
    if budget != MAX_DAILY_CREDITS:
        raise QueueValidationError(
            f"{path}: daily_budget must remain {MAX_DAILY_CREDITS}"
        )
    if not isinstance(ledger.get("days"), dict):
        raise QueueValidationError(f"{path}: days must be an object")

    for day_key, day_value in ledger["days"].items():
        _validate_ledger_day(day_key, day_value, path, budget)
    return ledger


def _validate_ledger_day(
    day_key: str, day_value: Any, source: Path, budget: int
) -> None:
    try:
        date.fromisoformat(day_key)
    except (TypeError, ValueError) as exc:
        raise QueueValidationError(f"{source}: invalid ledger day {day_key!r}") from exc
    if not isinstance(day_value, dict):
        raise QueueValidationError(f"{source}: day {day_key} must be an object")

    reserved = day_value.get("reserved", 0)
    consumed = day_value.get("consumed", 0)
    jobs = day_value.get("jobs", {})
    for field, value in (("reserved", reserved), ("consumed", consumed)):
        if isinstance(value, bool) or not isinstance(value, int) or value < 0:
            raise QueueValidationError(f"{source}: {day_key}.{field} must be non-negative")
    if not isinstance(jobs, dict):
        raise QueueValidationError(f"{source}: {day_key}.jobs must be an object")

    calculated_reserved = 0
    calculated_consumed = 0
    for request_id, job in jobs.items():
        if not isinstance(request_id, str) or not REQUEST_ID_PATTERN.fullmatch(request_id):
            raise QueueValidationError(f"{source}: {day_key} has invalid job id {request_id!r}")
        if not isinstance(job, dict):
            raise QueueValidationError(f"{source}: {day_key}.{request_id} must be an object")

        state = job.get("state")
        if state not in {"reserved", "completed", "failed"}:
            raise QueueValidationError(
                f"{source}: {day_key}.{request_id} has invalid state {state!r}"
            )
        estimated = _bounded_integer(
            job.get("estimated_credits"),
            1,
            MAX_DAILY_CREDITS,
            source,
            f"{day_key}.{request_id}.estimated_credits",
        )

        if state == "reserved":
            if set(job) != {"state", "estimated_credits"}:
                raise QueueValidationError(
                    f"{source}: reserved job {request_id!r} has unexpected fields"
                )
            calculated_reserved += estimated
            continue

        if set(job) != {"state", "estimated_credits", "actual_credits"}:
            raise QueueValidationError(
                f"{source}: finalized job {request_id!r} has unexpected fields"
            )
        actual = job.get("actual_credits")
        if isinstance(actual, bool) or not isinstance(actual, int) or actual < 0:
            raise QueueValidationError(
                f"{source}: {day_key}.{request_id}.actual_credits must be non-negative"
            )
        if actual > estimated:
            raise QueueValidationError(
                f"{source}: {day_key}.{request_id} spent above its reservation"
            )
        calculated_consumed += actual

    if reserved != calculated_reserved or consumed != calculated_consumed:
        raise QueueValidationError(
            f"{source}: {day_key} aggregates do not match its job records"
        )
    if reserved + consumed > budget:
        raise QueueValidationError(
            f"{source}: {day_key} commits {reserved + consumed}, above {budget} credits"
        )


def build_plan(
    requests: Sequence[AssetRequest],
    ledger: dict[str, Any],
    target_date: date,
    parallelism: int = 4,
) -> DailyPlan:
    if parallelism < 1 or parallelism > 16:
        raise QueueValidationError("parallelism must be between 1 and 16")

    budget = ledger["daily_budget"]
    day_value = ledger["days"].get(target_date.isoformat(), {})
    committed = day_value.get("reserved", 0) + day_value.get("consumed", 0)
    active_or_completed_ids = {
        request_id
        for day in ledger["days"].values()
        for request_id, value in day.get("jobs", {}).items()
        if isinstance(value, dict) and value.get("state") in {"reserved", "completed"}
    }
    attempted_today_ids = set(day_value.get("jobs", {}))
    unavailable_ids = active_or_completed_ids | attempted_today_ids
    completed_ids = {
        request_id
        for day in ledger["days"].values()
        for request_id, value in day.get("jobs", {}).items()
        if isinstance(value, dict) and value.get("state") == "completed"
    }

    candidates = sorted(
        (
            request
            for request in requests
            if request.status == "approved"
            and request.request_id not in unavailable_ids
            and (request.not_before is None or request.not_before <= target_date)
        ),
        key=lambda request: (request.priority, request.created_at, request.request_id),
    )

    selected: list[AssetRequest] = []
    deferred: list[str] = []
    blocked: list[str] = []
    remaining = budget - committed

    for request in candidates:
        if any(dependency not in completed_ids for dependency in request.dependencies):
            blocked.append(request.request_id)
            continue
        if request.estimated_credits <= remaining:
            selected.append(request)
            remaining -= request.estimated_credits
        else:
            deferred.append(request.request_id)

    return DailyPlan(
        target_date=target_date,
        budget=budget,
        already_committed=committed,
        selected=tuple(selected),
        deferred_ids=tuple(deferred),
        blocked_ids=tuple(blocked),
        parallelism=parallelism,
    )


@contextmanager
def _exclusive_lock(path: Path, timeout_seconds: float = 10.0) -> Iterator[None]:
    lock_path = path.with_suffix(path.suffix + ".lock")
    deadline = time.monotonic() + timeout_seconds
    descriptor: int | None = None
    while descriptor is None:
        try:
            descriptor = os.open(lock_path, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
        except FileExistsError:
            if time.monotonic() >= deadline:
                raise TimeoutError(f"could not acquire ledger lock {lock_path}")
            time.sleep(0.05)
    try:
        os.write(descriptor, str(os.getpid()).encode("ascii"))
        yield
    finally:
        os.close(descriptor)
        lock_path.unlink(missing_ok=True)


def _atomic_write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(
        prefix=f".{path.name}.", suffix=".tmp", dir=path.parent
    )
    temporary_path = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as stream:
            json.dump(value, stream, indent=4, sort_keys=True)
            stream.write("\n")
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary_path, path)
    finally:
        temporary_path.unlink(missing_ok=True)


def reserve_requests(
    request_ids: Sequence[str],
    target_date: date,
    queue_dir: Path = DEFAULT_QUEUE_DIR,
    ledger_path: Path = DEFAULT_LEDGER_PATH,
) -> dict[str, Any]:
    if not request_ids:
        raise QueueValidationError("at least one request id is required")
    if len(request_ids) != len(set(request_ids)):
        raise QueueValidationError("request ids must be unique")

    with _exclusive_lock(ledger_path):
        requests = {request.request_id: request for request in load_requests(queue_dir)}
        ledger = load_ledger(ledger_path)
        missing = set(request_ids) - requests.keys()
        if missing:
            raise QueueValidationError(f"unknown request ids {sorted(missing)}")

        plan = build_plan(tuple(requests.values()), ledger, target_date, parallelism=16)
        selectable = {request.request_id: request for request in plan.selected}
        unselectable = set(request_ids) - selectable.keys()
        if unselectable:
            raise QueueValidationError(
                "requests are unapproved, blocked, already reserved, not yet eligible, "
                f"or outside today's budget: {sorted(unselectable)}"
            )

        selected = [selectable[request_id] for request_id in request_ids]
        day_key = target_date.isoformat()
        day_value = ledger["days"].setdefault(
            day_key, {"reserved": 0, "consumed": 0, "jobs": {}}
        )
        for request in selected:
            day_value["jobs"][request.request_id] = {
                "estimated_credits": request.estimated_credits,
                "state": "reserved",
            }
            day_value["reserved"] += request.estimated_credits

        _validate_ledger_day(day_key, day_value, ledger_path, ledger["daily_budget"])
        _atomic_write_json(ledger_path, ledger)
        return {
            "date": day_key,
            "reserved": [request.request_id for request in selected],
            "reserved_credits": sum(request.estimated_credits for request in selected),
            "day_committed": day_value["reserved"] + day_value["consumed"],
        }


def finalize_request(
    request_id: str,
    target_date: date,
    actual_credits: int,
    succeeded: bool,
    ledger_path: Path = DEFAULT_LEDGER_PATH,
) -> dict[str, Any]:
    if isinstance(actual_credits, bool) or not isinstance(actual_credits, int):
        raise QueueValidationError("actual_credits must be an integer")
    if actual_credits < 0:
        raise QueueValidationError("actual_credits must be non-negative")

    with _exclusive_lock(ledger_path):
        ledger = load_ledger(ledger_path)
        day_key = target_date.isoformat()
        day_value = ledger["days"].get(day_key)
        if not isinstance(day_value, dict):
            raise QueueValidationError(f"no reservations exist for {day_key}")
        job = day_value.get("jobs", {}).get(request_id)
        if not isinstance(job, dict) or job.get("state") != "reserved":
            raise QueueValidationError(
                f"{request_id!r} is not reserved on {day_key}"
            )

        estimated = job["estimated_credits"]
        if actual_credits > estimated:
            raise QueueValidationError(
                f"actual cost {actual_credits} exceeds reservation {estimated}"
            )

        day_value["reserved"] -= estimated
        day_value["consumed"] += actual_credits
        job["state"] = "completed" if succeeded else "failed"
        job["actual_credits"] = actual_credits

        _validate_ledger_day(day_key, day_value, ledger_path, ledger["daily_budget"])
        _atomic_write_json(ledger_path, ledger)
        return {
            "date": day_key,
            "request": request_id,
            "state": job["state"],
            "actual_credits": actual_credits,
            "day_committed": day_value["reserved"] + day_value["consumed"],
        }


def validate_repository_queue(
    queue_dir: Path = DEFAULT_QUEUE_DIR,
    ledger_path: Path = DEFAULT_LEDGER_PATH,
) -> tuple[tuple[AssetRequest, ...], dict[str, Any]]:
    requests = load_requests(queue_dir)
    ledger = load_ledger(ledger_path)

    known_ids = {request.request_id for request in requests}
    ledger_ids = {
        request_id
        for day_value in ledger["days"].values()
        for request_id in day_value.get("jobs", {})
    }
    unknown_ledger_ids = ledger_ids - known_ids
    if unknown_ledger_ids:
        raise QueueValidationError(
            f"ledger references unknown requests {sorted(unknown_ledger_ids)}"
        )
    return requests, ledger


def _date_argument(value: str) -> date:
    try:
        return date.fromisoformat(value)
    except ValueError as exc:
        raise argparse.ArgumentTypeError("expected YYYY-MM-DD") from exc


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser("validate", help="validate queue requests and ledger")

    plan_parser = subparsers.add_parser("plan", help="preview approved work")
    plan_parser.add_argument("--date", type=_date_argument, required=True)
    plan_parser.add_argument("--parallelism", type=int, default=4)

    reserve_parser = subparsers.add_parser("reserve", help="reserve an approved batch")
    reserve_parser.add_argument("--date", type=_date_argument, required=True)
    reserve_parser.add_argument("--request", action="append", required=True)

    for command in ("complete", "fail"):
        finalize_parser = subparsers.add_parser(
            command, help=f"mark a reserved request as {command}d"
        )
        finalize_parser.add_argument("--date", type=_date_argument, required=True)
        finalize_parser.add_argument("--request", required=True)
        finalize_parser.add_argument("--actual-credits", type=int, required=True)
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    parser = _build_parser()
    args = parser.parse_args(argv)
    try:
        if args.command == "validate":
            requests, _ = validate_repository_queue()
            print(f"Asset queue valid: {len(requests)} request(s), {MAX_DAILY_CREDITS}/day")
            return 0
        if args.command == "plan":
            requests, ledger = validate_repository_queue()
            plan = build_plan(requests, ledger, args.date, args.parallelism)
            print(json.dumps(plan.as_mapping(), indent=4))
            return 0
        if args.command == "reserve":
            reservation = reserve_requests(args.request, args.date)
            print(json.dumps(reservation, indent=4))
            return 0
        if args.command in {"complete", "fail"}:
            result = finalize_request(
                args.request,
                args.date,
                args.actual_credits,
                succeeded=args.command == "complete",
            )
            print(json.dumps(result, indent=4))
            return 0
    except (QueueValidationError, TimeoutError) as exc:
        print(f"asset queue error: {exc}", file=sys.stderr)
        return 1
    parser.error(f"unknown command {args.command}")
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
