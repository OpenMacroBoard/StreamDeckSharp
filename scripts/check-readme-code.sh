#!/bin/sh
# Checks that the quick start code in the READMEs is identical to the GettingStarted
# example project. That example is compiled with the solution, so the code shown in
# the READMEs is code that compiles.
#
# To change the quick start code: edit the example, then copy it into the READMEs.
set -eu

cd "$(dirname "$0")/.."

example=examples/OpenMacroBoard.Examples.GettingStarted/Program.cs
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

# Compare without byte order mark and without CR, so line endings do not matter.
normalize() {
    tr -d '\r' | awk 'NR == 1 { sub(/^\357\273\277/, "") } { print }'
}

normalize < "$example" > "$tmp/example"

status=0

for readme in README.md src/OpenMacroBoard.SDK/README.md; do
    # The first fenced csharp block of the README
    normalize < "$readme" \
        | awk '/^```csharp$/ { inside = 1; next } /^```$/ { if (inside) exit } inside { print }' \
        > "$tmp/readme"

    if cmp -s "$tmp/example" "$tmp/readme"; then
        echo "OK: quick start code in $readme matches $example"
    else
        echo "ERROR: quick start code in $readme differs from $example" >&2
        diff "$tmp/example" "$tmp/readme" >&2 || true
        status=1
    fi
done

exit $status
