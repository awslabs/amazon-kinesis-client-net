# Amazon Kinesis Client Library for .NET

This package provides an interface to the [Amazon Kinesis Client Library][amazon-kcl] (KCL) [MultiLangDaemon][multi-lang-daemon] for the .NET Framework.

Developers can use the KCL to build distributed applications that process streaming data reliably at scale. The KCL takes care of many of the complex tasks associated with distributed computing, such as load balancing across multiple instances, responding to instance failures, checkpointing processed records, and reacting to changes in stream volume.

This package wraps and manages the interaction with the *MultiLangDaemon*, which is provided as part of the [Amazon KCL for Java][amazon-kcl-github] so that developers can focus on implementing their record processing logic.

A record processor in C# typically looks something like the following:

```csharp
using Amazon.Kinesis.ClientLibrary;

namespace Sample
{
    public class RecordProcessor : IShardRecordProcessor
    {
        public void Initialize(InitializationInput input)
        {
            //
            // Initialize the record processor
            //
        }

        public void ProcessRecords(ProcessRecordsInput input)
        {
            //
            // Process a batch of records from input.Records, and optionally checkpoint by calling
            // input.Checkpointer.Checkpoint() 
            //
        }

        public void LeaseLost(LeaseLossInput leaseLossInput)
        {
            //
            // Perform any cleanup required.
            // This record processor has lost it's lease so checkpointing is not possible.
            // This is why LeaseLostInput does not provide a Checkpointer property,
            //
        }

        public void ShardEnded(ShardEndedInput shardEndedInput)
        {
            //
            // The record process has processed all records in the shard, and will no longer receive records.
            // It is required that this method call shardEndedInput.Checkpointer.Checkpoint() to inform the KCL
            // that the record processor has acknowledged the completion of the shard.
            //
        }

        public void ShutdownRequested(ShutdownRequestedInput shutdownRequestedInput)
        {
            //
            // This is called when the KCL is being shutdown, and if desired the record processor can checkpoint here
            // by calling shutdownRequestedInput.Checkpointer.Checkpoint(...)
            //
        }
    }

    internal class MainClass
    {
        public static void Main(string[] args)
        {
            KclProcess.Create(new RecordProcessor()).Run();
        }
    }
}
```

For more information about [Amazon Kinesis][amazon-kinesis] and the client libraries, see the
[official documentation][amazon-kinesis-docs] as well as the [Amazon Kinesis forums][kinesis-forum].

## Getting started


### Set up your AWS credentials

