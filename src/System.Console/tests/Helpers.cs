// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

class Helpers
{
    public static void WriteAndReadHelper(MemoryStream memStream, Func<TextWriter> getHelper, Func<StreamReader, string> readHelper)
    {
        const string TestString = "Test";

        using (StreamWriter sw = new StreamWriter(memStream))
        {
            TextWriter newStream = getHelper();

            Assert.NotNull(newStream);

            newStream.Write(TestString);
            newStream.Flush();

            memStream.Seek(0, SeekOrigin.Begin);

            using (StreamReader sr = new StreamReader(memStream))
            {
                string fromConsole = readHelper(sr);

                Assert.Equal(TestString, fromConsole);
            }
        }
    }

    public static readonly SemaphoreSlim ConsoleSetStreamGate = new SemaphoreSlim(1, 1);

    public static void RunWithConsoleOut(TextWriter writer, Action<TextWriter> action)
        => RunWithConsoleOut(writer, _ => action((TextWriter)_), writer);

    public static void RunWithConsoleOut(TextWriter writer, Action action)
        => RunWithConsoleOut(writer, _ => action());

    public static void RunWithConsoleError(TextWriter writer, Action action)
        => RunWithConsoleError(writer, _ => action());

    public static void RunWithConsoleIn(TextReader reader, Action<TextReader> action)
        => RunWithConsoleIn(reader, _ => action((TextReader)_), reader);

    public static void RunWithConsoleIn(TextReader reader, Action action)
        => RunWithConsoleIn(reader, _ => action());

    private static void RunWithConsoleIn(TextReader reader, Action<object> action, object state = null)
    {
        ConsoleSetStreamGate.Wait();
        using (reader)
        {
            TextReader savedIn = Console.In;
            try
            {
                Console.SetIn(reader);
                action(state);
            }
            finally
            {
                Console.SetIn(savedIn);
                Helpers.ConsoleSetStreamGate.Release();
            }
        }
    }

    public static async Task RunWithConsoleOutAsync(TextWriter writer, Func<Task> action)
    {
        TextWriter savedOut = Console.Out;
        await ConsoleSetStreamGate.WaitAsync();
        try
        {
            Console.SetOut(writer);
            await action();
        }
        finally
        {
            Console.SetOut(savedOut);
            writer.Dispose();
            Helpers.ConsoleSetStreamGate.Release();
        }
    }

    private static void RunWithConsoleOut(TextWriter writer, Action<object> action, object state = null)
    {
        ConsoleSetStreamGate.Wait();
        using (writer)
        {
            TextWriter savedOut = Console.Out;
            try
            {
                Console.SetOut(writer);
                action(state);
            }
            finally
            {
                Console.SetOut(savedOut);
                Helpers.ConsoleSetStreamGate.Release();
            }
        }
    }

    private static void RunWithConsoleError(TextWriter writer, Action<object> action, object state = null)
    {
        ConsoleSetStreamGate.Wait();
        using (writer)
        {
            TextWriter savedError = Console.Error;
            try
            {
                Console.SetError(writer);
                action(state);
            }
            finally
            {
                Console.SetError(savedError);
                Helpers.ConsoleSetStreamGate.Release();
            }
        }
    }

    public static void RunInRedirectedError(Action<MemoryStream> command)
    {
        // Make sure that redirecting to a memory stream causes no special writing to the stream when using Console.CursorVisible
        MemoryStream data = new MemoryStream();
        var writer = new StreamWriter(data, new UTF8Encoding(false), 0x1000, leaveOpen: true) { AutoFlush = true };
        RunWithConsoleError(writer, o => command((MemoryStream)data), data);
    }

    public static void RunInRedirectedOutput(Action<MemoryStream> command)
    {
        // Make sure that redirecting to a memory stream causes no special writing to the stream when using Console.CursorVisible
        MemoryStream data = new MemoryStream();
        var writer = new StreamWriter(data, new UTF8Encoding(false), 0x1000, leaveOpen: true) { AutoFlush = true };
        RunWithConsoleOut(writer, o => command((MemoryStream)data), data);
    }

    public static void RunInNonRedirectedOutput(Action<MemoryStream> command)
    {
        // Make sure that when writing out to a UnixConsoleStream
        // written out.
        MemoryStream data = new MemoryStream();
        var writer = new InterceptStreamWriter(
                    Console.OpenStandardOutput(),
                    new StreamWriter(data, new UTF8Encoding(false), 0x1000, leaveOpen: true) { AutoFlush = true },
                    new UTF8Encoding(false), 0x1000, leaveOpen: true)
                { AutoFlush = true };
        RunWithConsoleOut(writer, o => command((MemoryStream)data), data);
    }
}
