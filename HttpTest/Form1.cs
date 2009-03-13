using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using Deusty.Net;
using Deusty.Net.HTTP;


namespace HttpTest
{
	public partial class Form1 : Form
	{
		// What webpage to get
		private const String FETCH_HOST = "deusty.com";
		private const String FETCH_REQUEST = "/index.php";
		private const String FETCH_SSL_NAME = "www.deusty.com";

		// Timeouts (in milliseconds) for retreiving various parts of the HTTP response
		private const int WRITE_TIMEOUT           = 10 * 1000;
		private const int READ_HEADER_TIMEOUT     = 10 * 1000;
		private const int READ_FOOTER_TIMEOUT     = 10 * 1000;
		private const int READ_CHUNKSIZE_TIMEOUT  = 10 * 1000;
		private const int READ_BODY_TIMEOUT       = -1 * 1000;

		// Tags used to differentiate what it is we're currently downloading
		private const long HTTP_HEADERS           = 15;
		private const long HTTP_BODY              = 30;
		private const long HTTP_BODY_CHUNKED      = 40;

		// Stages of downloading a chunked resource
		private const uint CHUNKED_STAGE_SIZE     = 1;
		private const uint CHUNKED_STAGE_DATA     = 2;
		private const uint CHUNKED_STAGE_FOOTER   = 3;

		// Asynchronous tcp socket
		private AsyncSocket asyncSocket;

		// HTTP response header
		private HTTPHeader response;

		// Size of data we're receiving
		private Int32 fileSizeInBytes;
		private Int32 totalBytesReceived;

		// Used to keep track of current stage in chunked trasfer
		private uint chunkedTransferStage;

		// Used to time how long the download takes, from connected to finished downloading
		private int headerLength;
		private DateTime startTime;
		

		public Form1()
		{
			InitializeComponent();

			CreateAndSetupAsyncSocket();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// Restore previous window location and size
			if (Properties.Settings.Default.ScreenSize.Equals(SystemInformation.PrimaryMonitorSize))
			{
				this.Location = Properties.Settings.Default.Location;
				this.Size = Properties.Settings.Default.Size;
			}
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			// Save window size and location
			Properties.Settings.Default.ScreenSize = SystemInformation.PrimaryMonitorSize;
			if (this.WindowState == FormWindowState.Normal)
			{
				Properties.Settings.Default.Location = this.Location;
				Properties.Settings.Default.Size = this.Size;
			}
			else
			{
				Properties.Settings.Default.Location = this.RestoreBounds.Location;
				Properties.Settings.Default.Size = this.RestoreBounds.Size;
			}

			Properties.Settings.Default.Save();
		}

		private void CreateAndSetupAsyncSocket()
		{
			// Create a new instance of Deusty.Net.AsyncSocket
			asyncSocket = new AsyncSocket();

			// Tell AsyncSocket to invoke its delegate methods on our form thread
			asyncSocket.SynchronizingObject = this;

			// Register for the events we're interested in
			asyncSocket.DidConnect += new AsyncSocket.SocketDidConnect(asyncSocket_DidConnect);
			asyncSocket.DidSecure += new AsyncSocket.SocketDidSecure(asyncSocket_DidSecure);
			asyncSocket.DidRead += new AsyncSocket.SocketDidRead(asyncSocket_DidRead);
			asyncSocket.DidReadPartial += new AsyncSocket.SocketDidReadPartial(asyncSocket_DidReadPartial);
			asyncSocket.WillClose += new AsyncSocket.SocketWillClose(asyncSocket_WillClose);
			asyncSocket.DidClose += new AsyncSocket.SocketDidClose(asyncSocket_DidClose);
		}

		private void fetchButton_Click(object sender, EventArgs e)
		{
			fetchButton.Enabled = false;
			sslCheckBox.Enabled = false;
					
			logRichTextBox.Clear();
			
			if(sslCheckBox.Checked)
				asyncSocket.Connect(FETCH_HOST, 443);
			else
				asyncSocket.Connect(FETCH_HOST, 80);
		}

