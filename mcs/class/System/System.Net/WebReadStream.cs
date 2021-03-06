//
// WebReadStream.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
	abstract class WebReadStream : Stream
	{
		public WebOperation Operation {
			get;
		}

		protected Stream InnerStream {
			get;
		}

		public WebReadStream (WebOperation operation, Stream innerStream)
		{
			Operation = operation;
			InnerStream = innerStream;
		}

		Exception NotSupported => new NotSupportedException ();

		public override long Length => throw NotSupported;

		public override long Position {
			get => throw NotSupported;
			set => throw NotSupported;
		}

		public override bool CanSeek => false;

		public override bool CanRead => true;

		public override bool CanWrite => false;

		public override void SetLength (long value)
		{
			throw NotSupported;
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw NotSupported;
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			throw NotSupported;
		}

		public override void Flush ()
		{
			throw NotSupported;
		}

		protected Exception GetException (Exception e)
		{
			e = HttpWebRequest.FlattenException (e);
			if (e is WebException)
				return e;
			if (Operation.Aborted || e is OperationCanceledException || e is ObjectDisposedException)
				return HttpWebRequest.CreateRequestAbortedException ();
			return e;
		}

		public override int Read (byte[] buffer, int offset, int size)
		{
			if (!CanRead)
				throw new NotSupportedException (SR.net_writeonlystream);
			Operation.ThrowIfClosedOrDisposed ();

			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));

			int length = buffer.Length;
			if (offset < 0 || length < offset)
				throw new ArgumentOutOfRangeException (nameof (offset));
			if (size < 0 || (length - offset) < size)
				throw new ArgumentOutOfRangeException (nameof (size));

			try {
				return ReadAsync (buffer, offset, size, CancellationToken.None).Result;
			} catch (Exception e) {
				throw GetException (e);
			}
		}

		public override IAsyncResult BeginRead (byte[] buffer, int offset, int size,
							AsyncCallback cb, object state)
		{
			if (!CanRead)
				throw new NotSupportedException (SR.net_writeonlystream);
			Operation.ThrowIfClosedOrDisposed ();

			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));

			int length = buffer.Length;
			if (offset < 0 || length < offset)
				throw new ArgumentOutOfRangeException (nameof (offset));
			if (size < 0 || (length - offset) < size)
				throw new ArgumentOutOfRangeException (nameof (size));

			var task = ReadAsync (buffer, offset, size, CancellationToken.None);
			return TaskToApm.Begin (task, cb, state);
		}

		public override int EndRead (IAsyncResult r)
		{
			if (r == null)
				throw new ArgumentNullException (nameof (r));

			try {
				return TaskToApm.End<int> (r);
			} catch (Exception e) {
				throw GetException (e);
			}
		}
	}
}

