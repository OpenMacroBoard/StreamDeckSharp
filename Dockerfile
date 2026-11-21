# syntax=docker/dockerfile:1
#
# Builds the whole solution, runs the tests and creates the NuGet packages.
# Nothing here publishes anything. Everything is plain `dotnet` commands, so the
# same steps work without Docker if you have the .NET SDK installed.
#
# Build and test (the build fails if compilation or a test fails):
#
#   docker build .
#
# Create NuGet packages for local testing in ./output/packages. They get a local
# pre-release version such as 11.0.0-local.20261116201530, so they can never be
# mistaken for a release or collide with one in your NuGet cache:
#
#   docker build --target packages --output type=local,dest=output/packages .
#
# Create packages with the release version (for publishing; pass an empty suffix):
#
#   docker build --target packages --build-arg VERSION_SUFFIX= \
#       --output type=local,dest=output/packages .
#
# Copy the test results (.trx) to ./output/test-results, also when tests fail:
#
#   docker build --target test-results --output type=local,dest=output/test-results .
#
# Packing compares the public API with the last released packages and fails on breaking
# changes that are not acknowledged in a CompatibilitySuppressions.xml file. To acknowledge
# the current breaking changes, rewrite those files in the source tree, then review the diff:
#
#   docker build --target api-suppressions --output type=local,dest=. .

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

ARG CONFIGURATION=Release
# NuGet's vulnerability audit runs during restore, and most projects treat its warnings
# as errors. If a new advisory breaks the build before a fix is available, pass
# --build-arg NUGET_AUDIT=false to build anyway.
ARG NUGET_AUDIT=true

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    CONFIGURATION=${CONFIGURATION} \
    # Snapshot tests (Verify) must fail on a mismatch instead of trying to open a diff tool
    DiffEngine_Disabled=true \
    NuGetAudit=${NUGET_AUDIT} \
    # Some projects target net10.0-windows; this lets them compile on Linux
    EnableWindowsTargeting=true \
    # The projects that are published as NuGet packages
    PACKAGE_PROJECTS="\
        src/OpenMacroBoard.SDK/OpenMacroBoard.SDK.csproj \
        src/OpenMacroBoard.SocketIO/OpenMacroBoard.SocketIO.csproj \
        src/OpenMacroBoard.SDK.KeyBitmap.GDI/OpenMacroBoard.SDK.KeyBitmap.GDI.csproj \
        src/OpenMacroBoard.SDK.KeyBitmap.SkiaSharp/OpenMacroBoard.SDK.KeyBitmap.SkiaSharp.csproj \
        src/StreamDeckSharp/StreamDeckSharp.csproj"

WORKDIR /repo
COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore StreamDeckSharp.sln

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build StreamDeckSharp.sln --configuration "${CONFIGURATION}" --no-restore


FROM build AS test

# OpenMacroBoard.Tests-Win needs GDI+ and is left out, it only runs on Windows.
# Every test project runs, even if an earlier one fails. The outcome is only recorded here,
# so the test results can still be copied out; the last stage fails the build if a test failed.
# The README check makes sure the quick start code in the READMEs is the compiled example.
RUN --mount=type=cache,target=/root/.nuget/packages \
    failed=0; \
    sh scripts/check-readme-code.sh || failed=1; \
    for project in \
        tests/OpenMacroBoard.Tests/OpenMacroBoard.Tests.csproj \
        tests/StreamDeckSharp.Tests/StreamDeckSharp.Tests.csproj \
    ; do \
        dotnet test "$project" \
            --configuration "${CONFIGURATION}" --no-build \
            --logger "trx;LogFileName=$(basename "$project" .csproj).trx" \
            --results-directory /test-results \
        || failed=1; \
    done; \
    echo $failed > /test-exit-code

FROM scratch AS test-results
COPY --from=test /test-results /


FROM build AS pack

# Pre-release suffix of the package version:
#   local (default)  a local test version, 11.0.0-local.<UTC timestamp>; the timestamp
#                    changes whenever the sources change, so NuGet never serves a stale copy
#   empty            the release version, 11.0.0
#   anything else    used as given, for example beta.1
ARG VERSION_SUFFIX=local

# The READMEs inside the packages get absolute links, because nuget.org cannot resolve
# relative ones. They point to this branch or tag of the repository. For a release pass
# the tag of the release, so the links keep showing the files of that version.
ARG README_LINK_REF=main

# Packing does not depend on the test stage, so packages for local testing can be
# created while a test is failing. Run the tests before publishing anything.
# Packing also runs the package validation: it fails if the public API has breaking changes
# compared to the last release that are not listed in CompatibilitySuppressions.xml.
RUN --mount=type=cache,target=/root/.nuget/packages \
    set -e; \
    suffix="$VERSION_SUFFIX"; \
    if [ "$suffix" = local ]; then suffix="local.$(date -u +%Y%m%d%H%M%S)"; fi; \
    for project in $PACKAGE_PROJECTS; do \
        dotnet pack "$project" \
            --configuration "${CONFIGURATION}" \
            ${suffix:+--version-suffix "$suffix"} \
            -p:ReadmeLinkRef="$README_LINK_REF" \
            --output /packages; \
    done

FROM scratch AS packages
COPY --from=pack /packages /


FROM build AS api-suppressions-build

# Rewrites the CompatibilitySuppressions.xml files, so they list exactly the breaking changes
# the code has right now. A project without breaking changes gets no file.
RUN --mount=type=cache,target=/root/.nuget/packages \
    set -e; \
    mkdir /suppressions; \
    for project in $PACKAGE_PROJECTS; do \
        dotnet pack "$project" \
            --configuration "${CONFIGURATION}" \
            -p:ApiCompatGenerateSuppressionFile=true \
            --output /tmp/unused-packages; \
        file="$(dirname "$project")/CompatibilitySuppressions.xml"; \
        if [ -f "$file" ]; then \
            mkdir -p "/suppressions/$(dirname "$project")"; \
            cp "$file" "/suppressions/$file"; \
        fi; \
    done

FROM scratch AS api-suppressions
COPY --from=api-suppressions-build /suppressions /


# Default target: build + test + package validation. Fails if any test failed or if the
# packages have breaking changes that are not acknowledged.
FROM test AS verify
COPY --from=pack /packages /tmp/packages
RUN exit "$(cat /test-exit-code)"
