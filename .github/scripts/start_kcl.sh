#!/bin/bash
set -e
set -o pipefail

SAMPLE_PROPERTIES="../.github/resources/kcl.properties"

if [[ "$RUNNER_OS" == "macOS" ]]; then
  brew install coreutils
  (cd SampleConsumer && gtimeout $RUN_TIME_SECONDS dotnet run --project ../Bootstrap/Bootstrap.csproj --properties $SAMPLE_PROPERTIES --execute 2>&1 | tee ../kcl_output.log) || [ $? -eq 124 ]
elif [[ "$RUNNER_OS" == "Linux" || "$RUNNER_OS" == "Windows" ]]; then
  (cd SampleConsumer && timeout $RUN_TIME_SECONDS dotnet run --project ../Bootstrap/Bootstrap.csproj --properties $SAMPLE_PROPERTIES --execute 2>&1 | tee ../kcl_output.log) || [ $? -eq 124 ]
else
  echo "Unknown OS: $RUNNER_OS"
  exit 1
fi

echo "---------ERROR LOGS HERE-------"
grep -i error kcl_output.log || echo "No errors found in logs"