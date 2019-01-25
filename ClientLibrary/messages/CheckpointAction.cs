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