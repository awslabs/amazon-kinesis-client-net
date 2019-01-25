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
    /// Contextual information that can used to perform specialized shutdown procedures for this IRecordProcessor.
    /// </summary>
    public interface ShutdownInput {
        /// <summary>
        /// Gets the shutdown reason.
        /// </summary>
        /// <value>The shutdown reason.</value>
        ShutdownReason Reason { get; }

        /// <summary>
        /// Gets the checkpointer.
        /// </summary>
        /// <value>The checkpointer.</value>
        Checkpointer Checkpointer { get; }
    }
}