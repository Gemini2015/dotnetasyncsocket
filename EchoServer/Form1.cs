// Uncomment out the following line for a multi-threaded architecture
//#define IS_MULTITHREADED

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Deusty.Net;


namespace EchoServer
{
	public partial class Form1 : Form
	{
		private bool isStarted;
		private AsyncSocket listenSocket;
		private List<AsyncSocket> connectedSockets;

		public Form1()
		{
			InitializeComponent();

			// Create a new instance of Deusty.Net.AsyncSocket
			listenSocket = new AsyncSocket();

#if IS_MULTITHREADED
			// Tell AsyncSocket to allow multi-threaded delegate methods
			// Note: Accepted sockets will automatically inherit this setting as well
			listenSocket.AllowMultithreadedCallbacks = true;
#else
			// Tell AsyncSocket to invoke its delegate methods on our form thread
			// Note: Accepted sockets will automatically inherit this setting as well
			listenSocket.SynchronizingObject = this;
#endif

			// Register for the events we're interested in
			listenSocket.DidAccept += new AsyncSocket.SocketDidAccept(listenSocket_DidAccept);

			// Initialize list to hold connected sockets
			// We support multiple concurrent connections
			connectedSockets = new List<AsyncSocket>();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// Restore previous window location and size
			if (Properties.Settings.Default.ScreenSize.Equals(SystemInformation.PrimaryMonitorSize))
			{
				this.Location = Properties.Settings.Default.Location;
				this.Size = Properties.Settings.Default.Size;
			}

			// Restore previous used port (if needed)
			UInt16 serverPort = Properties.Settings.Default.ServerPort;
			if (serverPort > 0)
			{
				portTextBox.Text = serverPort.ToString();
				portTextBox.ForeColor = Color.Black;
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

		private void portTextBox_Enter(object sender, EventArgs e)
		{
			if (portTextBox.ForeColor == Color.Gray)
			{
				portTextBox.Clear();
				portTextBox.ForeColor = Color.Black;
			}
		}

		private void portTextBox_Leave(object sender, EventArgs e)
		{
			UInt16 port;
			bool parseSuccess = UInt16.TryParse(portTextBox.Text, out port);

			if (parseSuccess)
			{
				Properties.Settings.Default.ServerPort = port;
			}
			else if (e != null)
			{
				Properties.Settings.Default.ServerPort = 0;

				portTextBox.Text = "Any";
				portTextBox.ForeColor = Color.Gray;
			}
		}

		private void portTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				portTextBox_Leave(sender, null);
				portTextBox.SelectAll();
			}
		}

		private void startStopButton_Click(object sender, EventArgs e)
		{
			if (!isStarted)
			{
				// Start the echo server
				UInt16 port;
				UInt16.TryParse(portTextBox.Text, out port);

				// AsyncSocket.Accept will setup sockets for IPv4 and IPv6
				// You can connect using telnet

				Exception error;
				if (!listenSocket.Accept(port, out error))
				{
					LogError("Error starting server: {0}", error);
					return;
				}
				
				LogInfo("Echo server started on port {0}", listenSocket.LocalPort);
				isStarted = true;

				portTextBox.Enabled = false;
				startStopButton.Text = "Stop";
			}
			else
			{
				// Stop accepting connections
				listenSocket.Close();

#if IS_MULTITHREADED
				// Stop any client connections
				lock (connectedSockets)
				{
					foreach (AsyncSocket socket in connectedSockets)
					{
						socket.Close();
					}
				}
#else
				// Stop any client connections
				foreach (AsyncSocket socket in connectedSockets)
				{
					socket.Close();
				}
#endif

				LogInfo("Stopped Echo server");
				isStarted = false;

				portTextBox.Enabled = true;
				startStopButton.Text = "Start";
			}
		}

#if IS_MULTITHREADED
		private delegate void LogMessageDelegate(String format, params Object[] args);
#endif
		
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

		private void LogMessage(String format, params Object[] args)
		{
			String msg = String.Format(format, args);

			logRichTextBox.SelectionColor = Color.Black;
			logRichTextBox.AppendText(msg);
			logRichTextBox.SelectionStart = logRichTextBox.Text.Length;
			logRichTextBox.ScrollToCaret();
		}

		private void listenSocket_DidAccept(AsyncSocket sender, AsyncSocket newSocket)
		{
#if IS_MULTITHREADED
			object[] args = { newSocket.RemoteAddress, newSocket.RemotePort };
			this.BeginInvoke(new LogMessageDelegate(LogInfo), "Accepted client {0}:{1}", args);
#else
			LogInfo("Accepted client {0}:{1}", newSocket.RemoteAddress, newSocket.RemotePort);
#endif

			newSocket.DidRead += new AsyncSocket.SocketDidRead(asyncSocket_DidRead);
			newSocket.DidWrite += new AsyncSocket.SocketDidWrite(asyncSocket_DidWrite);
			newSocket.WillClose += new AsyncSocket.SocketWillClose(asyncSocket_WillClose);
			newSocket.DidClose += new AsyncSocket.SocketDidClose(asyncSocket_DidClose);

#if IS_MULTITHREADED
			lock (connectedSockets)
			{
				connectedSockets.Add(newSocket);
			}
#else
			connectedSockets.Add(newSocket);
#endif

			newSocket.Write(new Data("Welcome to the AsyncSocket Echo Server\r\n"), -1, 0);

			// Remember: newSocket automatically inherits the invoke options of it's parent (listenSocket).
		}

		private void asyncSocket_DidRead(AsyncSocket sender, Data data, long tag)
		{
			String msg = null;
			try
			{
				msg = Encoding.UTF8.GetString(data.ByteArray);

#if IS_MULTITHREADED
				object[] args = { };
				this.BeginInvoke(new LogMessageDelegate(LogMessage), msg, args);
#else
				LogMessage(msg);
#endif
			}
			catch(Exception e)
			{
#if IS_MULTITHREADED
				object[] args = { e };
				this.BeginInvoke(new LogMessageDelegate(LogError), "Error converting received data into UTF-8 String: {0}", args);
#else
				LogError("Error converting received data into UTF-8 String: {0}", e);
#endif
			}

			// Even if we were unable to write the incoming data to the log,
			// we're still going to echo it back to the client.
			sender.Write(data, -1, 0);
		}

		private void asyncSocket_DidWrite(AsyncSocket sender, long tag)
		{
			sender.Read(AsyncSocket.CRLFData, -1, 0);
		}

		private void asyncSocket_WillClose(AsyncSocket sender, Exception e)
		{
#if IS_MULTITHREADED
			object[] args = { sender.RemoteAddress, sender.RemotePort };
			this.BeginInvoke(new LogMessageDelegate(LogInfo), "Client Disconnected: {0}:{1}", args);
#else
			LogInfo("Client Disconnected: {0}:{1}", sender.RemoteAddress, sender.RemotePort);
#endif
		}

		private void asyncSocket_DidClose(AsyncSocket sender)
		{
#if IS_MULTITHREADED
			lock (connectedSockets)
			{
				connectedSockets.Remove(sender);
			}
#else
			connectedSockets.Remove(sender);
#endif
		}
	}
}