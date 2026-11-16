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
# Create the NuGet packages in ./output/packages:
#
#   docker build --target packages --output type=local,dest=output/packages .
#
#   For local testing add a version suffix, so the packages do not collide with a
#   released version in your NuGet cache (gives for example 11.0.0-local.1):
#
#   docker build --target packages --build-arg VERSION_SUFFIX=local.1 \
#       --output type=local,dest=output/packages .
#
# Copy the test results (.trx) to ./output/test-results, also when tests fail:
#
#   docker build --target test-results --output type=local,dest=output/test-results .

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

ARG CONFIGURATION=Release
# NuGet's vulnerability audit runs during restore, and most projects treat its warnings
# as errors. Pass --build-arg NUGET_AUDIT=false to build despite known advisories.
ARG NUGET_AUDIT=true

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    CONFIGURATION=${CONFIGURATION} \
    # Snapshot tests (Verify) must fail on a mismatch instead of trying to open a diff tool
    DiffEngine_Disabled=true \
    NuGetAudit=${NUGET_AUDIT} \
    # Some projects target net10.0-windows; this lets them compile on Linux
    EnableWindowsTargeting=true

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
RUN --mount=type=cache,target=/root/.nuget/packages \
    failed=0; \
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

# Optional pre-release suffix, for example "local.1". Empty gives the release version.
ARG VERSION_SUFFIX=

# Packing does not depend on the test stage, so packages for local testing can be
# created while a test is failing. Run the tests before publishing anything.
RUN --mount=type=cache,target=/root/.nuget/packages \
    set -e; \
    for project in \
        src/OpenMacroBoard.SDK/OpenMacroBoard.SDK.csproj \
        src/OpenMacroBoard.SocketIO/OpenMacroBoard.SocketIO.csproj \
        src/OpenMacroBoard.SDK.KeyBitmap.GDI/OpenMacroBoard.SDK.KeyBitmap.GDI.csproj \
        src/StreamDeckSharp/StreamDeckSharp.csproj \
    ; do \
        dotnet pack "$project" \
            --configuration "${CONFIGURATION}" \
            ${VERSION_SUFFIX:+--version-suffix "$VERSION_SUFFIX"} \
            --output /packages; \
    done

FROM scratch AS packages
COPY --from=pack /packages /


# Default target: build + test. Fails if any test failed.
FROM test AS verify
RUN exit "$(cat /test-exit-code)"
