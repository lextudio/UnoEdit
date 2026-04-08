#!/usr/bin/env python3
import re
import sys
import statistics
from datetime import datetime


TS_LINE_RE = re.compile(r'^(?P<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})')
MANAGED_RE = re.compile(
    r"UpdateCaretRect adjusted -> x=(?P<rx>[-0-9.]+), y=(?P<ry>[-0-9.]+), w=(?P<w>[-0-9.]+), h=(?P<h>[-0-9.]+).*id=(?P<id>\d+)"
)
# fallback: older logs include CalculatePlatformInputCaretRect post-transform entries (no id)
CALC_MANAGED_RE = re.compile(
    r"CalculatePlatformInputCaretRect post-transform point\.X=(?P<px>[-0-9.]+), point\.Y=(?P<py>[-0-9.]+) returnedRect X=(?P<rx>[-0-9.]+) Y=(?P<ry>[-0-9.]+) W=(?P<w>[-0-9.]+) H=(?P<h>[-0-9.]+)"
)

NATIVE_RAW_RE = re.compile(
    r"unoedit_ime_update_caret_rect id=(?P<id>\d+) raw x=(?P<x>[-0-9.]+) y=(?P<y>[-0-9.]+) w=(?P<w>[-0-9.]+) h=(?P<h>[-0-9.]+)"
)
# fallback native pattern for older logs without id
NATIVE_RAW_NOID_RE = re.compile(
    r"unoedit_ime_update_caret_rect raw x=(?P<x>[-0-9.]+) y=(?P<y>[-0-9.]+) w=(?P<w>[-0-9.]+) h=(?P<h>[-0-9.]+)"
)
SCREEN_RE = re.compile(
    r"unoedit_ime_update_caret_rect .* screenRect_points=\{\{(?P<sx>[-0-9.]+), (?P<sy>[-0-9.]+)\}, \{(?P<sw>[-0-9.]+), (?P<sh>[-0-9.]+)\}\}.*flipped=(?P<flipped>\d+)"
)


def parse_file_with_timestamps(path):
    managed = []
    native = []
    screen = []
    current_ts = None

    def parse_ts(ts_str):
        try:
            return datetime.strptime(ts_str, "%Y-%m-%d %H:%M:%S.%f")
        except Exception:
            return None

    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        for lineno, line in enumerate(f, start=1):
            line = line.rstrip('\n')
            mts = TS_LINE_RE.match(line)
            if mts:
                t = parse_ts(mts.group('ts'))
                if t:
                    current_ts = t

            # attempt to find managed and native entries; assign current_ts (may be None)
            mm = MANAGED_RE.search(line)
            if mm:
                managed.append({
                    'idx': len(managed) + 1,
                    'rx': float(mm.group('rx')),
                    'ry': float(mm.group('ry')),
                    'id': int(mm.group('id')),
                    'ts': current_ts,
                    'line': lineno,
                    'raw': line,
                })
            else:
                mcalc = CALC_MANAGED_RE.search(line)
                if mcalc:
                    managed.append({
                        'idx': len(managed) + 1,
                        'rx': float(mcalc.group('rx')),
                        'ry': float(mcalc.group('ry')),
                        'ts': current_ts,
                        'line': lineno,
                        'raw': line,
                    })

            nm = NATIVE_RAW_RE.search(line)
            if nm:
                native.append({
                    'idx': len(native) + 1,
                    'x': float(nm.group('x')),
                    'y': float(nm.group('y')),
                    'id': int(nm.group('id')),
                    'ts': current_ts,
                    'line': lineno,
                    'raw': line,
                })
            else:
                nm2 = NATIVE_RAW_NOID_RE.search(line)
                if nm2:
                    native.append({
                        'idx': len(native) + 1,
                        'x': float(nm2.group('x')),
                        'y': float(nm2.group('y')),
                        'ts': current_ts,
                        'line': lineno,
                        'raw': line,
                    })

            sm = SCREEN_RE.search(line)
            if sm:
                screen.append(sm.groupdict())

    return managed, native, screen