		private void LogInfo(String format, params Object[] args)
		{
			String msg = String.Format(format, args);
			
			logRichTextBox.SelectionColor = Color.Purple;
			logRichTextBox.AppendText(msg + "\r\n");
			logRichTextBox.SelectionStart = logRichTextBox.Text.Length;
			logRichTextBox.ScrollToCaret();
		}

		private void LogError(String format, params Object[] args)
		{
			String msg = String.Format(format, args);
			
			logRichTextBox.SelectionColor = Color.Red;
			logRichTextBox.AppendText(msg + "\r\n");
			logRichTextBox.SelectionStart = logRichTextBox.Text.Length;
			logRichTextBox.ScrollToCaret();
		}

		private void LogMessage(String msg)
		{
			logRichTextBox.SelectionColor = Color.Black;
			logRichTextBox.AppendText(msg);
			logRichTextBox.SelectionStart = logRichTextBox.Text.Length;
			logRichTextBox.ScrollToCaret();
		}

		private void SendRequest()
		{
			// Record when we first started the download
			headerLength = 0;
			startTime = DateTime.Now;

			// Create a HTTP request using the Deusty.Net.HTTP.HTTPHeader class.
			// The HTTP protocol is fairly straightforward, but this class helps hide protocol
			// specific information that we're not really concerned with for this AsyncSocket sameple.
			
			HTTPHeader request = HTTPHeader.CreateRequest("GET", FETCH_REQUEST);
			request.SetHeaderFieldValue("Host", FETCH_HOST);

			LogInfo("Sending Request:");
			LogMessage(request.ToString());

			// Convert HTTPHeader object to a Data object.
			// The Data class is just a simple wrapper around a byte[] array.
			// You can pass any class that implements the IData interface to AsyncSocket.Write().
			// There are several other IData classes in the Data.cs file,
			// including the FileData class which wraps a file, making it quick and easy to send a file.
			
			Data requestData = new Data(request.ToString());

			// Now write the data over the socket.
			// This call is asynchronous, and returns immediately.
			// When finished writing, the asyncSocket_DidWrite method is called.

			asyncSocket.Write(requestData, WRITE_TIMEOUT, HTTP_HEADERS);

			// Setup http response
			response = HTTPHeader.CreateEmptyResponse();

			// And start reading in the response
			asyncSocket.Read(AsyncSocket.CRLFData, READ_HEADER_TIMEOUT, HTTP_HEADERS);

			// Reset progress bar
			progressBar.Value = 0;
		}

		private void asyncSocket_DidConnect(AsyncSocket sender, System.Net.IPAddress address, ushort port)
		{
			LogInfo("Connected to {0}:{1}", address, port);

			// The TCP handshake is now complete, meaning we're connected to the remote host.

			if (sslCheckBox.Checked)
			{
				// We want to create a secure SSL/TLS connection.
				// So we need to start the TLS handshake.

				// The StartTLSAsClient method takes 3 parameters.
				// The first is mandatory, the second two are optional.
				// Param 1: The expected server name on the remote certificate.
				// Param 2: Callback to allow you to explicityly check the remote certificate for validation purposes.
				// Param 3: Callback to allow you to choose your own local certificate.

				// The deusty certificate is a self-signed certificate.
				// Self-signed certificates are obviously rejected by default.
				// So we setup a callback in order to accept it.

				RemoteCertificateValidationCallback rcvc =
					new RemoteCertificateValidationCallback(asyncSocket_RemoteCertificateValidationCallback);

				asyncSocket.StartTLSAsClient(FETCH_SSL_NAME, rcvc, null);
			}
			else
			{
				// Not using TLS, so immediatley send the http request
				SendRequest();
			}
		}

