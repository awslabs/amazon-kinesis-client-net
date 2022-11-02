//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//
namespace Amazon.Kinesis.ClientLibrary
{
    /// <summary>
    /// Contextual information that can used to perform specialized initialization for this IRecordProcessor.
    /// </summary>
    public interface InitializationInput
    {
        /// <summary>
        /// Gets the shard identifier.
        /// </summary>
        /// <value>The shard identifier. Each IRecordProcessor processes records from one and only one shard.</value>
        string ShardId { get; }

        /// <summary>
        /// The sequence number that this shard was last checkpointed at. This maybe null if the shard has never been
        /// checkpointed.
        /// </summary>
        string SequenceNumber { get; }

        /// <summary>
        /// The subsequence number that the shard was last checkpoint at.  This may be null if the shard has never been
        /// checkpointed or was not checkpointed on a deaggregated record.
        /// </summary>
        long? SubSequenceNumber { get; }
    }
}