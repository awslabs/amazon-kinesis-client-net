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
using System.Linq;
using System.Runtime.Serialization;

namespace Amazon.Kinesis.ClientLibrary
{
    [DataContract]
    internal class ProcessRecordsAction : Action
    {

        public const string ACTION = "processRecords";
        
        [DataMember(Name = "records")]
        private List<DefaultRecord> _actualRecords;

        [DataMember(Name = "millisBehindLatest")]
        public long? MillisBehindLatest { get; set; }

        public List<Record> Records
        {
            get
            {
                return _actualRecords.Select(x => x as Record).ToList();
            }
        }

        public ProcessRecordsAction(params DefaultRecord[] records)
        {
            Type = ACTION;
            _actualRecords = records.ToList();
        }

        public override void Dispatch(IShardRecordProcessor processor, Checkpointer checkpointer)
        {
            processor.ProcessRecords(new DefaultProcessRecordsInput(this, checkpointer));
        }
    }
}