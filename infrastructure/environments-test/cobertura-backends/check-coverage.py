#!/usr/bin/env python3
import glob
import os
import sys
import xml.etree.ElementTree as ET

LINE_FLOOR = 100.0


def find_report(path):
    if os.path.isfile(path):
        return path
    matches = glob.glob(os.path.join(path, "**", "coverage.cobertura.xml"), recursive=True)
    if not matches:
        return None
    return max(matches, key=os.path.getmtime)


def main():
    if len(sys.argv) < 2:
        print("usage: check-coverage.py <service> [results-dir]", file=sys.stderr)
        sys.exit(2)

    service = sys.argv[1]
    results_dir = sys.argv[2] if len(sys.argv) > 2 else os.path.join("apps", service, "Test", "TestResults")

    report = find_report(results_dir)
    if report is None:
        print(f"[{service}] No coverage.cobertura.xml found under {results_dir}", file=sys.stderr)
        sys.exit(2)

    root = ET.parse(report).getroot()
    line_rate = float(root.get("line-rate", "0")) * 100

    gaps = {}
    for class_element in root.iter("class"):
        rate = float(class_element.get("line-rate", "1")) * 100
        if rate < LINE_FLOOR:
            name = class_element.get("filename", class_element.get("name", "?"))
            gaps[name] = rate

    print(f"[{service}] business-logic line coverage = {line_rate:.2f}% (floor {LINE_FLOOR:.0f}%)")

    if line_rate < LINE_FLOOR:
        print(f"\nCritical coverage gate FAILED for {service}:", file=sys.stderr)
        for name, rate in sorted(gaps.items()):
            print(f"  - {name}: lines {rate:.2f}%", file=sys.stderr)
        print(f"\n{len(gaps)} file(s) below the {LINE_FLOOR:.0f}% line floor for business logic.", file=sys.stderr)
        sys.exit(1)

    print(f"[{service}] Critical coverage gate passed.")


if __name__ == "__main__":
    main()
