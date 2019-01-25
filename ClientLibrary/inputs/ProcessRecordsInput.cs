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