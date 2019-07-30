//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//
using System.Runtime.Serialization;

namespace Amazon.Kinesis.ClientLibrary
{   
    [DataContract]
    internal class ShutdownRequestedAction : Action
    {
        public const string ACTION = "shutdownRequested";

        public ShutdownRequestedAction()
        {
            Type = ACTION;
        }
        
        public override void Dispatch(IShardRecordProcessor processor, Checkpointer checkpointer)
        {
            processor.ShutdownRequested(new DefaultShutdownRequestedInput(checkpointer));
        }
    }
}