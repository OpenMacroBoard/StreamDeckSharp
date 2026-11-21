#!/bin/sh
#
# Creates the NuGet packages of a release in a folder. Publishes nothing.
#
#   sh scripts/pack-release.sh <tag> [<output folder>]
#
#   sh scripts/pack-release.sh v11.0            packages with version 11.0.0
#   sh scripts/pack-release.sh v11.0.1          packages with version 11.0.1
#   sh scripts/pack-release.sh v11.1-beta.1     packages with version 11.1.0-beta.1
#
# Run it on a checkout of that tag. The tag decides two things:
#
#   - the links in the package READMEs point to the files of that tag
#   - the pre-release part of the package version (the part after the "-")
#
# The version number itself comes from the project files. The script fails if a package
# does not get exactly the version the tag stands for, so a forgotten version change is
# noticed before anything is published.
#
# The release workflow (.github/workflows/release.yml) calls this script. It only needs
# Docker, so the same command works on a developer machine.

set -eu

tag="${1:-}"
output="${2:-output/packages}"

# v<major>.<minor>, optionally .<patch>, optionally -<pre-release part>
if ! printf '%s\n' "$tag" | grep -Eq '^v[0-9]+\.[0-9]+(\.[0-9]+)?(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$'; then
    echo "error: '$tag' is not a release tag. Expected for example v11.0, v11.0.1 or v11.1-beta.1." >&2
    exit 1
fi

number="${tag#v}"
suffix=""
case "$number" in
    *-*)
        suffix="${number#*-}"
        number="${number%%-*}"
        ;;
esac

# A tag like v11.0 stands for the version 11.0.0
case "$number" in
    *.*.*) ;;
    *) number="$number.0" ;;
esac

version="$number${suffix:+-$suffix}"

cd "$(dirname "$0")/.."

if [ -n "$(ls -A "$output" 2>/dev/null)" ]; then
    echo "error: the output folder '$output' is not empty." >&2
    exit 1
fi

echo "Packing version $version (tag $tag) into $output"

docker build \
    --target packages \
    --build-arg "VERSION_SUFFIX=$suffix" \
    --build-arg "README_LINK_REF=$tag" \
    --output "type=local,dest=$output" \
    .

failed=0
count=0

for package in "$output"/*.nupkg; do
    [ -e "$package" ] || continue
    count=$((count + 1))

    case "$(basename "$package")" in
        *[A-Za-z]".$version.nupkg")
            echo "  ok     $(basename "$package")"
            ;;
        *)
            echo "  WRONG  $(basename "$package")"
            failed=1
            ;;
    esac
done

if [ "$count" -eq 0 ]; then
    echo "error: no packages were created." >&2
    exit 1
fi

if [ "$failed" -ne 0 ]; then
    echo "error: the tag $tag stands for version $version, but not every package has that version." >&2
    echo "Change the tag or the VersionPrefix in the project files, so they agree." >&2
    exit 1
fi

echo "$count packages with version $version are in $output"
