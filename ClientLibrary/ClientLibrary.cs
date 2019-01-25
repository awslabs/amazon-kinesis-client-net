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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Amazon.Kinesis.ClientLibrary
{
    #region Public

    /// <summary>
    /// Instances of this delegate can be passed to Checkpointer's Checkpoint methods. The delegate will be
    /// invoked when a checkpoint operation fails.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number at which the checkpoint was attempted.</param>
    /// <param name="errorMessage">The error message received from the checkpoint failure.</param>
    /// <param name="checkpointer">The Checkpointer instance that was used to perform the checkpoint operation.</param>
    public delegate void CheckpointErrorHandler(string sequenceNumber, string errorMessage, Checkpointer checkpointer);

    /// <summary>
    /// Provides a simple CheckpointErrorHandler that retries the checkpoint operation a number of times,
    /// with a fixed delay in between each attempt.
    /// </summary>
    public static class RetryingCheckpointErrorHandler
    {
        /// <summary>
        /// Create a simple CheckpointErrorHandler that retries the checkpoint operation a number of times,
        /// with a fixed delay in between each attempt.
        /// </summary>
        /// <param name="retries">Number of retries to perform before giving up.</param>
        /// <param name="delay">Delay between each retry.</param>
        public static CheckpointErrorHandler Create(int retries, TimeSpan delay)
        {
            return (seq, err, checkpointer) =>
            {
                if (retries > 0)
                {
                    Thread.Sleep(delay);
                    checkpointer.Checkpoint(seq, Create(retries - 1, delay));
                }
            };
        }
    }

    /// <summary>
    /// Instances of KclProcess communicate with the multi-lang daemon. The Main method of your application must
    /// create an instance of KclProcess and call its Run method.
    /// </summary>
    public abstract class KclProcess
    {
        /// <summary>
        /// Create an instance of KclProcess that uses the given IRecordProcessor to process records.
        /// </summary>
        /// <param name="recordProcessor">IRecordProcessor used to process records.</param>
        public static KclProcess Create(IRecordProcessor recordProcessor)
        {
            return Create(recordProcessor, new IoHandler());
        }

        public static KclProcess Create(IShardRecordProcessor recordProcessor)
        {
            return Create(recordProcessor, new IoHandler());
        }

        internal static KclProcess Create(IRecordProcessor recordProcessor, IoHandler ioHandler)
        {
            return new DefaultKclProcess(new ShardRecordProcessorToRecordProcessor(recordProcessor), ioHandler);
        }

        internal static KclProcess Create(IShardRecordProcessor recordProcessor, IoHandler ioHandler)
        {
            return new DefaultKclProcess(recordProcessor, ioHandler);
        }

        /// <summary>
        /// Starts the KclProcess. Once this method is called, the KclProcess instance will continuously communicate with
        /// the multi-lang daemon, performing actions as appropriate. This method blocks until it detects that the
        /// multi-lang daemon has terminated the communication.
        /// </summary>
        public abstract void Run();
    }

    #endregion

    internal class ShardRecordProcessorToRecordProcessor : IShardRecordProcessor
    {
        private IRecordProcessor RecordProcessor { get; set; }

        internal ShardRecordProcessorToRecordProcessor(IRecordProcessor recordProcessor)
        {
            RecordProcessor = recordProcessor;
        }

        public void Initialize(InitializationInput input)
        {
            RecordProcessor.Initialize(input);
        }

        public void ProcessRecords(ProcessRecordsInput input)
        {
            RecordProcessor.ProcessRecords(input);
        }

        public void LeaseLost(LeaseLossInput leaseLossInput)
        {
            RecordProcessor.Shutdown(new DefaultShutdownInput(ShutdownReason.ZOMBIE, null));
        }

        public void ShardEnded(ShardEndedInput shardEndedInput)
        {
            RecordProcessor.Shutdown(new DefaultShutdownInput(ShutdownReason.TERMINATE, shardEndedInput.Checkpointer));
        }

        public void ShutdownRequested(ShutdownRequestedInput shutdownRequestedInput)
        {
            //
            // Does nothing
            //
        }
    }

    internal class MalformedActionException : Exception
    {
        public MalformedActionException(string message)
            : base(message)
        {
        }

        public MalformedActionException(string message, Exception cause)
            : base(message, cause)
        {
        }
    }

    [DataContract]
    internal class DefaultRecord : Record
    {
        [DataMember(Name = "sequenceNumber")] private string _sequenceNumber;

        [DataMember(Name = "subSequenceNumber")]
        private long? _subSequenceNumber;

        [DataMember(Name = "data")] private string _base64;
        private byte[] _data;

        [DataMember(Name = "partitionKey")] private string _partitionKey;

        [DataMember(Name = "approximateArrivalTimestamp")]
        private double _approximateArrivalTimestamp;

        public override string PartitionKey => _partitionKey;

        public override double ApproximateArrivalTimestamp => _approximateArrivalTimestamp;

        public override string SequenceNumber => _sequenceNumber;
        public override long? SubSequenceNumber => _subSequenceNumber;

        public override byte[] Data
        {
            get
            {
                if (_data != null)
                {
                    return _data;
                }

                _data = Convert.FromBase64String(_base64);
                _base64 = null;
                return _data;
            }
        }

        public DefaultRecord(string sequenceNumber, string partitionKey, string data, long? subSequenceNumber = null,
            double approximateArrivalTimestamp = 0d)
        {
            _data = Encoding.UTF8.GetBytes(data);
            _sequenceNumber = sequenceNumber;
            _partitionKey = partitionKey;
            _subSequenceNumber = subSequenceNumber;
            _approximateArrivalTimestamp = approximateArrivalTimestamp;
        }
    }

    internal class DefaultProcessRecordsInput : ProcessRecordsInput
    {
        public List<Record> Records { get; }

        public Checkpointer Checkpointer { get; }

        public long? MillisBehindLatest { get; }

        public DefaultProcessRecordsInput(ProcessRecordsAction processRecordsAction, Checkpointer checkpointer)
        {
            Records = processRecordsAction.Records;
            MillisBehindLatest = processRecordsAction.MillisBehindLatest;
            Checkpointer = checkpointer;
        }
    }

    internal class DefaultInitializationInput : InitializationInput
    {
        public string ShardId { get; }
        public string SequenceNumber { get; }
        public long? SubSequenceNumber { get; }

        public DefaultInitializationInput(InitializeAction initializeAction)
        {
            ShardId = initializeAction.ShardId;
            SequenceNumber = initializeAction.SequenceNumber;
            SubSequenceNumber = initializeAction.SubSequenceNumber;
        }
    }

    internal class DefaultShutdownInput : ShutdownInput
    {
        public ShutdownReason Reason { get; }
        public Checkpointer Checkpointer { get; }

        public DefaultShutdownInput(ShutdownReason reason, Checkpointer checkpointer)
        {
            Reason = reason;
            Checkpointer = checkpointer;
        }
    }

    internal class DefaultLeaseLostInput : LeaseLossInput
    {
    }

    internal class DefaultShardEndedInput : ShardEndedInput
    {
        public Checkpointer Checkpointer { get; }

        public DefaultShardEndedInput(Checkpointer checkpointer)
        {
            Checkpointer = checkpointer;
        }
    }

    internal class DefaultShutdownRequestedInput : ShutdownRequestedInput
    {
        public Checkpointer Checkpointer { get; }

        public DefaultShutdownRequestedInput(Checkpointer checkpointer)
        {
            Checkpointer = checkpointer;
        }
    }

    internal class IoHandler : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly StreamWriter _outWriter;
        private readonly StreamWriter _errorWriter;

        public IoHandler()
            : this(Console.OpenStandardInput(), Console.OpenStandardOutput(), Console.OpenStandardError())
        {
        }

        public IoHandler(Stream inStream, Stream outStream, Stream errStream)
        {
            _reader = new StreamReader(inStream);
            _outWriter = new StreamWriter(outStream);
            _errorWriter = new StreamWriter(errStream);
        }

        public void WriteAction(Action a)
        {
            _outWriter.WriteLine(a.ToJson());
            _outWriter.Flush();
        }

        public Action ReadAction()
        {
            var s = _reader.ReadLine();
            return s == null ? null : Action.Parse(s);
        }

        public void WriteError(string message, Exception e)
        {
            _errorWriter.WriteLine(message);
            _errorWriter.WriteLine(e.StackTrace);
            _errorWriter.Flush();
        }

        public void Dispose()
        {
            _reader.Dispose();
            _outWriter.Dispose();
            _errorWriter.Dispose();
        }
    }

    internal class DefaultKclProcess : KclProcess
    {
        private class InternalCheckpointer : Checkpointer
        {
            private readonly DefaultKclProcess _kclProcess;

            public InternalCheckpointer(DefaultKclProcess kclProcess)
            {
                _kclProcess = kclProcess;
            }

            internal override void Checkpoint(string sequenceNumber, CheckpointErrorHandler errorHandler = null)
            {
                _kclProcess._iohandler.WriteAction(new CheckpointAction(sequenceNumber));
                var response = _kclProcess._iohandler.ReadAction();
                if (response is CheckpointAction checkpointResponse)
                {
                    if (!string.IsNullOrEmpty(checkpointResponse.Error))
                    {
                        errorHandler?.Invoke(sequenceNumber, checkpointResponse.Error, this);
                    }
                }
                else
                {
                    errorHandler?.Invoke(sequenceNumber, $"Unexpected response type {response.GetType().Name}", this);
                }
            }
        }

        private readonly IShardRecordProcessor _processor;
        private readonly IoHandler _iohandler;
        private readonly Checkpointer _checkpointer;

        internal DefaultKclProcess(IShardRecordProcessor processor, IoHandler iohandler)
        {
            _processor = processor;
            _iohandler = iohandler;
            _checkpointer = new InternalCheckpointer(this);
        }

        public override void Run()
        {
            while (ProcessNextLine())
            {
            }
        }

        private bool ProcessNextLine()
        {
            Action a = _iohandler.ReadAction();
            if (a != null)
            {
                DispatchAction(a);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void DispatchAction(Action action)
        {
            action.Dispatch(_processor, _checkpointer);
            _iohandler.WriteAction(new StatusAction(action.GetType()));
        }
    }
}