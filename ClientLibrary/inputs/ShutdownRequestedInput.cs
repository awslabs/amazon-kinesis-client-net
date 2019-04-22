//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//
namespace Amazon.Kinesis.ClientLibrary
{
    public interface ShutdownRequestedInput
    {
        Checkpointer Checkpointer { get; }
    }
}