//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//
using System.Runtime.Serialization;

namespace Amazon.Kinesis.ClientLibrary
{
    [DataContract]
    internal class ShardEndedAction : Action
    {

        public const string ACTION = "shardEnded";

        public ShardEndedAction()
        {
            Type = ACTION;
        }
        
        public override void Dispatch(IShardRecordProcessor processor, Checkpointer checkpointer)
        {
            processor.ShardEnded(new DefaultShardEndedInput(checkpointer));
        }
    }
}