// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public abstract class Connect<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        public Connect(ITestOutputHelper output) : base(output) {}

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task Connect_Success(IPAddress listenAt)
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, listenAt, out port))
            {
                using (Socket client = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    Task connectTask = ConnectAsync(client, new IPEndPoint(listenAt, port));
                    await connectTask;
                    Assert.True(client.Connected);
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task Connect_MultipleIPAddresses_Success(IPAddress listenAt)
        {
            if (!SupportsMultiConnect)
                return;

            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, listenAt, out port))
            using (Socket client = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                Task connectTask = MultiConnectAsync(client, new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, port);
                await connectTask;
                Assert.True(client.Connected);
            }
        }

        [Fact]
        public async Task Connect_OnConnectedSocket_Fails()
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback, out port))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port));

                // In the sync case, we throw a derived exception here, so need to use ThrowsAnyAsync
                SocketException se = await Assert.ThrowsAnyAsync<SocketException>(() => ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port)));
                Assert.Equal(SocketError.IsConnected, se.SocketErrorCode);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Unix currently does not support Disconnect
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task Connect_AfterDisconnect_Fails()
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback, out port))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port));
                client.Disconnect(reuseSocket: false);

                if (ConnectAfterDisconnectResultsInInvalidOperationException)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port)));
                }
                else
                {
                    SocketException se = await Assert.ThrowsAsync<SocketException>(() => ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port)));
                    Assert.Equal(SocketError.IsConnected, se.SocketErrorCode);
                }
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)] // Not supported on OSX.
        public async Task ConnectGetsCanceledByDispose()
        {
            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the operation is started, we won't see a SocketException.

            SocketError? localSocketError = null;
            for (int i = 0; i < 10 && !localSocketError.HasValue; i++)
            {
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Task connectTask = Task.Run(async () =>
                {
                    await ConnectAsync(client, new IPEndPoint(IPAddress.Parse("1.1.1.1"), 23));
                });

                Task disposeTask = Task.Run(async () =>
                {
                    // Wait a little so the connect is started.
                    await Task.Delay(100);

                    client.Dispose();
                });

                Task timeoutTask = Task.Delay(30000);

                Assert.NotSame(timeoutTask, await Task.WhenAny(disposeTask, connectTask, timeoutTask));

                await disposeTask;

                try
                {
                    await connectTask;
                }
                catch (SocketException se)
                {
                    localSocketError = se.SocketErrorCode;
                }
                catch (ObjectDisposedException)
                {}
            }
            Assert.True(localSocketError.HasValue);
            if (UsesSync)
            {
                Assert.Equal(SocketError.Interrupted, localSocketError.Value);
            }
            else
            {
                Assert.Equal(SocketError.OperationAborted, localSocketError.Value);
            }
        }
    }

    public sealed class ConnectSync : Connect<SocketHelperArraySync>
    {
        public ConnectSync(ITestOutputHelper output) : base(output) {}
    }

    public sealed class ConnectSyncForceNonBlocking : Connect<SocketHelperSyncForceNonBlocking>
    {
        public ConnectSyncForceNonBlocking(ITestOutputHelper output) : base(output) {}
    }

    public sealed class ConnectApm : Connect<SocketHelperApm>
    {
        public ConnectApm(ITestOutputHelper output) : base(output) {}
    }

    public sealed class ConnectTask : Connect<SocketHelperTask>
    {
        public ConnectTask(ITestOutputHelper output) : base(output) {}
    }

    public sealed class ConnectEap : Connect<SocketHelperEap>
    {
        public ConnectEap(ITestOutputHelper output) : base(output) {}
    }
}