Before running a KCL application, make sure that your environment is configured to allow the *MultiLangDaemon* to access your [AWS security credentials](http://docs.aws.amazon.com/general/latest/gr/aws-security-credentials.html).

If you've installed the [AWS SDK for .NET][aws-sdk-dotnet], you may have already configured your AWS credentials using the SDK credential store in Microsoft Visual Studio; however, because Amazon KCL for .NET applications never deal with your credentials directly but defer to the *MultiLangDaemon*, this store is not available to your KCL application.

Instead, you can provide your credentials through environment variables (**AWS\_ACCESS\_KEY\_ID** and **AWS\_SECRET\_ACCESS\_KEY**) or a [credential profile][aws-sdk-dotnet-credentials] in your home directory.

### Building and running the sample projects

In addition to source code for the Amazon KCL for .NET itself, this repository contains a sample application, which can serve as a starting point for your KCL application. To try out this sample application, you can [download a ZIP][master-zip] of the latest sources, the contents of which can be opened as a solution in [Microsoft Visual Studio][visual-studio].

The sample application consists of two projects:

* **A data producer** (_SampleProducer\\SampleProducer.cs_)  
This program creates an Amazon Kinesis stream and continuously puts random records into it. There is commented-out code that deletes the created stream at the end; however, you should uncomment and use this code only if you do not intend to run SampleConsumer.

* **A data processor** (_SampleConsumer\\SampleConsumer.cs_)  
A new instance of this program is invoked by the *MultiLangDaemon* for each shard in the stream. It consumes the data from the shard. If you no longer need to work with the stream after running SampleConsumer, remember to delete both the Amazon DynamoDB checkpoint table and the Kinesis stream in your AWS account.

The following defaults are used in the sample application:

* **Stream name**: _kclnetsample_ 
* **Number of shards**: _1_

### Running the data producer

To run the data producer, run the *SampleProducer* project.
#### Notes

* The [AWS SDK for .NET][aws-sdk-dotnet] must be installed as a prerequisite to running the producer.

### Running the data processor

Because the Amazon KCL for .NET requires the *MultiLangDaemon*, which is provided by the Amazon KCL for Java, a bootstrap program has been provided. This program downloads all required dependencies prior to invoking the *MultiLangDaemon*, which executes the processor as a subprocess.

To run the processor, first **build the SampleConsumer project**, then **run the bootstrap project** with the following configuration:

* **Working directory**: _the root of the SampleConsumer project_
* **Arguments**: `--properties kcl.properties --execute`


#### Notes

* You must have [Java][jvm] installed.
* If you omit the `--execute` argument, the bootstrap program outputs a command that can be used to start the KCL directly.
* The *MultiLangDaemon* reads its configuration from the `kcl.properties` file, which contains a few important settings:
  * **executableName = dotnet SampleProcessor.dll**  
    The name of the processor executable, make sure `Bootstrap` is able to find its path.
  * **streamName = kclnetsample**  
    The name of the Kinesis stream from which to read data. This must match the stream name used by your producer.
  * More options are described in the [properties file][sample-properties].

### Cleaning up

This sample application creates a few resources in the default region of your AWS account:

* A Kinesis stream named _kclnetsample_, which stores the data generated by your producer
* A DynamoDB table named _DotNetKinesisSample_,  which tracks the state of your processor

Each of these resources will continue to incur AWS service costs until they are deleted. After you are finished testing the sample application, you can delete these resources through the [AWS Management Console][aws-console].

## What you should know about the MultiLangDaemon

The Amazon KCL for .NET uses the [Amazon KCL for Java][amazon-kcl-github] internally. We have implemented a Java-based daemon, called the *MultiLangDaemon*, which handles all of the heavy lifting. Our approach has the daemon spawn the user-defined record processor program as a sub-process. The *MultiLangDaemon* communicates with this sub-process over standard input/output using a simple protocol, and therefore the record processor program can be written in any language.

At runtime, there will always be a one-to-one correspondence between a record processor, a child process,
and an [Amazon Kinesis shard][amazon-kinesis-shard]. The *MultiLangDaemon* ensures that, without
any developer intervention.

In this release, we have abstracted these implementation details and exposed an interface that enables
you to focus on writing record processing logic in C#. This approach enables the [Amazon KCL][amazon-kcl] to
be language-agnostic, while providing identical features and similar parallel processing model across
all languages.

## Release Notes
### Release 4.1.0 (October 1, 2025)
* [#276](https://github.com/awslabs/amazon-kinesis-client-net/pull/276) Add multi-lang support for leaseAssignmentIntervalMillis
* [#281](https://github.com/awslabs/amazon-kinesis-client-net/pull/281) Upgrade amazon-kinesis-client from 3.0.0 to 3.1.3
* [#281](https://github.com/awslabs/amazon-kinesis-client-net/pull/281) Upgrade aws-sdk from 2.25.64 to 2.33.0
* [#281](https://github.com/awslabs/amazon-kinesis-client-net/pull/281) Upgrade netty.version from 4.1.108.Final to 4.2.4.Final
* [#281](https://github.com/awslabs/amazon-kinesis-client-net/pull/281) Upgrade fasterxml-jackson from 2.13.5 to 2.15.0
* [#281](https://github.com/awslabs/amazon-kinesis-client-net/pull/281) Upgrade checker-qual from 2.5.2 to 3.49.4
* [#281](https://github.com/awslabs/amazon-kinesis-client-net/pull/281) Upgrade org.apache.commons:commons-lang3 from 3.14.0 to 3.18.0
* [#266](https://github.com/awslabs/amazon-kinesis-client-net/pull/266) Upgrade commons-beanutils from 1.9.4 to 1.11.0

### Release 4.0.1 (May 28, 2025)
* [#264](https://github.com/awslabs/amazon-kinesis-client-net/pull/264) Bump ch.qos.logback:logback-core from 1.3.14 to 1.3.15
* [#263](https://github.com/awslabs/amazon-kinesis-client-net/pull/263) Bump io.netty:netty-handler from 4.1.108.Final to 4.1.118.Final

### Release 4.0.0 (November 6, 2024)
* New lease assignment / load balancing algorithm
  * KCL 3.x introduces a new lease assignment and load balancing algorithm. It assigns leases among workers based on worker utilization metrics and throughput on each lease, replacing the previous lease count-based lease assignment algorithm.
  * When KCL detects higher variance in CPU utilization among workers, it proactively reassigns leases from over-utilized workers to under-utilized workers for even load balancing. This ensures even CPU utilization across workers and removes the need to over-provision the stream processing compute hosts.
* Optimized DynamoDB RCU usage
  * KCL 3.x optimizes DynamoDB read capacity unit (RCU) usage on the lease table by implementing a global secondary index with leaseOwner as the partition key. This index mirrors the leaseKey attribute from the base lease table, allowing workers to efficiently discover their assigned leases by querying the index instead of scanning the entire table.
  * This approach significantly reduces read operations compared to earlier KCL versions, where workers performed full table scans, resulting in higher RCU consumption.
* Graceful lease handoff
  * KCL 3.x introduces a feature called "graceful lease handoff" to minimize data reprocessing during lease reassignments. Graceful lease handoff allows the current worker to complete checkpointing of processed records before transferring the lease to another worker. For graceful lease handoff, you should implement checkpointing logic within the existing `shutdownRequested()` method.
  * This feature is enabled by default in KCL 3.x, but you can turn off this feature by adjusting the configuration property `isGracefulLeaseHandoffEnabled`.
  * While this approach significantly reduces the probability of data reprocessing during lease transfers, it doesn't completely eliminate the possibility. To maintain data integrity and consistency, it's crucial to design your downstream consumer applications to be idempotent. This ensures that the application can handle potential duplicate record processing without adverse effects.
* New DynamoDB metadata management artifacts
  * KCL 3.x introduces two new DynamoDB tables for improved lease management:
    * Worker metrics table: Records CPU utilization metrics from each worker. KCL uses these metrics for optimal lease assignments, balancing resource utilization across workers. If CPU utilization metric is not available, KCL assigns leases to balance the total sum of shard throughput per worker instead.
    * Coordinator state table: Stores internal state information for workers. Used to coordinate in-place migration from KCL 2.x to KCL 3.x and leader election among workers.
  * Follow this [documentation](https://docs.aws.amazon.com/streams/latest/dev/kcl-migration-from-2-3.html#kcl-migration-from-2-3-IAM-permissions) to add required IAM permissions for your KCL application.
* Other improvements and changes
  * Dependency on the AWS SDK for Java 1.x has been fully removed.
    * The Glue Schema Registry integration functionality no longer depends on AWS SDK for Java 1.x. Previously, it required this as a transient dependency.
    * Multilangdaemon has been upgraded to use AWS SDK for Java 2.x. It no longer depends on AWS SDK for Java 1.x.
  * `idleTimeBetweenReadsInMillis` (PollingConfig) now has a minimum default value of 200.
    * This polling configuration property determines the [publishers](https://github.com/awslabs/amazon-kinesis-client/blob/master/amazon-kinesis-client/src/main/java/software/amazon/kinesis/retrieval/polling/PrefetchRecordsPublisher.java) wait time between GetRecords calls in both success and failure cases. Previously, setting this value below 200 caused unnecessary throttling. This is because Amazon Kinesis Data Streams supports up to five read transactions per second per shard for shared-throughput consumers.
  * Shard lifecycle management is improved to deal with edge cases around shard splits and merges to ensure records continue being processed as expected.
* Migration
  * The programming interfaces of KCL 3.x remain identical with KCL 2.x for an easier migration. For detailed migration instructions, please refer to the [Migrate consumers from KCL 2.x to KCL 3.x](https://docs.aws.amazon.com/streams/latest/dev/kcl-migration-from-2-3.html) page in the Amazon Kinesis Data Streams developer guide.
* Configuration properties
  * New configuration properties introduced in KCL 3.x are listed in this [doc](https://github.com/awslabs/amazon-kinesis-client/blob/master/docs/kcl-configurations.md#new-configurations-in-kcl-3x).
  * Deprecated configuration properties in KCL 3.x are listed in this [doc](https://github.com/awslabs/amazon-kinesis-client/blob/master/docs/kcl-configurations.md#discontinued-configuration-properties-in-kcl-3x). You need to keep the deprecated configuration properties during the migration from any previous KCL version to KCL 3.x.
* Metrics
  * New CloudWatch metrics introduced in KCL 3.x are explained in the [Monitor the Kinesis Client Library with Amazon CloudWatch](https://docs.aws.amazon.com/streams/latest/dev/monitoring-with-kcl.html) in the Amazon Kinesis Data Streams developer guide. The following operations are newly added in KCL 3.x:
    * `LeaseAssignmentManager`
    * `WorkerMetricStatsReporter`
    * `LeaseDiscovery`

### Release 3.0.1 (April 24, 2024)
* Upgraded amazon-kinesis-client from 2.4.4 to 2.5.8
* Upgraded netcoreapp from 5.0 to 6.0
* Upgraded awssdk.version from 2.19.2 to 2.19.16
* Upgraded fasterxml-jackson.version from 2.13.4 to 2.14.5
* Upgraded com.google.protobuf:protobuf-java from 3.21.5 to 3.23.0
* Upgraded aws-java-sdk-core from 1.12.370 to 1.12.468
* Upgraded error_prone_annotations from 2.7.1 to 2.19.1
* Upgraded NSubstitute from 3.1.0 to 5.1.0
* Upgraded Stateless from 2.5.11.0 to 5.13.0
* Upgraded MSTest.TestFramework from 1.3.2 to 3.1.1
* Upgraded MSTest.TestAdapter from 1.3.2 to 3.1.1
* Upgraded jackson-databind from 2.13.4 to 2.13.4.2
* Upgraded guava from 31.0.1-jre to 32.1.1-jre
* Upgraded io.netty:netty-codec-http from 4.1.86.Final to 4.1.108.Final
* Upgraded Microsoft.Extensions.DependencyInjection from 6.0.0 to 8.0.0
* Upgraded ch.qos.logback:logback-core from 1.3.0 to 1.3.12
* Upgraded Microsoft.NET.Test.Sdk from 15.5.0 to 17.9.0
* Upgraded AWSSDK.Kinesis from 3.7.1.5 to 3.7.301.73
* Upgraded commons-collections4 from 4.2 to 4.4
* Upgraded commons-logging from 1.1.3 to 1.2
* Upgraded NUnit3TestAdapter from 3.9.0 to 4.5.0
* Upgraded CommandLineParser from 2.2.1 to 2.9.1
* Upgraded Microsoft.NET.Test.Sdk from 15.5.0 to 17.9.0
* Upgraded NUnit from 3.9.0 to 4.1.0
* Upgraded ion-java from 1.5.1 to 1.11.4

### Release 3.0.0 (January 12, 2023)
* Upgraded to use version 2.4.4 of the [Amazon Kinesis Client library][amazon-kcl-github]

### Release 2.0.0 (February 27, 2019)
* Added support for [Enhanced Fan-Out](https://aws.amazon.com/blogs/aws/kds-enhanced-fanout/).  
  Enhanced Fan-Out provides dedicated throughput per stream consumer, and uses an HTTP/2 push API (SubscribeToShard) to deliver records with lower latency.
* Updated the Amazon Kinesis Client Library for Java to version 2.1.2.
  * Version 2.1.2 uses 4 additional Kinesis API's  
    __WARNING: These additional API's may require updating any explicit IAM policies__
    * [`RegisterStreamConsumer`](https://docs.aws.amazon.com/kinesis/latest/APIReference/API_RegisterStreamConsumer.html)
    * [`SubscribeToShard`](https://docs.aws.amazon.com/kinesis/latest/APIReference/API_SubscribeToShard.html)
    * [`DescribeStreamConsumer`](https://docs.aws.amazon.com/kinesis/latest/APIReference/API_DescribeStreamConsumer.html)
    * [`DescribeStreamSummary`](https://docs.aws.amazon.com/kinesis/latest/APIReference/API_DescribeStreamSummary.html)
  * For more information about Enhanced Fan-Out with the Amazon Kinesis Client Library please see the [announcement](https://aws.amazon.com/blogs/aws/kds-enhanced-fanout/) and [developer documentation](https://docs.aws.amazon.com/streams/latest/dev/introduction-to-enhanced-consumers.html).
* Added a new record processor interface [`IShardRecordProcessor`](https://github.com/awslabs/amazon-kinesis-client-net/blob/95fd04a5702c287358eb3f58057017a6fd96000d/ClientLibrary/IShardRecordProcessor.cs#L18). This interface closely matches the Java [`ShardRecordProcessor`](https://github.com/awslabs/amazon-kinesis-client/blob/258be9a504a0e179d9cf9e0eaa6e0cf99003578b/amazon-kinesis-client/src/main/java/software/amazon/kinesis/processor/ShardRecordProcessor.java#L27) interface.  
  While the original `IRecordProcessor` interface remains present, and will continue to work it's recommended to upgrade to the newer interface.
  * The `Shutdown` method from `IRecordProcessor` has been replaced by `LeaseLost` and `ShardEnded`.
  * Added the `LeaseLost` method which is invoked when a lease is lost.  
    `LeaseLost` replaces `Shutdown` where `ShutdownInput.Reason` was `ShutdownReason.ZOMBIE`.
  * Added the `ShardEnded` method which is invoked when all records from a split or merge have been processed.  
    `ShardEnded`  replaces `Shutdown` where `ShutdownInput.Reason` was `ShutdownReason.TERMINATE`.
  * Added `ShutdownRequested` which provides the record processor a last chance to checkpoint during the Amazon Kinesis Client Library shutdown process before the lease is canceled.  
    * To control how long the Amazon Kinesis Client Library waits for the record processors to complete shutdown, add `timeoutInSeconds=<seconds to wait>` to your properties file.
* Updated the AWS Java SDK version to 2.4.0
* MultiLangDaemon now provides logging using Logback.
  * MultiLangDaemon supports custom configurations for logging via a Logback XML configuration file.
  * The Bootstrap program was been updated to accept either `-l` or `--log-configuration` to provide a Logback XML configuration file.

## See Also

* [Developing Processor Applications for Amazon Kinesis Using the Amazon Kinesis Client Library][amazon-kcl]
* [Amazon KCL for Java][amazon-kcl-github]
* [Amazon KCL for Python][amazon-kinesis-python-github]
* [Amazon KCL for Ruby][amazon-kinesis-ruby-github]
* [Amazon KCL for Node.js][amazon-kinesis-nodejs-github]
* [Amazon Kinesis documentation][amazon-kinesis-docs]
* [Amazon Kinesis forum][kinesis-forum]

[amazon-kinesis]: http://aws.amazon.com/kinesis
[amazon-kinesis-docs]: http://aws.amazon.com/documentation/kinesis/
[amazon-kinesis-shard]: http://docs.aws.amazon.com/kinesis/latest/dev/key-concepts.html
[amazon-kcl]: http://docs.aws.amazon.com/kinesis/latest/dev/kinesis-record-processor-app.html
[amazon-kcl-github]: https://github.com/awslabs/amazon-kinesis-client
[amazon-kinesis-python-github]: https://github.com/awslabs/amazon-kinesis-client-python
[amazon-kinesis-ruby-github]: https://github.com/awslabs/amazon-kinesis-client-ruby
[amazon-kinesis-nodejs-github]: https://github.com/awslabs/amazon-kinesis-client-nodejs
[multi-lang-daemon]: https://github.com/awslabs/amazon-kinesis-client/blob/master/amazon-kinesis-client-multilang/src/main/java/com/amazonaws/services/kinesis/multilang/package-info.java
[kinesis-forum]: http://developer.amazonwebservices.com/connect/forum.jspa?forumID=169
[master-zip]: https://github.com/awslabs/amazon-kinesis-client-net/archive/master.zip
[aws-sdk-dotnet]: https://aws.amazon.com/sdk-for-net/
[aws-sdk-dotnet-credentials]: http://docs.aws.amazon.com/AWSSdkDocsNET/latest/DeveloperGuide/net-dg-config-creds.html#net-dg-config-creds-creds-file
[aws-console]: http://aws.amazon.com/console/
[sample-properties]: https://github.com/awslabs/amazon-kinesis-client-net/blob/master/SampleConsumer/kcl.properties
[visual-studio]: http://www.visualstudio.com/
[jvm]: http://java.com/en/download/

## License

This library is licensed under the Apache 2.0 License. 