		private bool asyncSocket_RemoteCertificateValidationCallback(Object sender,
		                                                    X509Certificate certificate,
		                                                          X509Chain chain,
		                                                    SslPolicyErrors sslPolicyErrors)
		{
			// You can decide whether or not to accept the certificate here
			Console.WriteLine("Remote Certificate: {0}", certificate);
			Console.WriteLine("Public Key: {0}", Data.ToHexString(certificate.GetPublicKey()));
			return true;
		}

		private void asyncSocket_DidSecure(AsyncSocket sender)
		{
			LogInfo("Connection secured");
			SendRequest();
		}

		private void asyncSocket_DidRead(AsyncSocket sender, Data data, long tag)
		{
		//	LogInfo("DidRead: {0}", tag);

			if (tag == HTTP_HEADERS)
			{
				// We read in one line of the http header response
				// Do we have the full http header yet?

				headerLength += data.Length;

				response.AppendBytes(data.ByteArray);
				if (!response.IsHeaderComplete())
				{
					// We don't have a complete header yet
					asyncSocket.Read(AsyncSocket.CRLFData, READ_HEADER_TIMEOUT, HTTP_HEADERS);
				}
				else
				{
					LogInfo("Received Response: ({0} bytes)", headerLength);
					LogMessage(response.ToString());

					// Check the http status code
					if (response.GetStatusCode() != 200)
					{
						// We only support status code 200 in this overly simple http client

						LogError("HTTP status code is not \"200 OK\"");
						LogError("Disconnecting...");

						asyncSocket.Close();
						return;
					}

					// Extract the Content-Length
					String contentLength = response.GetHeaderFieldValue("Content-Length");
					Int32.TryParse(contentLength, out fileSizeInBytes);

					// Extract Transfer-Encoding
					String transferEncoding = response.GetHeaderFieldValue("Transfer-Encoding");
					bool usingChunkedTransfer = String.Equals(transferEncoding, "chunked", StringComparison.OrdinalIgnoreCase);

					if (fileSizeInBytes > 0)
					{
						// Using traditional transfer

						totalBytesReceived = 0;
						asyncSocket.Read(fileSizeInBytes, READ_BODY_TIMEOUT, HTTP_BODY);
					}
					else if (usingChunkedTransfer)
					{
						// Using chunked transfer
						// For more information on chunked transfer, see the "HTTP Made Really Easy" website:
						// http://www.jmarshall.com/easy/http/

						LogInfo("Using chunked transfer - Unable to use progress bar");

						chunkedTransferStage = CHUNKED_STAGE_SIZE;
						asyncSocket.Read(AsyncSocket.CRLFData, READ_CHUNKSIZE_TIMEOUT, HTTP_BODY_CHUNKED);
					}
					else
					{
						LogError("Unable to extract content length, and not using chunked transfer encoding!");
						LogError("Disconnecting...");

						asyncSocket.Close();
						return;
					}
				}
			}
			else if (tag == HTTP_BODY)
			{
				// Write the data to log
				try
				{
					LogMessage(data.ToString());
				}
				catch
				{
					LogError("Cannot convert chunk to UTF-8 string");
				}

				LogInfo("\r\nDownload complete");
				progressBar.Value = 100;

				TimeSpan ellapsed = DateTime.Now - startTime;
				LogInfo("\r\nTotal Time = {0:N} milliseconds", ellapsed.TotalMilliseconds);

				LogInfo("Disconnecting...");
				asyncSocket.Close();
			}
			else if (tag == HTTP_BODY_CHUNKED)
			{
				if (chunkedTransferStage == CHUNKED_STAGE_SIZE)
				{
					// We have just read in a line with the size of the chunk data, in hex,
					// possibly followed by a semicolon and extra parameters that can be ignored,
					// and ending with CRLF
					String sizeLine = data.ToString();

					Int32 chunkSizeInBytes;
					Int32.TryParse(sizeLine, System.Globalization.NumberStyles.HexNumber, null, out chunkSizeInBytes);

					if (chunkSizeInBytes > 0)
					{
						// Don't forget about the trailing CRLF when downloading the data
						chunkSizeInBytes += 2;

						chunkedTransferStage = CHUNKED_STAGE_DATA;
						asyncSocket.Read(chunkSizeInBytes, READ_BODY_TIMEOUT, tag);
					}
					else
					{
						chunkedTransferStage = CHUNKED_STAGE_FOOTER;
						asyncSocket.Read(AsyncSocket.CRLFData, READ_FOOTER_TIMEOUT, tag);
					}
				}
				else if (chunkedTransferStage == CHUNKED_STAGE_DATA)
				{
					// Write the data to log (excluding trailing CRLF)
					try
					{
						String str = Encoding.UTF8.GetString(data.ByteArray, 0, data.Length - 2);
						LogMessage(str);
					}
					catch
					{
						LogError("Cannot convert chunk to UTF-8 string");
					}

					chunkedTransferStage = CHUNKED_STAGE_SIZE;
					asyncSocket.Read(AsyncSocket.CRLFData, READ_CHUNKSIZE_TIMEOUT, tag);
				}
				else if (chunkedTransferStage == CHUNKED_STAGE_FOOTER)
				{
					// The data we just downloaded is either a footer, or an empty line (single CRLF)
					if (data.Length > 2)
					{
						LogInfo("Received HTTP Footer:");
						LogInfo(data.ToString());

						asyncSocket.Read(AsyncSocket.CRLFData, READ_FOOTER_TIMEOUT, tag);
					}
					else
					{
						LogInfo("\r\nDownload complete");

						TimeSpan ellapsed = DateTime.Now - startTime;
						LogInfo("\r\nTotal Time = {0:N} milliseconds", ellapsed.TotalMilliseconds);

						LogInfo("Disconnecting...");
						asyncSocket.Close();
					}
				}
			}
		}

