using System.Collections.Generic;
using System.Net.Sockets;

namespace nMqtt
{
    /// <summary>
    /// 缓存
    /// </summary>
    internal sealed class BufferManager
    {
        byte[] m_buffer;
        int m_currentIndex;
        readonly int m_numBytes;
        readonly int m_bufferSize;
        readonly Stack<int> m_freeIndexPool;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        /// <summary>
        /// Allocates buffer space used by the buffer pool
        /// </summary>
        public void ResetBuffer()
        {
            // create one big large buffer and divide that out to each SocketAsyncEventArg object
            m_buffer = new byte[m_numBytes];
        }

        /// <summary>
        /// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
        /// </summary>
        /// <returns>true if the buffer was successfully set, else false</returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                    return false;
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object.  This frees the buffer back to the 
        /// buffer pool
        /// </summary>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}