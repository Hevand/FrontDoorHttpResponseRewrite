using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.UI.HtmlControls;

namespace AFD.Samples.HttpResponseRewrite.IISModule
{
    //Originates from: https://weblog.west-wind.com/posts/2009/Nov/13/Capturing-and-Transforming-ASPNET-Output-with-ResponseFilter
    internal class ResponseRewriteFilter : Stream
    {
        readonly Stream _stream;

        long _position;

        MemoryStream _cacheStream = new MemoryStream();
        private int _cachePointer;

        public ResponseRewriteFilter(Stream source)
        {
            this._stream = source;
        }

        private bool IsCaptured
        {
            get
            {
                if (CaptureStream != null || CaptureString != null || TransformStream == null || TransformString == null)
                    return true;

                return false;
            }
        }

        private bool IsOutputDelayed
        {
            get
            {
                if (TransformStream == null || TransformString == null)
                    return true;

                return false;
            }
        }

        public event Action<MemoryStream> CaptureStream;

        public event Action<string> CaptureString;

        public event Func<byte[], byte[]> TransformWrite;

        public event Func<MemoryStream, MemoryStream> TransformStream;

        public event Func<string, string> TransformString;

        protected virtual void OnCaptureStream(MemoryStream ms)
        {
            CaptureStream?.Invoke(ms);
        }


        private void OnCaptureStringInternal(MemoryStream ms)
        {
            if (CaptureString != null)
            {
                string content = HttpContext.Current.Response.ContentEncoding.GetString(ms.ToArray());
                OnCaptureString(content);
            }
        }

        protected virtual void OnCaptureString(string output)
        {
            CaptureString?.Invoke(output);
        }

        protected virtual byte[] OnTransformWrite(byte[] buffer)
        {
            if (TransformWrite != null)
                return TransformWrite(buffer);
            return buffer;
        }

        private byte[] OnTransformWriteStringInternal(byte[] buffer)
        {
            Encoding encoding = HttpContext.Current.Response.ContentEncoding;
            string output = OnTransformWriteString(encoding.GetString(buffer));
            return encoding.GetBytes(output);
        }

        private string OnTransformWriteString(string value)
        {
            if (TransformString != null)
                return TransformString(value);
            return value;
        }


        protected virtual MemoryStream OnTransformCompleteStream(MemoryStream ms)
        {
            if (TransformStream != null)
                return TransformStream(ms);

            return ms;
        }

        private string OnTransformCompleteString(string responseText)
        {
            TransformString?.Invoke(responseText);

            return responseText;
        }


        internal MemoryStream OnTransformCompleteStringInternal(MemoryStream ms)
        {
            if (TransformString == null)
                return ms;

            //string content = ms.GetAsString();
            string content = HttpContext.Current.Response.ContentEncoding.GetString(ms.ToArray());

            content = TransformString(content);
            byte[] buffer = HttpContext.Current.Response.ContentEncoding.GetBytes(content);
            ms = new MemoryStream();
            ms.Write(buffer, 0, buffer.Length);
            //ms.WriteString(content);

            return ms;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => 0;

        public override long Position { get => _position; set =>_position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _cacheStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _cacheStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (IsCaptured)
            {
                // copy to holding buffer only - we'll write out later
                _cacheStream.Write(buffer, 0, count);
                _cachePointer += count;
            }

            // just transform this buffer
            if (TransformWrite != null)
                buffer = OnTransformWrite(buffer);
            if (TransformString != null)
                buffer = OnTransformWriteStringInternal(buffer);

            if (!IsOutputDelayed)
                _stream.Write(buffer, offset, buffer.Length);
        }


        public override void Flush()
        {
            if (IsCaptured && _cacheStream.Length > 0)
            {
                // Check for transform implementations
                _cacheStream = OnTransformCompleteStream(_cacheStream);
                _cacheStream = OnTransformCompleteStringInternal(_cacheStream);

                OnCaptureStream(_cacheStream);
                OnCaptureStringInternal(_cacheStream);

                // write the stream back out if output was delayed
                if (IsOutputDelayed)
                    _stream.Write(_cacheStream.ToArray(), 0, (int)_cacheStream.Length);

                // Clear the cache once we've written it out
                _cacheStream.SetLength(0);
            }

            // default flush behavior
            _stream.Flush();
        }

    }
}