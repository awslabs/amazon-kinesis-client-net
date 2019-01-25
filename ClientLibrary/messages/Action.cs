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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Amazon.Kinesis.ClientLibrary
{
    [DataContract]
    internal class Action
    {
        protected static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>()
        {
            { InitializeAction.ACTION, typeof(InitializeAction) },
            { ProcessRecordsAction.ACTION, typeof(ProcessRecordsAction) },
            { LeaseLostAction.ACTION, typeof(LeaseLostAction) },
            { ShardEndedAction.ACTION, typeof(ShardEndedAction) },
            { ShutdownRequestedAction.ACTION, typeof(ShutdownRequestedAction) },
            { CheckpointAction.ACTION, typeof(CheckpointAction) },
            { StatusAction.ACTION, typeof(StatusAction) }
        };

        public static Action Parse(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                try
                {
                    // Deserialize just the action field to get the type
                    var jsonSerializer = new DataContractJsonSerializer(typeof(Action));
                    var a = jsonSerializer.ReadObject(ms) as Action;
                    // Deserialize again into the appropriate type
                    ms.Position = 0;
                    jsonSerializer = new DataContractJsonSerializer(Types[a.Type]);
                    a = jsonSerializer.ReadObject(ms) as Action;
                    return a;
                }
                catch (Exception e)
                {
                    ms.Position = 0;
                    throw new MalformedActionException("Received an action which couldn't be understood: " + json, e);
                }
            }
        }

        [DataMember(Name = "action")]
        public string Type { get; set; }

        public virtual void Dispatch(IShardRecordProcessor processor, Checkpointer checkpointer)
        {
            throw new NotImplementedException("Actions need to implement Dispatch, this likely indicates a bug.");
        }

        public string ToJson()
        {
            var jsonSerializer = new DataContractJsonSerializer(GetType());
            var ms = new MemoryStream();
            jsonSerializer.WriteObject(ms, this);
            ms.Position = 0;
            using (var sr = new StreamReader(ms))
            {
                return sr.ReadLine();
            }
        }

        public override string ToString()
        {
            return ToJson();
        }
    }
}