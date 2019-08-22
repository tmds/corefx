// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;


public class SyncTextWriter
{
    [Fact]
    public void SyncTextWriterLockedOnThis()
    {
        Helpers.RunWithConsoleOut(new CallbackTextWriter(),
        sw =>
        {
            CallbackTextWriter newWriter = sw as CallbackTextWriter;
            TextWriter syncWriter = Console.Out;

            newWriter.Callback = () =>
            {
                Assert.True(Monitor.IsEntered(syncWriter));
            };
            Console.Write("c");
            Assert.True(newWriter.WriteCharCalled);

            Console.Write("{0}", 32);
            Assert.True(newWriter.WriteFormatCalled);
        });
    }

    private sealed class CallbackTextWriter : TextWriter
    {
        internal Action Callback;

        public bool WriteCharCalled;
        public bool WriteFormatCalled;


        public override Encoding Encoding { get { return Encoding.UTF8; } }

        public override void Write(char value)
        {
            WriteCharCalled = true;
            Callback();
        }

        public override void Write(string format, object arg0)
        {
            WriteFormatCalled = true;
            Callback();
        }
    }
}
