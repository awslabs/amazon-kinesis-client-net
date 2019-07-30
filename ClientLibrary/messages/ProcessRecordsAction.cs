//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
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