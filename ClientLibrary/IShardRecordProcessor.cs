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

namespace Amazon.Kinesis.ClientLibrary
{
    public interface IShardRecordProcessor
    {
        /// <summary>
        ///     Invoked by the Amazon Kinesis Client Library before data records are delivered to the IRecordProcessor
        ///     instance (via processRecords).
        /// </summary>
        /// <param name="input">
        ///     The InitializationInput containing information such as the shard id being assigned to this IRecordProcessor.
        /// </param>
        void Initialize(InitializationInput input);

        /// <summary>
        ///     <para>
        ///         Process data records. The Amazon Kinesis Client Library will invoke this method to deliver data records to the
        ///         application.
        ///     </para>
        ///     <para>
        ///         Upon fail over, the new instance will get records with sequence number > checkpoint position
        ///         for each partition key.
        ///     </para>
        /// </summary>
        /// <param name="input">
        ///     ProcessRecordsInput that contains a batch of records, a Checkpointer, as well as relevant contextual information.
        /// </param>
        void ProcessRecords(ProcessRecordsInput input);

        /// <summary>
        ///     This is invoked when the record processor no longer holds the lease and must shut down.
        /// </summary>
        /// <remarks>
        ///     <para>The record processor should do any necessary cleanup and prepare to exit.</para>
        ///     <para>*Any attempts to checkpoint will fail after a record processor has lost its lease.*</para>
        /// </remarks>
        /// <param name="leaseLossInput">Information related to the lease loss</param>
        void LeaseLost(LeaseLossInput leaseLossInput);

        /// <summary>
        ///     This is invoked when the record processor has reached the end of the shard, and will no longer receive additional
        ///     records.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Scaling shards in Amazon Kinesis Data Streams will close shards to indicate that they have been
        ///         replaced by the scaled shards.  A closed shard can no longer receive records from PutRecord or PutRecords,
        ///         and therefore when the end of the shard is reached can no longer
        ///     </para>
        /// </remarks>
        /// <param name="shardEndedInput">Information related to the end </param>
        void ShardEnded(ShardEndedInput shardEndedInput);

        /// <summary>
        ///     Invoked when the parent process has been requested to shutdown.
        /// </summary>
        /// <remarks>
        ///     This provides a final chance to checkpoint while the record processor still holds
        /// </remarks>
        /// <param name="shutdownRequestedInput"></param>
        void ShutdownRequested(ShutdownRequestedInput shutdownRequestedInput);

    }
}