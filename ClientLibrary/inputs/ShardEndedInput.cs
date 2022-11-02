//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//
namespace Amazon.Kinesis.ClientLibrary
{
    /// <summary>
    ///     Indicates that the record processor has reached the end of the shard and now needs to checkpoint to complete the
    ///     shard.
    /// </summary>
    public interface ShardEndedInput
    {
        /// <summary>
        /// The Checkpointer that must be used to complete processing
        /// </summary>
        Checkpointer Checkpointer { get; }
    }
}