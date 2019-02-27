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

namespace Amazon.Kinesis.ClientLibrary
{
    /// <summary>
    /// Receives and processes Kinesis records. Each IRecordProcessor instance processes data from 1 and only 1 shard.
    /// </summary>
    public interface IRecordProcessor
    {
        /// <summary>
        /// Invoked by the Amazon Kinesis Client Library before data records are delivered to the IRecordProcessor
        /// instance (via processRecords).
        /// </summary>
        /// <param name="input">
        /// The InitializationInput containing information such as the shard id being assigned to this IRecordProcessor.
        /// </param>
        void Initialize(InitializationInput input);

        /// <summary>
        /// <para>
        /// Process data records. The Amazon Kinesis Client Library will invoke this method to deliver data records to the
        /// application.
        /// </para>
        /// <para>
        /// Upon fail over, the new instance will get records with sequence number > checkpoint position
        /// for each partition key.
        /// </para>
        /// </summary>
        /// <param name="input">
        /// ProcessRecordsInput that contains a batch of records, a Checkpointer, as well as relevant contextual information.
        /// </param> 
        void ProcessRecords(ProcessRecordsInput input);

        /// <summary>
        /// <para>
        /// Invoked by the Amazon Kinesis Client Library to indicate it will no longer send data records to this
        /// RecordProcessor instance.
        /// </para>
        /// <para>
        /// The reason parameter indicates:
        /// <list type="bullet">
        /// <item>
        /// <term>TERMINATE</term>
        /// <description>
        /// The shard has been closed and there will not be any more records to process. The
        /// record processor should checkpoint (after doing any housekeeping) to acknowledge that it has successfully
        /// completed processing all records in this shard.
        /// </description>>
        /// </item>
        /// <item>
        /// <term>ZOMBIE</term>
        /// <description>
        /// A fail over has occurred and a different record processor is (or will be) responsible
        /// for processing records.
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="input">
        /// ShutdownInput that contains the reason, a Checkpointer, as well as contextual information.
        /// </param>
        void Shutdown(ShutdownInput input);
    }
}