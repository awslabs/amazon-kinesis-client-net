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