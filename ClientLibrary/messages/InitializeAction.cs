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
using System.Runtime.Serialization;

namespace Amazon.Kinesis.ClientLibrary
{
    [DataContract]
    internal class InitializeAction : Action
    {
        public const string ACTION = "initialize";

        [DataMember(Name = "shardId")]
        public string ShardId { get; set; }
        [DataMember(Name = "sequenceNumber")]
        public string SequenceNumber { get; set; }
        [DataMember(Name = "subSequenceNumber")]
        public long? SubSequenceNumber { get; set; }

        public InitializeAction(string shardId)
        {
            Type = ACTION;
            ShardId = shardId;
        }

        public override void Dispatch(IShardRecordProcessor processor, Checkpointer checkpointer)
        {
            processor.Initialize(new DefaultInitializationInput(this));
        }
    }
}