		private void asyncSocket_DidReadPartial(AsyncSocket sender, int partialLength, long tag)
		{
			if (tag == HTTP_BODY)
			{
				totalBytesReceived += partialLength;
				
				float percentComplete = (float)totalBytesReceived / (float)fileSizeInBytes;

				progressBar.Value = (int)(percentComplete * 100);
			}
		}

		private void asyncSocket_WillClose(AsyncSocket sender, Exception e)
		{
			LogInfo("Disconnecting: {0}", e);
		}

		private void asyncSocket_DidClose(AsyncSocket sender)
		{
			LogInfo("Disconnected from host");

			asyncSocket = null;
			CreateAndSetupAsyncSocket();

			fetchButton.Enabled = true;
			sslCheckBox.Enabled = true;
		}

		private void TestSynchronousSocket()
		{
		//	Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		//	
		//	LogInfo("Connecting...");
		//	progressBar.Value = 0;
		//
		//	socket.Connect(FETCH_HOST, 80);
		//
		//	startTime = DateTime.Now;
		//	LogInfo("Connected");
		//
		//	HTTPHeader request = HTTPHeader.CreateRequest("GET", FETCH_REQUEST);
		//	request.SetHeaderFieldValue("Host", FETCH_HOST);
		//
		//	byte[] requestData = Encoding.UTF8.GetBytes(request.ToString());
		//	
		//	int sent = 0;
		//	while (sent < requestData.Length)
		//	{
		//		sent += socket.Send(requestData, sent, requestData.Length - sent, SocketFlags.None);
		//		LogInfo("Sent {0} bytes ({1})", sent, (sent / (float)requestData.Length));
		//	}
		//
		//	byte[] responseData = new byte[/*headerLength + contentLength*/];
		//
		//	int received = 0;
		//	while (received < responseData.Length)
		//	{
		//		received += socket.Receive(responseData, received, responseData.Length - received, SocketFlags.None);
		//
		//		progressBar.Value = (int)((received / (float)responseData.Length) * 100);
		//	}
		//	
		//	TimeSpan ellapsed = DateTime.Now - startTime;
		//	LogInfo("\r\nTotal Time = {0:N} milliseconds", ellapsed.TotalMilliseconds);
		}
	}
}