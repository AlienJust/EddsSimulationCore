using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace nMqtt
{
    /// <summary>
    /// Socket async event args pool
    /// </summary>
    internal sealed class SocketAsyncEventArgsPool
    {
        private readonly Stack<SocketAsyncEventArgs> _pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            _pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            lock (_pool)
            {
                _pool.Push(item);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            lock (_pool)
                return _pool.Pop();
        }

        public int Count => _pool.Count;
    }
}