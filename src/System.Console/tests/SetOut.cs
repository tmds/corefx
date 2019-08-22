// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

public class SetOut
{
    [Fact]
    public static void SetOutThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => Helpers.RunWithConsoleOut(null, () => {} ));
    }

    [Fact]
    public static void SetOutReadLine()
    {
        Helpers.RunInRedirectedOutput(memStream =>
        {
            Helpers.WriteAndReadHelper(memStream, () => Console.Out, sr => sr.ReadLine());
        });
    }

    [Fact]
    public static void SetOutReadToEnd()
    {
        Helpers.RunInRedirectedOutput(memStream =>
        {
            Helpers.WriteAndReadHelper(memStream, () => Console.Out, sr => sr.ReadToEnd());
        });
    }
}
