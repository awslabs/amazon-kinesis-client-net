/*
 * Copyright 2015 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Amazon Software License (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/asl/
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Stateless;
using System.Threading;

namespace Amazon.Kinesis.ClientLibrary
{
    #region Public

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
        /// Gets the partition key of this Kinesis record.
        /// </summary>
        /// <value>The partition key.</value>
        public abstract string PartitionKey { get; }
    }

    /// <summary>
    /// Used by IRecordProcessors when they want to checkpoint their progress.
    /// The Amazon Kinesis Client Library will pass an object implementing this interface to IRecordProcessors,
    /// so they can checkpoint their progress.
    /// </summary>
    public abstract class Checkpointer
    {
        internal abstract void Checkpoint(string sequenceNumber, CheckpointErrorHandler errorHandler = null);

        /// <summary>
        /// <para>
        /// This method will checkpoint the progress at the last data record that was delivered to the record processor.
        /// </para>
        /// <para>
        /// Upon failover (after a successful checkpoint() call), the new/replacement IRecordProcessor instance
        /// will receive data records whose sequenceNumber > checkpoint position (for each partition key).
        /// </para>
        /// <para>
        /// In steady state, applications should checkpoint periodically (e.g. once every 5 minutes).
        /// </para>
        /// <para>
        /// Calling this API too frequently can slow down the application (because it puts pressure on the underlying
        /// checkpoint storage layer).
        /// </para>
        /// <para>
        /// You may optionally pass a CheckpointErrorHandler to the method, which will be invoked when the
        /// checkpoint operation fails. 
        /// </para>
        /// </summary>
        /// <param name="errorHandler">CheckpointErrorHandler that is invoked when the checkpoint operation fails.</param>
        public void Checkpoint(CheckpointErrorHandler errorHandler = null)
        {
            Checkpoint(null as string, errorHandler);
        }

        /// <summary>
        /// <para>
        /// This method will checkpoint the progress at the sequence number of the specified record.
        /// </para>
        /// <para>
        /// Upon failover (after a successful checkpoint() call), the new/replacement IRecordProcessor instance
        /// will receive data records whose sequenceNumber > checkpoint position (for each partition key).
        /// </para>
        /// <para>
        /// In steady state, applications should checkpoint periodically (e.g. once every 5 minutes).
        /// </para>
        /// <para>
        /// Calling this API too frequently can slow down the application (because it puts pressure on the underlying
        /// checkpoint storage layer).
        /// </para>
        /// <para>
        /// You may optionally pass a CheckpointErrorHandler to the method, which will be invoked when the
        /// checkpoint operation fails. 
        /// </para>
        /// </summary>
        /// <param name="record">Record whose sequence number to checkpoint at.</param>
        /// <param name="errorHandler">CheckpointErrorHandler that is invoked when the checkpoint operation fails.</param>
        public void Checkpoint(Record record, CheckpointErrorHandler errorHandler = null)
        {
            Checkpoint(record.SequenceNumber, errorHandler);
        }
    }

    /// <summary>
    /// Contains a batch of records to be processed, along with contextual information.
    /// </summary>
    public interface ProcessRecordsInput
    {
        /// <summary>
        /// Get the records to be processed.
        /// </summary>
        /// <value>The records.</value>
        List<Record> Records { get; }

        /// <summary>
        /// Gets the checkpointer.
        /// </summary>
        /// <value>The checkpointer.</value>
        Checkpointer Checkpointer { get; }
    }

    /// <summary>
    /// Contextual information that can used to perform specialized initialization for this IRecordProcessor.
    /// </summary>
    public interface InitializationInput
    {
        /// <summary>
        /// Gets the shard identifier.
        /// </summary>
        /// <value>The shard identifier. Each IRecordProcessor processes records from one and only one shard.</value>
        string ShardId { get; }
    }

    /// <summary>
    /// Contextual information that can used to perform specialized shutdown procedures for this IRecordProcessor.
    /// </summary>
    public interface ShutdownInput {
        /// <summary>
        /// Gets the shutdown reason.
        /// </summary>
        /// <value>The shutdown reason.</value>
        ShutdownReason Reason { get; }

        /// <summary>
        /// Gets the checkpointer.
        /// </summary>
        /// <value>The checkpointer.</value>
        Checkpointer Checkpointer { get; }
    }

    /// <summary>
    /// Reason the RecordProcessor is being shutdown.
    /// Used to distinguish between a fail-over vs. a termination (shard is closed and all records have been delivered).
    /// In case of a fail over, applications should NOT checkpoint as part of shutdown,
    /// since another record processor may have already started processing records for that shard.
    /// In case of termination (resharding use case), applications SHOULD checkpoint their progress to indicate
    /// that they have successfully processed all the records (processing of child shards can then begin).
    /// </summary>
    public enum ShutdownReason {
        /// <summary>
        /// Processing will be moved to a different record processor (fail over, load balancing use cases).
        /// Applications SHOULD NOT checkpoint their progress (as another record processor may have already started
        /// processing data).
        /// </summary>
        ZOMBIE,

        /// <summary>
        /// Terminate processing for this RecordProcessor (resharding use case).
        /// Indicates that the shard is closed and all records from the shard have been delivered to the application.
        /// Applications SHOULD checkpoint their progress to indicate that they have successfully processed all records
        /// from this shard and processing of child shards can be started.
        /// </summary>
        TERMINATE
    }

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

        internal static KclProcess Create(IRecordProcessor recordProcessor, IoHandler ioHandler)
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
    internal class Action
    {
        protected static readonly Dictionary<String, Type> Types = new Dictionary<String, Type>()
        {
            { "initialize", typeof(InitializeAction) },
            { "processRecords", typeof(ProcessRecordsAction) },
            { "shutdown", typeof(ShutdownAction) },
            { "checkpoint", typeof(CheckpointAction) },
            { "status", typeof(StatusAction) }
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

        public override string ToString()
        {
            var jsonSerializer = new DataContractJsonSerializer(GetType());
            var ms = new MemoryStream();
            jsonSerializer.WriteObject(ms, this);
            ms.Position = 0;
            using (StreamReader sr = new StreamReader(ms))
            {
                return sr.ReadLine();
            }
        }
    }

    [DataContract]
    internal class InitializeAction : Action
    {
        [DataMember(Name = "shardId")]
        public string ShardId { get; set; }

        public InitializeAction(string shardId)
        {
            Type = "initialize";
            ShardId = shardId;
        }
    }

    [DataContract]
    internal class ProcessRecordsAction : Action
    {
        [DataMember(Name = "records")]
        private List<DefaultRecord> _actualRecords;

        public List<Record> Records
        {
            get
            {
                return _actualRecords.Select(x => x as Record).ToList();
            }
        }

        public ProcessRecordsAction(params DefaultRecord[] records)
        {
            Type = "processRecords";
            _actualRecords = records.ToArray().ToList();
        }
    }

    [DataContract]
    internal class ShutdownAction : Action
    {
        [DataMember(Name = "reason")]
        public string Reason { get; set; }

        public ShutdownAction(string reason)
        {
            Type = "shutdown";
            Reason = reason;
        }
    }

    [DataContract]
    internal class CheckpointAction : Action
    {
        [DataMember(Name = "sequenceNumber")]
        public string SequenceNumber { get; set; }

        [DataMember(Name = "error", IsRequired = false)]
        public string Error { get; set; }

        public CheckpointAction(string sequenceNumber)
        {
            Type = "checkpoint";
            SequenceNumber = sequenceNumber;
        }
    }

    [DataContract]
    internal class StatusAction : Action
    {
        [DataMember(Name = "responseFor")]
        public string ResponseFor { get; set; }

        public StatusAction(Type t)
            : this(Types.Where(x => x.Value == t).Select(x => x.Key).First())
        {
        }

        public StatusAction(string type)
        {
            Type = "status";
            ResponseFor = type;
        }
    }

    [DataContract]
    internal class DefaultRecord : Record
    {
        [DataMember(Name = "sequenceNumber")]
        private string _sequenceNumber;

        [DataMember(Name = "data")]
        private string _base64;
        private byte[] _data;

        [DataMember(Name = "partitionKey")]
        private string _partitionKey;

        public override string PartitionKey { get { return _partitionKey; } }

        public override byte[] Data
        {
            get
            {
                if (_data == null)
                {
                    _data = Convert.FromBase64String(_base64);
                    _base64 = null;
                }
                return _data;
            }
        }

        public override string SequenceNumber { get { return _sequenceNumber; } }

        public DefaultRecord(string sequenceNumber, string partitionKey, string data)
        {
            _data = Encoding.UTF8.GetBytes(data);
            _sequenceNumber = sequenceNumber;
            _partitionKey = partitionKey;
        }
    }

    internal class DefaultProcessRecordsInput : ProcessRecordsInput
    {
        private readonly List<Record> _records;
        private readonly Checkpointer _checkpointer;

        public List<Record> Records { get { return _records; } }
        public Checkpointer Checkpointer { get { return _checkpointer; } }

        public DefaultProcessRecordsInput(List<Record> records, Checkpointer checkpointer) {
            _records = records;
            _checkpointer = checkpointer;
        }
    }

    internal class DefaultInitializationInput : InitializationInput
    {
        private readonly string _shardId;

        public string ShardId { get { return _shardId; } }

        public DefaultInitializationInput(string shardId)
        {
            _shardId = shardId;
        }
    }

    internal class DefaultShutdownInput : ShutdownInput
    {
        private readonly ShutdownReason _reason;
        private readonly Checkpointer _checkpointer;

        public ShutdownReason Reason { get { return _reason; } }
        public Checkpointer Checkpointer { get { return _checkpointer; } }

        public DefaultShutdownInput(ShutdownReason reason, Checkpointer checkpointer)
        {
            _reason = reason;
            _checkpointer = checkpointer;
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
            _outWriter.WriteLine(a.ToString());
            _outWriter.Flush();
        }

        public Action ReadAction()
        {
            string s = _reader.ReadLine();
            if (s == null)
            {
                return null;
            }
            return Action.Parse(s);
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
        private enum State
        {
            Start,
            Ready,
            Checkpointing,
            FinalCheckpointing,
            ShuttingDown,
            End,
            Processing,
            Initializing
        }

        private enum Trigger
        {
            BeginCheckpoint,
            BeginInitialize,
            BeginProcessRecords,
            BeginShutdown,
            FinishCheckpoint,
            FinishInitialize,
            FinishProcessRecords,
            FinishShutdown
        }

        private class InternalCheckpointer : Checkpointer
        {
            private readonly DefaultKclProcess _p;

            public InternalCheckpointer(DefaultKclProcess p)
            {
                _p = p;
            }

            internal override void Checkpoint(string sequenceNumber, CheckpointErrorHandler errorHandler = null)
            {
                _p.CheckpointSeqNo = sequenceNumber;
                _p._stateMachine.Fire(Trigger.BeginCheckpoint);
                if (_p.CheckpointError != null && errorHandler != null)
                {
                    errorHandler.Invoke(sequenceNumber, _p.CheckpointError, this);
                }
            }
        }

        private readonly StateMachine<State, Trigger> _stateMachine = new StateMachine<State, Trigger>(State.Start);
       
        private readonly IRecordProcessor _processor;
        private readonly IoHandler _iohandler;
        private readonly Checkpointer _checkpointer;

        private string CheckpointSeqNo { get; set; }

        private string CheckpointError { get; set; }

        private string ShardId { get; set; }

        private ShutdownReason ShutdownReason { get; set; }

        private List<Record> Records { get; set; }

        internal DefaultKclProcess(IRecordProcessor processor, IoHandler iohandler)
        {
            _processor = processor;
            _iohandler = iohandler;
            _checkpointer = new InternalCheckpointer(this);
            ConfigureStateMachine();
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

        private void DispatchAction(Action a)
        {
            var initAction = a as InitializeAction;
            if (initAction != null)
            {
                ShardId = initAction.ShardId;
                _stateMachine.Fire(Trigger.BeginInitialize);
                return;
            }

            var processRecordAction = a as ProcessRecordsAction;
            if (processRecordAction != null)
            {
                Records = processRecordAction.Records;
                _stateMachine.Fire(Trigger.BeginProcessRecords);
                return;
            }

            var shutdownAction = a as ShutdownAction;
            if (shutdownAction != null)
            {
                ShutdownReason = (ShutdownReason) Enum.Parse(typeof(ShutdownReason), shutdownAction.Reason);
                _stateMachine.Fire(Trigger.BeginShutdown);
                return;
            }

            var checkpointAction = a as CheckpointAction;
            if (checkpointAction != null)
            {
                CheckpointError = checkpointAction.Error;
                CheckpointSeqNo = checkpointAction.SequenceNumber;
                _stateMachine.Fire(Trigger.FinishCheckpoint);
                return;
            }

            throw new MalformedActionException("Received an action which couldn't be understood: " + a.Type);       
        }

        private void ConfigureStateMachine()
        {
            _stateMachine.OnUnhandledTrigger((state, trigger) =>
                {
                    throw new MalformedActionException("trigger " + trigger + " is invalid for state " + state);
                });

            // Uncomment to help debugging
            // _stateMachine.OnTransitioned(t => Console.Error.WriteLine(t.Source + " --> " + t.Destination));

            _stateMachine.Configure(State.Start)
                .Permit(Trigger.BeginInitialize, State.Initializing);

            _stateMachine.Configure(State.Ready)
                .OnEntryFrom(Trigger.FinishProcessRecords, FinishProcessRecords)
                .OnEntryFrom(Trigger.FinishInitialize, FinishInitialize)
                .Permit(Trigger.BeginProcessRecords, State.Processing)
                .Permit(Trigger.BeginShutdown, State.ShuttingDown);

            _stateMachine.Configure(State.Checkpointing)
                .OnEntryFrom(Trigger.BeginCheckpoint, BeginCheckpoint)
                .Permit(Trigger.FinishCheckpoint, State.Processing);

            _stateMachine.Configure(State.FinalCheckpointing)
                .OnEntryFrom(Trigger.BeginCheckpoint, BeginCheckpoint)
                .Permit(Trigger.FinishCheckpoint, State.ShuttingDown);

            _stateMachine.Configure(State.ShuttingDown)
                .OnEntryFrom(Trigger.BeginShutdown, BeginShutdown)
                .OnEntryFrom(Trigger.FinishCheckpoint, FinishCheckpoint)
                .Permit(Trigger.FinishShutdown, State.End)
                .Permit(Trigger.BeginCheckpoint, State.FinalCheckpointing);

            _stateMachine.Configure(State.End)
                .OnEntryFrom(Trigger.FinishShutdown, FinishShutdown);

            _stateMachine.Configure(State.Processing)
                .OnEntryFrom(Trigger.BeginProcessRecords, BeginProcessRecords)
                .OnEntryFrom(Trigger.FinishCheckpoint, FinishCheckpoint)
                .Permit(Trigger.BeginCheckpoint, State.Checkpointing)
                .Permit(Trigger.FinishProcessRecords, State.Ready);

            _stateMachine.Configure(State.Initializing)
                .OnEntryFrom(Trigger.BeginInitialize, BeginInitialize)
                .Permit(Trigger.FinishInitialize, State.Ready);
        }

        private void BeginInitialize()
        {
            _processor.Initialize(new DefaultInitializationInput(ShardId));
            _stateMachine.Fire(Trigger.FinishInitialize);
        }

        private void FinishInitialize()
        {
            _iohandler.WriteAction(new StatusAction(typeof(InitializeAction)));
        }

        private void BeginShutdown()
        {
            _processor.Shutdown(new DefaultShutdownInput(ShutdownReason, _checkpointer));
            _stateMachine.Fire(Trigger.FinishShutdown);
        }

        private void FinishShutdown()
        {
            _iohandler.WriteAction(new StatusAction(typeof(ShutdownAction)));
        }

        private void BeginProcessRecords()
        {
            _processor.ProcessRecords(new DefaultProcessRecordsInput(Records, _checkpointer));
            _stateMachine.Fire(Trigger.FinishProcessRecords);
        }

        private void FinishProcessRecords()
        {
            _iohandler.WriteAction(new StatusAction(typeof(ProcessRecordsAction)));
        }

        private void BeginCheckpoint()
        {
            _iohandler.WriteAction(new CheckpointAction(CheckpointSeqNo));
            ProcessNextLine();
        }

        private void FinishCheckpoint()
        {
            // nothing to do here
        }
    }
}
