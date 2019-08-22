// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

public class SetError
{
    [Fact]
    public static void SetErrorThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => Helpers.RunWithConsoleError(null, () => {} ));
    }

    [Fact]
    public static void SetErrorRead()
    {
        Helpers.RunInRedirectedError(memStream =>
        {
            Helpers.WriteAndReadHelper(memStream, () => Console.Error, sr => sr.ReadLine());
        });
    }

    [Fact]
    public static void SetErrorReadToEnd()
    {
        Helpers.RunInRedirectedError(memStream =>
        {
            Helpers.WriteAndReadHelper(memStream, () => Console.Error, sr => sr.ReadToEnd());
        });
    }
}
