//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
//
// Licensed under the Amazon Software License (the "License").
// You may not use this file except in compliance with the License.
// A copy of the License is located at
//
//  http://aws.amazon.com/asl/
//
// or in the "license" file accompanying this file. This file is distributed
// on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
// express or implied. See the License for the specific language governing
// permissions and limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.Kinesis.ClientLibrary;

namespace Amazon.Kinesis.ClientLibrary.SampleConsumer
{
    /// <summary>
    /// A sample processor of Kinesis records.
    /// </summary>
    class SampleRecordProcessor : IShardRecordProcessor
    {
        /// <value>The time to wait before this record processor
        /// reattempts either a checkpoint, or the processing of a record.</value>
        private static readonly TimeSpan Backoff = TimeSpan.FromSeconds(3);

        /// <value>The interval this record processor waits between
        /// doing two successive checkpoints.</value>
        private static readonly TimeSpan CheckpointInterval = TimeSpan.FromMinutes(1);

        /// <value>The maximum number of times this record processor retries either
        /// a failed checkpoint, or the processing of a record that previously failed.</value>
        private static readonly int NumRetries = 10;

        /// <value>The shard ID on which this record processor is working.</value>
        private string _kinesisShardId;

        /// <value>The next checkpoint time expressed in milliseconds.</value>
        private DateTime _nextCheckpointTime = DateTime.UtcNow;

        /// <summary>
        /// This method is invoked by the Amazon Kinesis Client Library before records from the specified shard
        /// are delivered to this SampleRecordProcessor.
        /// </summary>
        /// <param name="input">
        /// InitializationInput containing information such as the name of the shard whose records this
        /// SampleRecordProcessor will process.
        /// </param>
        public void Initialize(InitializationInput input)
        {
            Console.Error.WriteLine("Initializing record processor for shard: " + input.ShardId);
            this._kinesisShardId = input.ShardId;
        }

        /// <summary>
        /// This method processes the given records and checkpoints using the given checkpointer.
        /// </summary>
        /// <param name="input">
        /// ProcessRecordsInput that contains records, a Checkpointer and contextual information.
        /// </param>
        public void ProcessRecords(ProcessRecordsInput input)
        {
            // Process records and perform all exception handling.
            ProcessRecordsWithRetries(input.Records);

            // Checkpoint once every checkpoint interval.
            if (DateTime.UtcNow >= _nextCheckpointTime)
            {
                Checkpoint(input.Checkpointer);
                _nextCheckpointTime = DateTime.UtcNow + CheckpointInterval;
            }
        }

        /// <summary>
        /// This method processes records, performing retries as needed.
        /// </summary>
        /// <param name="records">The records to be processed.</param>
        private void ProcessRecordsWithRetries(List<Record> records)
        {
            foreach (Record rec in records)
            {
                bool processedSuccessfully = false;
                string data = null;
                for (int i = 0; i < NumRetries; ++i)
                {
                    try
                    {
                        // As per the accompanying AmazonKinesisSampleProducer.cs, the payload
                        // is interpreted as UTF-8 characters.
                        data = System.Text.Encoding.UTF8.GetString(rec.Data);

                        // Uncomment the following if you wish to see the retrieved record data.
                        //Console.Error.WriteLine(
                        //    String.Format("Retrieved record:\n\tpartition key = {0},\n\tsequence number = {1},\n\tdata = {2}",
                        //    rec.PartitionKey, rec.SequenceNumber, data));

                        // Your own logic to process a record goes here.

                        processedSuccessfully = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Exception processing record data: " + data, e);
                        //Back off before retrying upon an exception.
                        Thread.Sleep(Backoff);
                    }
                }

                if (!processedSuccessfully)
                {
                    Console.Error.WriteLine("Couldn't process record " + rec + ". Skipping the record.");
                }
            }
        }

        /// <summary>
        /// This checkpoints the specified checkpointer with retries.
        /// </summary>
        /// <param name="checkpointer">The checkpointer used to do checkpoints.</param>
        private void Checkpoint(Checkpointer checkpointer)
        {
            Console.Error.WriteLine("Checkpointing shard " + _kinesisShardId);

            // You can optionally provide an error handling delegate to be invoked when checkpointing fails.
            // The library comes with a default implementation that retries for a number of times with a fixed
            // delay between each attempt. If you do not provide an error handler, the checkpointing operation
            // will not be retried, but processing will continue.
            checkpointer.Checkpoint(RetryingCheckpointErrorHandler.Create(NumRetries, Backoff));
        }

        public void LeaseLost(LeaseLossInput leaseLossInput)
        {
            //
            // Perform any necessary cleanup after losing your lease.  Checkpointing is not possible at this point.
            //
            Console.Error.WriteLine($"Lost lease on {_kinesisShardId}");
        }

        public void ShardEnded(ShardEndedInput shardEndedInput)
        {
            //
            // Once the shard has ended it means you have processed all records on the shard. To confirm completion the
            // KCL requires that you checkpoint one final time using the default checkpoint value.
            //
            Console.Error.WriteLine(
                $"All records for {_kinesisShardId} have been processed, starting final checkpoint");
            shardEndedInput.Checkpointer.Checkpoint();
        }

        public void ShutdownRequested(ShutdownRequestedInput shutdownRequestedInput)
        {
            Console.Error.WriteLine($"Shutdown has been requested for {_kinesisShardId}. Checkpointing");
            shutdownRequestedInput.Checkpointer.Checkpoint();
        }
    }

    class MainClass
    {
        /// <summary>
        /// This method creates a KclProcess and starts running an SampleRecordProcessor instance.
        /// </summary>
        public static void Main(string[] args)
        {
            try
            {
                KclProcess.Create(new SampleRecordProcessor()).Run();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("ERROR: " + e);
            }
        }
    }
}