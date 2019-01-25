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
using System.Linq;
using System.Runtime.Serialization;

namespace Amazon.Kinesis.ClientLibrary
{
    [DataContract]
    internal class StatusAction : Action
    {
        public const string ACTION = "status";
        
        [DataMember(Name = "responseFor")]
        public string ResponseFor { get; set; }

        public StatusAction(Type t)
            : this(Types.Where(x => x.Value == t).Select(x => x.Key).First())
        {
        }

        public StatusAction(string type)
        {
            Type = ACTION;
            ResponseFor = type;
        }
    }
}