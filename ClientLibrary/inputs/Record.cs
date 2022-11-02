//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//
using System.Runtime.Serialization;

namespace Amazon.Kinesis.ClientLibrary
{
    /// <summary>
    /// A Kinesis record.
    /// </summary>
    [DataContract]
    public abstract class Record
    {
        /// <summary>
        /// Gets the binary data from this Kinesis record, already decoded from Base64.
        /// </summary>
        /// <value>The data in the Kinesis record.</value>
        public abstract byte[] Data { get; }

        /// <summary>
        /// Gets the sequence number of this Kinesis record.
        /// </summary>
        /// <value>The sequence number.</value>
        public abstract string SequenceNumber { get; }

        /// <summary>
        /// Get the subsequence number of this Kinesis record.  This is only set if the record is part of an aggregate
        /// record
        /// </summary>
        /// <value>The subsequence number.</value>
        public abstract long? SubSequenceNumber { get; }

        /// <summary>
        /// Gets the partition key of this Kinesis record.
        /// </summary>
        /// <value>The partition key.</value>
        public abstract string PartitionKey { get; }

        /// <summary>
        /// The approximate time that the record was inserted into the stream
        /// </summary>
        /// <value>server-side timestamp</value>
        public abstract double ApproximateArrivalTimestamp { get; }
    }
}