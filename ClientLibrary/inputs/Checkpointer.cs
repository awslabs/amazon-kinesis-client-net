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
    /// <summary>
    /// Used by IRecordProcessors when they want to checkpoint their progress.
    /// The Amazon Kinesis Client Library will pass an object implementing this interface to IRecordProcessors,
    /// so they can checkpoint their progress.
    /// </summary>
    public abstract class Checkpointer
    {
        internal abstract void Checkpoint(string sequenceNumber, CheckpointErrorHandler errorHandler = null);

        /// <summary>
        /// <para>
        /// This method will checkpoint the progress at the last data record that was delivered to the record processor.
        /// </para>
        /// <para>
        /// Upon failover (after a successful checkpoint() call), the new/replacement IRecordProcessor instance
        /// will receive data records whose sequenceNumber > checkpoint position (for each partition key).
        /// </para>
        /// <para>
        /// In steady state, applications should checkpoint periodically (e.g. once every 5 minutes).
        /// </para>
        /// <para>
        /// Calling this API too frequently can slow down the application (because it puts pressure on the underlying
        /// checkpoint storage layer).
        /// </para>
        /// <para>
        /// You may optionally pass a CheckpointErrorHandler to the method, which will be invoked when the
        /// checkpoint operation fails. 
        /// </para>
        /// </summary>
        /// <param name="errorHandler">CheckpointErrorHandler that is invoked when the checkpoint operation fails.</param>
        public void Checkpoint(CheckpointErrorHandler errorHandler = null)
        {
            Checkpoint(null as string, errorHandler);
        }

        /// <summary>
        /// <para>
        /// This method will checkpoint the progress at the sequence number of the specified record.
        /// </para>
        /// <para>
        /// Upon failover (after a successful checkpoint() call), the new/replacement IRecordProcessor instance
        /// will receive data records whose sequenceNumber > checkpoint position (for each partition key).
        /// </para>
        /// <para>
        /// In steady state, applications should checkpoint periodically (e.g. once every 5 minutes).
        /// </para>
        /// <para>
        /// Calling this API too frequently can slow down the application (because it puts pressure on the underlying
        /// checkpoint storage layer).
        /// </para>
        /// <para>
        /// You may optionally pass a CheckpointErrorHandler to the method, which will be invoked when the
        /// checkpoint operation fails. 
        /// </para>
        /// </summary>
        /// <param name="record">Record whose sequence number to checkpoint at.</param>
        /// <param name="errorHandler">CheckpointErrorHandler that is invoked when the checkpoint operation fails.</param>
        public void Checkpoint(Record record, CheckpointErrorHandler errorHandler = null)
        {
            Checkpoint(record.SequenceNumber, errorHandler);
        }
    }
}