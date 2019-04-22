//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
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