def pair_entries(managed, native):
    # First attempt: deterministic id-based pairing when available.
    native_by_id = {n.get('id'): n for n in native if 'id' in n}
    native_used_ids = set()
    pairs = {}

    # sort managed entries by timestamp (None -> last), keep original index
    def ts_key(e):
        return (e.get('ts') is None, e.get('ts') or datetime.max, e['idx'])

    managed_sorted = sorted(managed, key=ts_key)

    # First pass: pair entries that both have an id
    for m in managed_sorted:
        mid = m.get('id')
        if mid is not None and mid in native_by_id:
            pairs[m['idx']] = native_by_id[mid]
            native_used_ids.add(mid)

    # Prepare remaining native availability for coordinate-based pairing
    native_avail = [
        {'idx': n['idx'], 'x': n['x'], 'y': n['y'], 'ts': n.get('ts'), 'line': n['line'], 'id': n.get('id')}
        for n in native if n.get('id') not in native_used_ids
    ]

    # Second pass: for managed entries not yet paired, fallback to nearest-coordinate pairing
    for m in managed_sorted:
        if m['idx'] in pairs:
            continue

        rx, ry = m['rx'], m['ry']
        if not native_avail:
            pairs[m['idx']] = None
            continue

        best = None
        best_coord = None
        for n in native_avail:
            coord = abs(rx - n['x']) + abs(ry - n['y'])
            if best is None or coord < best_coord:
                best = n
                best_coord = coord

        eps = 0.0001
        candidates = [n for n in native_avail if abs((abs(rx - n['x']) + abs(ry - n['y'])) - best_coord) <= eps]

        if len(candidates) > 1 and m.get('ts') is not None:
            candidates.sort(key=lambda n: abs(((m['ts'] - n['ts']).total_seconds()) if n.get('ts') is not None else 1e9))
            chosen = candidates[0]
        else:
            chosen = candidates[0]

        native_avail = [n for n in native_avail if n['idx'] != chosen['idx']]
        pairs[m['idx']] = chosen

    # any remaining native_avail are unmatched
    unmatched_native = native_avail
    return pairs, unmatched_native


def print_summary(managed, native, screen, pairs, unmatched_native):
    print('Managed -> Native_Raw comparisons:')
    paired_list = []
    for m in managed:
        p = pairs.get(m['idx'])
        mid = m.get('id')
        if p is None:
            print(f"{m['idx']} (id={mid}): managed({m['rx']:.3f},{m['ry']:.3f}) native_raw(??,??) delta(??,??)  [line {m['line']}]")
        else:
            dx = m['rx'] - p['x']
            dy = m['ry'] - p['y']
            nid = p.get('id')
            print(f"{m['idx']} (id={mid}): managed({m['rx']:.3f},{m['ry']:.3f}) native_raw(id={nid} {p['x']:.3f},{p['y']:.3f}) delta({dx:.3f},{dy:.3f})  [m.line {m['line']} n.line {p['line']}]")
            paired_list.append((dx, dy))

    for n in unmatched_native:
        print(f"UNPAIRED native_raw(id={n.get('id')})({n['x']:.3f},{n['y']:.3f}) [line {n['line']}]")

    if paired_list:
        dxs = [d[0] for d in paired_list]
        dys = [d[1] for d in paired_list]
        print('\nStats:')
        print(f"count={len(paired_list)} dx mean={statistics.mean(dxs):.3f} max={max(dxs):.3f} dy mean={statistics.mean(dys):.3f} max={max(dys):.3f}")
    else:
        print('\nStats: no paired entries')

    if screen:
        print(f"\nFound {len(screen)} native screen rect entries (sample):")
        for i, s in enumerate(screen[:6]):
            print(f"{i+1}: screenRect ({s['sx']},{s['sy']}) size ({s['sw']},{s['sh']}) flipped={s['flipped']}")


def main():
    if len(sys.argv) < 2:
        print('Usage: compare_caret_rects.py <logfile>')
        sys.exit(2)

    logfile = sys.argv[1]
    managed, native, screen = parse_file_with_timestamps(logfile)

    # If the log contains id-tagged UpdateCaretRect entries, prefer those
    # and drop older CalculatePlatformInputCaretRect-derived entries to avoid
    # duplicate id=None managed rows in the comparison output.
    if any(m.get('id') is not None for m in managed):
        managed = [m for m in managed if m.get('id') is not None]

    pairs, unmatched_native = pair_entries(managed, native)
    print_summary(managed, native, screen, pairs, unmatched_native)

    # Threshold (in points) above which a delta is considered a failure
    EPSILON = 0.001

    # Determine if there are any mismatches to fail CI/tests
    failed = False

    # Any unmatched native entries is a failure
    if unmatched_native:
        print(f"FAIL: {len(unmatched_native)} unmatched native entries")
        failed = True

    # Check per-pair deltas
    for m in managed:
        p = pairs.get(m['idx'])
        if p is None:
            print(f"FAIL: managed entry {m['idx']} has no paired native entry")
            failed = True
            continue

        dx = abs(m['rx'] - p['x'])
        dy = abs(m['ry'] - p['y'])
        if dx > EPSILON or dy > EPSILON:
            print(f"FAIL: managed entry {m['idx']} delta too large: dx={dx:.3f}, dy={dy:.3f}")
            failed = True

    if failed:
        sys.exit(1)
    else:
        print('OK: All paired entries within tolerance')
        sys.exit(0)


if __name__ == '__main__':
    main()
