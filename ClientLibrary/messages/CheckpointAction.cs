//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//

using System;
using System.Runtime.Serialization;

namespace Amazon.Kinesis.ClientLibrary
{
    [DataContract]
    internal class CheckpointAction : Action
    {

        public const string ACTION = "checkpoint";
        
        [DataMember(Name = "sequenceNumber")]
        public string SequenceNumber { get; set; }

        [DataMember(Name = "error", IsRequired = false)]
        public string Error { get; set; }

        public CheckpointAction(string sequenceNumber)
        {
            Type = ACTION;
            SequenceNumber = sequenceNumber;
        }

        public override void Dispatch(IShardRecordProcessor processor, Checkpointer checkpointer)
        {
            throw new  NotSupportedException("CheckpointAction should never be dispatched, but handled in line");
        }
    }
}