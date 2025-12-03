#!/bin/bash
set -e

# Build the Bootstrap project and add project references
dotnet add Bootstrap/Bootstrap.csproj reference SampleConsumer/SampleConsumer.csproj
dotnet add Bootstrap/Bootstrap.csproj reference ClientLibrary/ClientLibrary.csproj
dotnet build SampleConsumer/SampleConsumer.csproj -o SampleConsumer/bin
dotnet build Bootstrap/Bootstrap.csproj -o SampleConsumer/bin

SAMPLE_PROPERTIES=".github/resources/kcl.properties"

# Manipulate sample.properties file that the KCL application pulls properties from (ex: streamName, applicationName)
# Depending on the OS, different properties need to be changed
if [[ "$RUNNER_OS" == "macOS" ]]; then
  sed -i "" "s/kclnetsample/$STREAM_NAME/g" $SAMPLE_PROPERTIES
  sed -i "" "s/DotNetKinesisSample/$APP_NAME/g" $SAMPLE_PROPERTIES
  sed -i "" "s|executableName = dotnet SampleConsumer.dll|executableName = dotnet bin/SampleConsumer.dll|g" $SAMPLE_PROPERTIES
  sed -i "" "51s/kclnetsample/$STREAM_NAME/g" SampleProducer/SampleProducer.cs
elif [[ "$RUNNER_OS" == "Linux" || "$RUNNER_OS" == "Windows" ]]; then
  sed -i "s/kclnetsample/$STREAM_NAME/g" $SAMPLE_PROPERTIES
  sed -i "s/DotNetKinesisSample/$APP_NAME/g" $SAMPLE_PROPERTIES
  sed -i "s|executableName = dotnet SampleConsumer.dll|executableName = dotnet bin/SampleConsumer.dll|g" $SAMPLE_PROPERTIES
  sed -i "51s/kclnetsample/$STREAM_NAME/g" SampleProducer/SampleProducer.cs
else
  echo "Unknown OS: $RUNNER_OS"
  exit 1
fi

cat $SAMPLE_PROPERTIES