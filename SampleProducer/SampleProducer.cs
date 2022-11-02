/*
 * Copyright 2015 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.IO;
using System.Text;
using System.Threading;
using Amazon.Kinesis.Model;

/// <summary>
/// <para> Before running the code:
/// Fill in your AWS access credentials in the provided credentials file template,
/// and be sure to move the file to the default location under your home folder --
/// C:\users\username\.aws\credentials on Windows -- where the sample code will
/// load the credentials from.
/// https://console.aws.amazon.com/iam/home?#security_credential
/// </para>
/// <para>
/// WARNING:
/// To avoid accidental leakage of your credentials, DO NOT keep the credentials
/// file in your source directory.
/// </para>
/// </summary>

namespace Amazon.Kinesis.ClientLibrary.SampleProducer
{
    /// <summary>
    /// A sample producer of Kinesis records.
    /// </summary>
    class SampleRecordProducer
    {


        /// <summary>
        /// The AmazonKinesisClient instance used to establish a connection with AWS Kinesis,
        /// create a Kinesis stream, populate it with records, and (optionally) delete the stream.
        /// The SDK attempts to fetch credentials in the order described in:
        /// http://docs.aws.amazon.com/sdkfornet/latest/apidocs/items/MKinesis_KinesisClientctorNET4_5.html.
        /// You may also wish to change the RegionEndpoint.
        /// </summary>
        private static readonly AmazonKinesisClient kinesisClient = new AmazonKinesisClient(RegionEndpoint.USEast1);

        /// <summary>
        /// This method verifies your credentials, creates a Kinesis stream, waits for the stream
        /// to become active, then puts 10 records in it, and (optionally) deletes the stream.
        /// </summary>
        public static void Main(string[] args)
        {
            const string myStreamName = "kclnetsample";
            const int myStreamSize = 1;

            try
            {
                var createStreamRequest = new CreateStreamRequest();
                createStreamRequest.StreamName = myStreamName;
                createStreamRequest.ShardCount = myStreamSize;
                var createStreamReq = createStreamRequest;
                var CreateStreamResponse = kinesisClient.CreateStreamAsync(createStreamReq).Result;
                Console.Error.WriteLine("Created Stream : " + myStreamName);
            }
            catch (AggregateException ae)
            {
                ae.Handle((x) =>
                {
                    if (x is ResourceInUseException)
                    {
                        Console.Error.WriteLine("Producer is not creating stream " + myStreamName +
                        " to put records into as a stream of the same name already exists.");
                        return true;
                    }
                    return false; // Let anything else stop the application.
                });
            }

            WaitForStreamToBecomeAvailable(myStreamName);

            Console.Error.WriteLine("Putting records in stream : " + myStreamName);
            // Write 10 UTF-8 encoded records to the stream.
            for (int j = 0; j < 10; ++j)
            {
                PutRecordRequest requestRecord = new PutRecordRequest();
                requestRecord.StreamName = myStreamName;
                requestRecord.Data = new MemoryStream(Encoding.UTF8.GetBytes("testData-" + j));
                requestRecord.PartitionKey = "partitionKey-" + j;
                var putResultResponse = kinesisClient.PutRecordAsync(requestRecord).Result;
                Console.Error.WriteLine(
                    String.Format("Successfully putrecord {0}:\n\t partition key = {1,15}, shard ID = {2}",
                        j, requestRecord.PartitionKey, putResultResponse.ShardId));
            }

            // Uncomment the following if you wish to delete the stream here.
            //Console.Error.WriteLine("Deleting stream : " + myStreamName);
            //DeleteStreamRequest deleteStreamReq = new DeleteStreamRequest();
            //deleteStreamReq.StreamName = myStreamName;
            //try
            //{
            //    kinesisClient.DeleteStream(deleteStreamReq);
            //    Console.Error.WriteLine("Stream is now being deleted : " + myStreamName);
            //}
            //catch (ResourceNotFoundException ex)
            //
            //    Console.Error.WriteLine("Stream could not be found; " + ex);
            //}
            //catch (AmazonClientException ex)
            //{
            //    Console.Error.WriteLine("Error deleting stream; " + ex);
            //}
        }

        /// <summary>
        /// This method waits a maximum of 10 minutes for the specified stream to become active.
        /// <param name="myStreamName">Name of the stream whose active status is waited upon.</param>
        /// </summary>
        private static void WaitForStreamToBecomeAvailable(string myStreamName)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(10);
            while (DateTime.UtcNow < deadline)
            {
                DescribeStreamRequest describeStreamReq = new DescribeStreamRequest();
                describeStreamReq.StreamName = myStreamName;
                var describeResult = kinesisClient.DescribeStreamAsync(describeStreamReq).Result;
                string streamStatus = describeResult.StreamDescription.StreamStatus;
                Console.Error.WriteLine("  - current state: " + streamStatus);
                if (streamStatus == StreamStatus.ACTIVE)
                {
                    return;
                }
                Thread.Sleep(TimeSpan.FromSeconds(20));
            }

            throw new Exception("Stream " + myStreamName + " never went active.");
        }

    }
}