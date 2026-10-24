#!/usr/bin/env bash
# Owner rule: exactly one type declaration per file (entities/DTOs/enums included).
# Counts class/record/struct/enum/interface declarations per file; >1 = violation.
cd "$(dirname "$0")/.."
fail=0
for f in $(find src tests spikes -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*'); do
  n=$(sed 's|//.*||' "$f" | grep -Ec '(^|[[:space:]])(class|record|struct|enum|interface)[[:space:]]+[A-Za-z_]')
  if [ "$n" -gt 1 ]; then
    echo "VIOLATION: $f declares $n types"
    fail=1
  fi
done
if [ "$fail" = "0" ]; then echo "one-class-per-file: CLEAN"; fi
exit $fail
