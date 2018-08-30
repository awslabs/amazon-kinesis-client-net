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
using System.IO;
using NUnit.Framework;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.Kinesis.ClientLibrary
{
    [TestFixture]
    internal class KclTest
    {
        private Stream _input;
        private Stream _output;
        private Stream _error;
        private IoHandler _ioHandler;

        [SetUp]
        public void Init()
        {
            _input = new MemoryStream();
            _output = new MemoryStream();
            _error = new MemoryStream();
            _ioHandler = new IoHandler(_input, _output, _error);
        }

        /// <summary>
        /// Any uncaught exception from IRecordProcessor should be immediately fatal.
        /// Attempting to recover is very dangerous since the user's state is now
        /// potentially invalid, and sending the IRecordProcessor more records could
        /// lead to unexpected behavior from the user's perspective.
        /// 
        /// Any attempt to recover should be done by the user (rather than the
        /// framework), by catching exceptions rather than allowing them to bubble up.
        /// 
        /// This test checks that uncaught exeptions from the IRecordProcessor bubbles
        /// all the way up and is thrown by KCLProcess.Run(). It tests all 3 methods
        /// in IRecordProcessor (Initialize, ProcessRecords, Shutdown).
        /// </summary>
        [Test]
        public void CrashOnRecordProcessorException()
        {
            Action<IRecordProcessor> runTest = rp =>
            {
                _input.Position = 0;

                var kclProcess = KclProcess.Create(rp, new IoHandler(_input, _output, _error));

                try
                {
                    kclProcess.Run();
                    Assert.Fail("Should have seen the ClientException propagate up the call stack.");
                }
                catch (ClientException)
                {
                    // expected
                }
            };

            WriteActions(
                new InitializeAction("shardId-0"),
                new ProcessRecordsAction(new DefaultRecord("1", "a", "hello")),
                new ShutdownAction(ShutdownReason.ZOMBIE.ToString())
            );

            var recordProcessor = Substitute.For<IRecordProcessor>();

            Console.Error.WriteLine("Testing Shutdown");
            recordProcessor.Shutdown(Arg.Do<ShutdownInput>(x =>
                    {
                        throw new ClientException();
                    }));
            runTest.Invoke(recordProcessor);
                    
            Console.Error.WriteLine("Testing ProcessRecords");
            recordProcessor.ProcessRecords(Arg.Do<ProcessRecordsInput>(x =>
                    {
                        throw new ClientException();
                    }));
            runTest.Invoke(recordProcessor);

            Console.Error.WriteLine("Testing Initialize");
            recordProcessor.Initialize(Arg.Do<InitializationInput>(x =>
                    {
                        throw new ClientException();
                    }));
            runTest.Invoke(recordProcessor);
        }

        /// <summary>
        /// If the multilang daemon issues a command that causes us to go into an
        /// invalid state, it should be fatal. At that point it's not clear whether
        /// the daemon itself is in a valid state, and blindly following the
        /// commands could lead to undefined behavior.
        /// </summary>
        [Test]
        public void CrashOnInvalidActionException()
        {
            WriteActions(new CheckpointAction("456") { Error = "badstuff" });

            var kclProcess = KclProcess.Create(Substitute.For<IRecordProcessor>(), _ioHandler);

            try
            {
                kclProcess.Run();
                Assert.Fail("Should have thrown a MalformedActionException");
            }
            catch (MalformedActionException)
            {
                // all good
            }
        }

        /// <summary>
        /// If the multilang daemon writes out a message we can't understand, it
        /// should be fatal, for the same reasons as described in the previous
        /// test.
        /// </summary>
        [Test]
        public void CrashOnCorruptActionException()
        {
            WriteLines(_input, "gibberish");

            var kclProcess = KclProcess.Create(Substitute.For<IRecordProcessor>(), _ioHandler);

            try
            {
                kclProcess.Run();
                Assert.Fail("Should have thrown a MalformedActionException");
            }
            catch (MalformedActionException)
            {
                // all good
            }
        }

        /// <summary>
        /// We designed the Checkpoint method to not throw exceptions. The reason is
        /// that if it does, and the user doesn't handle it, it's going to bubble all
        /// the way up. At that point we have 2 options:
        ///     1) swallow the exception and continue processing
        ///     2) crash
        /// Option 1 is very dangerous because the user could have additional code
        /// after the Checkpoint call that she expects to be executed; throwing an
        /// exception during the Checkpoint call will terminate the user's function
        /// prematurely, skipping that code. This could then lead to undefined behavior
        /// for the user.
        /// 
        /// Option 2 is undesirable because a checkpoint failure is not inherently
        /// fatal, and it's actually ok to skip a checkpoint and go straight to the
        /// next one.
        /// 
        /// As a result, we decided to not throw any exceptions, and instead provide
        /// the option for the user to install a callback when a checkpoint error
        /// occurs.
        /// 
        /// This test tests that code after Checkpoint calls are executed even if the
        /// checkpoint operation fails.
        /// </summary>
        [Test]
        public void NonFatalCheckpointError()
        {
            var recordProcessor = Substitute.For<IRecordProcessor>();
            bool madeItPastCheckpoint = false;
            recordProcessor.ProcessRecords(Arg.Do<ProcessRecordsInput>(x => {
                x.Checkpointer.Checkpoint();
                madeItPastCheckpoint = true;
            }));

            WriteActions(
                new InitializeAction("shardId-0"),
                new ProcessRecordsAction(new DefaultRecord("1", "a", "hello")),
                new CheckpointAction("456") { Error = "badstuff" }
            );

            KclProcess.Create(recordProcessor, _ioHandler).Run();

            Assert.IsTrue(madeItPastCheckpoint,
                "Code after checkpoint should've executed even though checkpointing failed.");
        }

        /// <summary>
        /// Normal, error-free operation.
        /// </summary>
        [Test]
        public void HappyCase()
        {
            WriteActions(
                new InitializeAction("0"),
                new ProcessRecordsAction(new DefaultRecord("456", "cat", "bWVvdw==")),
                new CheckpointAction("456"),
                new ShutdownAction("TERMINATE"),
                new CheckpointAction("456")
            );

            TestRecordProcessor recordProcessor = new TestRecordProcessor
            {
                ProcessFunc = (input) => input.Checkpointer.Checkpoint(input.Records.Last()),
                ShutdownFunc = (input) => input.Checkpointer.Checkpoint()
            };

            KclProcess.Create(recordProcessor, _ioHandler).Run();
            List<Action> outputActions = ParseActionsFromOutput();

            dynamic a = outputActions[0];
            Assert.IsTrue(a is StatusAction);
            Assert.IsTrue(a.ResponseFor == "initialize");


            a = outputActions[1];
            Assert.IsTrue(a is CheckpointAction);
            Assert.IsTrue(a.SequenceNumber == "456" && a.Error == null);


            a = outputActions[2];
            Assert.IsTrue(a is StatusAction);
            Assert.IsTrue(a.ResponseFor == "processRecords");


            a = outputActions[3];
            Assert.IsTrue(a is CheckpointAction);
            Assert.IsTrue(a.SequenceNumber == null && a.Error == null);
           

            a = outputActions[4];
            Assert.IsTrue(a is StatusAction);
            Assert.IsTrue(a.ResponseFor == "shutdown");
        }

        /// <summary>
        /// Test that the user is able to use a callback to handle checkpoint
        /// errors.
        /// </summary>
        [Test]
        public void CheckpointErrorHandling()
        {
            WriteActions(
                new InitializeAction("0"),
                new ProcessRecordsAction(new DefaultRecord("456", "cat", "bWVvdw==")),
                new CheckpointAction(null) { Error = "oh noes" },
                new ShutdownAction("TERMINATE")
            );

            bool handlerCodeCalled = false;
            CheckpointErrorHandler handler = (sequenceNumber, errorMsg, checkpointer) =>
            {
                handlerCodeCalled = true;
            };

            TestRecordProcessor recordProcessor = new TestRecordProcessor
            {
                ProcessFunc = (input) => input.Checkpointer.Checkpoint(input.Records.Last(), handler)
            };

            KclProcess.Create(recordProcessor, _ioHandler).Run();

            Assert.IsTrue(handlerCodeCalled);
        }

        /// <summary>
        /// Test that RetryingCheckpointErrorHandler is able to retry checkpoints.
        /// </summary>
        [Test]
        public void RetryHandler()
        {
            WriteActions(
                new InitializeAction("0"),
                new ProcessRecordsAction(new DefaultRecord("456", "cat", "bWVvdw==")),
                new CheckpointAction(null) { Error = "oh noes" },
                new CheckpointAction(null) { Error = "oh noes" },
                new CheckpointAction(null) { Error = "oh noes" },
                new CheckpointAction(null) { Error = "oh noes" },
                new ShutdownAction(ShutdownReason.TERMINATE.ToString()),
                new CheckpointAction(null) { Error = "oh noes" },
                new CheckpointAction(null) { Error = "oh noes" },
                new CheckpointAction(null) { Error = "oh noes" },
                new CheckpointAction(null) { Error = "oh noes" }
            );

            const int numRetries = 3;
            TestRecordProcessor recordProcessor = new TestRecordProcessor
            {
                ProcessFunc = (input) => input.Checkpointer.Checkpoint(input.Records.Last(),
                        RetryingCheckpointErrorHandler.Create(numRetries, TimeSpan.Zero)),
                ShutdownFunc = (input) => input.Checkpointer.Checkpoint(
                        RetryingCheckpointErrorHandler.Create(numRetries, TimeSpan.Zero))
            };

            KclProcess.Create(recordProcessor, _ioHandler).Run();
            List<Action> outputActions = ParseActionsFromOutput();

            Console.Error.WriteLine(String.Join("\n", outputActions.Select(x => x.ToString()).ToList()));

            int i = 0;
            dynamic a = outputActions[i++];
            Assert.IsTrue(a is StatusAction, "Action " + (i - 1) + " should be StatusAction");
            Assert.AreEqual("initialize", a.ResponseFor);

            for (int j = 0; j <= numRetries; j++)
            {
                a = outputActions[i++];
                Assert.IsTrue(a is CheckpointAction, "Action " + (i - 1) + " should be CheckpointAction");
                Assert.AreEqual("456", a.SequenceNumber);
                Assert.IsNull(a.Error);
            }

            a = outputActions[i++];
            Assert.IsTrue(a is StatusAction, "Action " + (i - 1) + " should be StatusAction");
            Assert.AreEqual("processRecords", a.ResponseFor);

            for (int j = 0; j <= numRetries; j++)
            {
                a = outputActions[i++];
                Assert.IsTrue(a is CheckpointAction, "Action " + (i - 1) + " should be CheckpointAction");
                Assert.IsNull(a.SequenceNumber);
                Assert.IsNull(a.Error);
            }

            a = outputActions[i++];
            Assert.IsTrue(a is StatusAction, "Action " + (i - 1) + " should be StatusAction");
            Assert.AreEqual("shutdown", a.ResponseFor);

        }

        private class TestRecordProcessor : IRecordProcessor
        {
            private Action<InitializationInput> InitFunc { get; set; }
            internal Action<ProcessRecordsInput> ProcessFunc { private get; set; }
            internal Action<ShutdownInput> ShutdownFunc { private get; set; }

            public void Initialize(InitializationInput input)
            {
                if (InitFunc != null)
                {
                    InitFunc.Invoke(input);
                }

            }

            public void ProcessRecords(ProcessRecordsInput input)
            {
                if (ProcessFunc != null)
                {
                    ProcessFunc.Invoke(input);
                }
            }

            public void Shutdown(ShutdownInput input)
            {
                if (ShutdownFunc != null)
                {
                    ShutdownFunc.Invoke(input);
                }
            }
        }

        private static void WriteLines(Stream stream, params string[] content)
        {
            var position = stream.Position;
            var writer = new StreamWriter(stream);
            foreach (string s in content)
            {
                writer.WriteLine(s);
            }
            writer.Flush();
            stream.Position = position;
        }

        private void WriteActions(params Action[] actions)
        {
            WriteLines(_input, actions.ToArray().Select(x => x.ToString()).ToArray());
        }

        private List<Action> ParseActionsFromOutput()
        {
            _output.Position = 0;
            return new StreamReader(_output, System.Text.Encoding.UTF8)
                .ReadToEnd()
                .Split('\n')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Select(Action.Parse)
                .ToList();
        }

        private class ClientException : Exception
        {

        }
    }
}
