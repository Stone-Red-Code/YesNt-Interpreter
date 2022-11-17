using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace YesNt.Interpreter.Utilities
{
    internal delegate void UserCallBack(string data);

    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

    public class FixedProcess : Process
    {
        internal AsyncStreamReader output;
        internal AsyncStreamReader error;

        public new event DataReceivedEventHandler OutputDataReceived;

        public new event DataReceivedEventHandler ErrorDataReceived;

        public new void BeginOutputReadLine()
        {
            Stream baseStream = StandardOutput.BaseStream;
            output = new AsyncStreamReader(baseStream, new UserCallBack(FixedOutputReadNotifyUser), StandardOutput.CurrentEncoding);
            output.BeginReadLine();
        }

        public new void BeginErrorReadLine()
        {
            Stream baseStream = StandardError.BaseStream;
            error = new AsyncStreamReader(baseStream, new UserCallBack(FixedErrorReadNotifyUser), StandardError.CurrentEncoding);
            error.BeginReadLine();
        }

        internal void FixedOutputReadNotifyUser(string data)
        {
            DataReceivedEventHandler outputDataReceived = OutputDataReceived;
            if (outputDataReceived != null)
            {
                DataReceivedEventArgs dataReceivedEventArgs = new DataReceivedEventArgs(data);
                if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
                {
                    _ = SynchronizingObject.Invoke(outputDataReceived, new object[]
                    {
                        this,
                        dataReceivedEventArgs
                    });
                    return;
                }
                outputDataReceived(this, dataReceivedEventArgs);
            }
        }

        internal void FixedErrorReadNotifyUser(string data)
        {
            DataReceivedEventHandler errorDataReceived = ErrorDataReceived;
            if (errorDataReceived != null)
            {
                DataReceivedEventArgs dataReceivedEventArgs = new DataReceivedEventArgs(data);
                if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
                {
                    _ = SynchronizingObject.Invoke(errorDataReceived, new object[]
                    {
                        this,
                        dataReceivedEventArgs
                    });
                    return;
                }
                errorDataReceived(this, dataReceivedEventArgs);
            }
        }
    }

    internal class AsyncStreamReader : IDisposable
    {
        internal const int DefaultBufferSize = 1024;
        private Stream stream;
        private Encoding encoding;
        private Decoder decoder;
        private byte[] byteBuffer;
        private char[] charBuffer;
        private UserCallBack userCallBack;
        private bool cancelOperation;
        private ManualResetEvent eofEvent;
        private readonly Queue messageQueue;
        private StringBuilder sb;
        private bool bLastCarriageReturn;
        public virtual Encoding CurrentEncoding => encoding;
        public virtual Stream BaseStream => stream;

        internal AsyncStreamReader(Stream stream, UserCallBack callback, Encoding encoding) : this(stream, callback, encoding, 1024)
        {
        }

        internal AsyncStreamReader(Stream stream, UserCallBack callback, Encoding encoding, int bufferSize)
        {
            Init(stream, callback, encoding, bufferSize);
            messageQueue = new Queue();
        }

        private void Init(Stream stream, UserCallBack callback, Encoding encoding, int bufferSize)
        {
            this.stream = stream;
            this.encoding = encoding;
            userCallBack = callback;
            decoder = encoding.GetDecoder();
            if (bufferSize < 128)
            {
                bufferSize = 128;
            }
            byteBuffer = new byte[bufferSize];
            int _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            charBuffer = new char[_maxCharsPerBuffer];
            cancelOperation = false;
            eofEvent = new ManualResetEvent(false);
            sb = null;
            bLastCarriageReturn = false;
        }

        public virtual void Close()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && stream != null)
            {
                stream.Close();
            }
            if (stream != null)
            {
                stream = null;
                encoding = null;
                decoder = null;
                byteBuffer = null;
                charBuffer = null;
            }
            if (eofEvent != null)
            {
                eofEvent.Close();
                eofEvent = null;
            }
        }

        internal void BeginReadLine()
        {
            if (cancelOperation)
            {
                cancelOperation = false;
            }
            if (sb == null)
            {
                sb = new StringBuilder(1024);
                _ = stream.BeginRead(byteBuffer, 0, byteBuffer.Length, new AsyncCallback(ReadBuffer), null);
                return;
            }
            FlushMessageQueue();
        }

        internal void CancelOperation()
        {
            cancelOperation = true;
        }

        private void ReadBuffer(IAsyncResult ar)
        {
            int num;
            try
            {
                num = stream.EndRead(ar);
            }
            catch (IOException)
            {
                num = 0;
            }
            catch (OperationCanceledException)
            {
                num = 0;
            }
            if (num == 0)
            {
                lock (messageQueue)
                {
                    if (sb.Length != 0)
                    {
                        messageQueue.Enqueue(sb.ToString());
                        sb.Length = 0;
                    }
                    messageQueue.Enqueue(null);
                }
                try
                {
                    FlushMessageQueue();
                    return;
                }
                finally
                {
                    _ = eofEvent.Set();
                }
            }
            int chars = decoder.GetChars(byteBuffer, 0, num, charBuffer, 0);
            _ = sb.Append(charBuffer, 0, chars);
            GetLinesFromStringBuilder();
            _ = stream.BeginRead(byteBuffer, 0, byteBuffer.Length, new AsyncCallback(ReadBuffer), null);
        }

        private void GetLinesFromStringBuilder()
        {
            int i = 0;
            int num = 0;
            int length = sb.Length;
            if (bLastCarriageReturn && length > 0 && sb[0] == '\n')
            {
                i = 1;
                num = 1;
                bLastCarriageReturn = false;
            }
            while (i < length)
            {
                char c = sb[i];
                if (c is '\r' or '\n')
                {
                    if (c == '\r' && i + 1 < length && sb[i + 1] == '\n')
                    {
                        i++;
                    }

                    string obj = sb.ToString(num, i + 1 - num);

                    num = i + 1;

                    lock (messageQueue)
                    {
                        messageQueue.Enqueue(obj);
                    }
                }
                i++;
            }

            // Flush Fix: Send Whatever is left in the buffer
            string endOfBuffer = sb.ToString(num, length - num);
            lock (messageQueue)
            {
                messageQueue.Enqueue(endOfBuffer);
                num = length;
            }
            // End Flush Fix

            if (sb[length - 1] == '\r')
            {
                bLastCarriageReturn = true;
            }
            if (num < length)
            {
                _ = sb.Remove(0, num);
            }
            else
            {
                sb.Length = 0;
            }
            FlushMessageQueue();
        }

        private void FlushMessageQueue()
        {
            while (messageQueue.Count > 0)
            {
                lock (messageQueue)
                {
                    if (messageQueue.Count > 0)
                    {
                        string data = (string)messageQueue.Dequeue();
                        if (!cancelOperation)
                        {
                            userCallBack(data);
                        }
                    }
                }
            }
        }

        internal void WaitUtilEOF()
        {
            if (eofEvent != null)
            {
                _ = eofEvent.WaitOne();
                eofEvent.Close();
                eofEvent = null;
            }
        }
    }

    public class DataReceivedEventArgs : EventArgs
    {
        internal string _data;

        /// <summary>Gets the line of characters that was written to a redirected <see cref="T:System.Diagnostics.Process" /> output stream.</summary>
        /// <returns>The line that was written by an associated <see cref="T:System.Diagnostics.Process" /> to its redirected <see cref="P:System.Diagnostics.Process.StandardOutput" /> or <see cref="P:System.Diagnostics.Process.StandardError" /> stream.</returns>
        /// <filterpriority>2</filterpriority>
        public string Data => _data;

        internal DataReceivedEventArgs(string data)
        {
            _data = data;
        }
    }
}