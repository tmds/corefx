// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class SetIn
{
    [Fact]
    public static void SetInThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => Helpers.RunWithConsoleIn(null, () => {} ));
    }

    [Fact]
    public static void SetInReadLine()
    {
        const string TextStringFormat = "Test {0}";

        MemoryStream memStream = new MemoryStream();
        StreamWriter sw = new StreamWriter(memStream);
        for (int i = 0; i < 20; i++)
        {
            sw.WriteLine(string.Format(TextStringFormat, i));
        }
        sw.Flush();
        memStream.Seek(0, SeekOrigin.Begin);

        Helpers.RunWithConsoleIn(new StreamReader(memStream),
        () =>
        {
            Assert.NotNull(Console.In);

            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(string.Format(TextStringFormat, i), Console.ReadLine());
            }
        });
    }
}
