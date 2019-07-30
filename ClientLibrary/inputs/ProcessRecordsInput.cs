//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//
using System.Collections.Generic;

namespace Amazon.Kinesis.ClientLibrary
{
    /// <summary>
    /// Contains a batch of records to be processed, along with contextual information.
    /// </summary>
    public interface ProcessRecordsInput
    {
        /// <summary>
        /// Get the records to be processed.
        /// </summary>
        /// <value>The records.</value>
        List<Record> Records { get; }

        /// <summary>
        /// Gets the checkpointer.
        /// </summary>
        /// <value>The checkpointer.</value>
        Checkpointer Checkpointer { get; }
        
        /// <summary>
        /// Indicates how far behind the current time this entry is in milliseconds
        /// </summary>
        long? MillisBehindLatest { get; }
    }
}