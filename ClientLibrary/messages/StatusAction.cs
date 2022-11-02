//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
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