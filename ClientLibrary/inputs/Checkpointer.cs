//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
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