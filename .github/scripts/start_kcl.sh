#!/bin/bash
set -e
set -o pipefail

# Get records from stream to verify they exist before continuing
SHARD_ITERATOR=$(aws kinesis get-shard-iterator --stream-name $STREAM_NAME --shard-id shardId-000000000000 --shard-iterator-type TRIM_HORIZON --query 'ShardIterator' --output text)
INITIAL_RECORDS=$(aws kinesis get-records --shard-iterator $SHARD_ITERATOR)
RECORD_COUNT_BEFORE=$(echo $INITIAL_RECORDS | jq '.Records | length')

echo "Found $RECORD_COUNT_BEFORE records in stream before KCL start"

if [[ "$RUNNER_OS" == "macOS" ]]; then
  brew install coreutils
  (cd SampleConsumer && gtimeout $RUN_TIME_SECONDS dotnet run --project ../Bootstrap/Bootstrap.csproj --properties kcl.properties --execute 2>&1 | tee ../kcl_output.log) || [ $? -eq 124 ]
elif [[ "$RUNNER_OS" == "Linux" || "$RUNNER_OS" == "Windows" ]]; then
  (cd SampleConsumer && timeout $RUN_TIME_SECONDS dotnet run --project ../Bootstrap/Bootstrap.csproj --properties kcl.properties --execute 2>&1 | tee ../kcl_output.log) || [ $? -eq 124 ]
else
  echo "Unknown OS: $RUNNER_OS"
  exit 1
fi

echo "---------ERROR LOGS HERE-------"
grep -i error kcl_output.log || echo "No errors found in logs"