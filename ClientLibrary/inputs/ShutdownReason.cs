//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//
namespace Amazon.Kinesis.ClientLibrary
{
    /// <summary>
    /// Reason the RecordProcessor is being shutdown.
    /// Used to distinguish between a fail-over vs. a termination (shard is closed and all records have been delivered).
    /// In case of a fail over, applications should NOT checkpoint as part of shutdown,
    /// since another record processor may have already started processing records for that shard.
    /// In case of termination (resharding use case), applications SHOULD checkpoint their progress to indicate
    /// that they have successfully processed all the records (processing of child shards can then begin).
    /// </summary>
    public enum ShutdownReason {
        /// <summary>
        /// Processing will be moved to a different record processor (fail over, load balancing use cases).
        /// Applications SHOULD NOT checkpoint their progress (as another record processor may have already started
        /// processing data).
        /// </summary>
        ZOMBIE,

        /// <summary>
        /// Terminate processing for this RecordProcessor (resharding use case).
        /// Indicates that the shard is closed and all records from the shard have been delivered to the application.
        /// Applications SHOULD checkpoint their progress to indicate that they have successfully processed all records
        /// from this shard and processing of child shards can be started.
        /// </summary>
        TERMINATE
    }
}