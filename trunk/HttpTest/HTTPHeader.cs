using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Deusty.Net.HTTP
{
	/// <summary>
	/// This class provides a means to read incoming HTTP header information, and to create proper HTTP headers.
	/// 
	/// The various manners in which this class would be used:
	/// 1. Create an empty request using static method, append data until header is complete, extract desired information.
	/// 2. Create an empty response using static method, append data until header is complete, extract desired information.
	/// 3. Create a request using static method, add header field values, call ToString() method.
	/// 4. Create a response using static method, add header field values, call ToString() method.
	/// </summary>
	public class HTTPHeader
	{
		private readonly bool isRequest;
		private readonly bool isEmpty;

		private StringBuilder buffer;
		private Dictionary<String, String> headers;

		private String httpVersion;

		private String requestMethod;
		private String requestURL;

		private int statusCode;
		private String statusDescription;

		/// <summary>
		/// Creates a new empty HTTPHeader, initialized as a request.
		/// Call appendByte(s) to add incoming data to the header,
		/// and use IsHeaderComplete to determine when you've received a full header.
		/// Finally, use the get methods to extract the desired information.
		/// </summary>
		/// <returns>
		///		A new HTTPHeader instance, initialized empty as a request.
		///	</returns>
		public static HTTPHeader CreateEmptyRequest()
		{
			HTTPHeader request = new HTTPHeader(true, true);
			return request;
		}

		/// <summary>
		/// Creates a new empty HTTPHeader, initialized as a response.
		/// Call appendByte(s) to add incoming data to the header,
		/// and use IsHeaderComplete to determine when you've received a full header.
		/// Finally, use the get methods to extract the desired information.
		/// </summary>
		/// <returns>
		///		A new HTTPHeader instance, initialized empty as a response.
		///	</returns>
		public static HTTPHeader CreateEmptyResponse()
		{
			HTTPHeader response = new HTTPHeader(false, true);
			return response;
		}

		/// <summary>
		/// Creates a new HTTPHeader, initialized as a request with the given method and URL.
		/// Use the setHeaderFieldValue method to set other HTTP header values,
		/// and use the overriden ToString() method to get the full finished header.
		/// </summary>
		/// <param name="requestMethod">
		///		The HTTP command (eg. "GET")
		/// </param>
		/// <param name="requestURL">
		///		The HTTP request (eg. "index.html")
		/// </param>
		/// <returns>
		///		A new HTTPHeader instance, initialized non-empty as a request with the given information.
		/// </returns>
		public static HTTPHeader CreateRequest(String requestMethod, String requestURL)
		{
			HTTPHeader request = new HTTPHeader(true, false);
			request.requestMethod = requestMethod;
			request.requestURL = requestURL;

			return request;
		}

		/// <summary>
		/// Creates a new HTTPHeader, which is a clone of the given request.
		/// If the given HTTPHeader is not a request header this method returns null.
		/// If the given HTTPHeader was initialized empty and is not yet complete this method returns null.
		/// Otherwise it returns a clone of the given header.
		/// That is, it will have the same HTTP version, request method and URL.
		/// Also, the headers will be a clone of the headers in the given request,
		/// but changes to the new headers will not affect the original headers.
		/// </summary>
		/// <param name="request">
		///		A complete HTTPHeader request instance to create a clone of.
		/// </param>
		/// <returns>
		///		Null if given an improper parameter. A clone of the original HTTPHeader otherwise.
		/// </returns>
		public static HTTPHeader CreateRequest(HTTPHeader request)
		{
			if (!request.isRequest) return null;

			if (request.isEmpty)
			{
				if (!request.IsHeaderComplete()) return null;
				if (request.headers == null) request.ParseRequest();
			}

			HTTPHeader clone = new HTTPHeader(true, false);
			clone.httpVersion = request.httpVersion;
			clone.requestMethod = request.requestMethod;
			clone.requestURL = request.requestURL;
			clone.headers = new Dictionary<String, String>(request.headers, StringComparer.OrdinalIgnoreCase);

			return clone;
		}

		/// <summary>
		/// Create a new HTTPHeader, initialized as a response with the given status code and description.
		/// Use the setHeaderFieldValue method to set other HTTP header values,
		/// and use the overriden ToString method to get the full finished header.
		/// </summary>
		/// <param name="statusCode">
		///		The HTTP status code (eg. 404)
		/// </param>
		/// <param name="statusDescription">
		///		The HTTP status description (eg. "Not Found")
		/// </param>
		/// <returns>
		///		A new HTTPHeader instance, initialized non-empty as a response with the given information.
		/// </returns>
		public static HTTPHeader CreateResponse(int statusCode, String statusDescription)
		{
			HTTPHeader response = new HTTPHeader(false, false);
			response.statusCode = statusCode;
			response.statusDescription = statusDescription;

			return response;
		}

		/// <summary>
		/// Creates a new HTTPHeader, which is a clone of the given response.
		/// If the given HTTPHeader is not a response header this method returns null.
		/// If the given HTTPHeader was initialized empty and is not yet complete this method returns null.
		/// Otherwise it returns a clone of the given header.
		/// That is, it will have the same HTTP version, status code and description.
		/// Also, the headers will be a clone of the headers in the given response,
		/// but changes to the new headers will not affect the original headers.
		/// </summary>
		/// <param name="response">
		///		A complete HTTPHeader response instance to create a clone of.
		/// </param>
		/// <returns>
		///		Null if given an improper parameter. A clone of the original HTTPHeader otherwise.
		/// </returns>
		public static HTTPHeader CreateResponse(HTTPHeader response)
		{
			if (response.isRequest) return null;

			if (response.isEmpty)
			{
				if (!response.IsHeaderComplete()) return null;
				if (response.headers == null) response.ParseResponse();
			}

			HTTPHeader clone = new HTTPHeader(false, false);
			clone.httpVersion = response.httpVersion;
			clone.statusCode = response.statusCode;
			clone.statusDescription = response.statusDescription;
			clone.headers = new Dictionary<String, String>(response.headers, StringComparer.OrdinalIgnoreCase);

			return clone;
		}

		/// <summary>
		/// Private Constructor.
		/// </summary>
		private HTTPHeader()
		{
			throw new Exception("Use the static HTTPHeader.CreateX() headers.");
		}

		/// <summary>
		/// Private Constructor.
		/// Use the public static constructors to create the proper objects.
		/// </summary>
		/// <param name="isRequest">
		///		Whether this HTTPHeader instance will be treated as a request or response.
		/// </param>
		/// <param name="isEmpty">
		///		Whether this HTTPHeader instance will be created as empty or not.
		///		An empty HTTPHeader will presumably be reading in a request or response from the network.
		/// </param>
		private HTTPHeader(bool isRequest, bool isEmpty)
		{
			this.isRequest = isRequest;
			this.isEmpty = isEmpty;

			if (isEmpty)
				buffer = new StringBuilder();
			else
			{
				headers = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
				httpVersion = "HTTP/1.1";
			}
		}

		/// <summary>
		/// Returns whether or not this HTTPHeader corresponds to an HTTP request.
		/// </summary>
		public bool IsRequest
		{
			get { return isRequest; }
		}

		/// <summary>
		/// Returns whether or not this HTTPHeader corresponds to an HTTP response.
		/// </summary>
		public bool IsResponse
		{
			get { return !isRequest; }
		}

		/// <summary>
		/// Appends the given incoming data to the HTTPHeader.
		/// 
		/// Note: This method only applies to HTTP header objects created using the createEmpty static constructor.
		/// </summary>
		/// <param name="data">
		///		Incoming data (presumably read from the network).
		/// </param>
		/// <returns>
		///		The number of bytes that were successfully added to the header.
		///		This will be positive if successful, and zero if the header is complete.
		///		Note that if data past the header is given, it will not be written to the header,
		///		and the return value will reflect this by being smaller than data.Length.
		/// 
		///		If the header was not created as an empty header capable of accepting raw data,
		///		or invalid data is given, this method returns -1;
		/// </returns>
		public int AppendBytes(byte[] data)
		{
			return AppendBytes(data, 0, data.Length);
		}

		/// <summary>
		/// Appends the given incoming data to the HTTPHeader.
		/// The data is appended starting at the given offset, and up to the given length.
		/// 
		/// Note: This method only applies to HTTP header objects created using the createEmpty static constructor
		/// </summary>
		/// <param name="data">
		///		Incoming data (presumably read from the network).
		/// </param>
		/// <param name="offset">
		///		The offset from which to start appending data.
		/// </param>
		/// <param name="length">
		///		The amount of data to append.
		/// </param>
		/// <returns>
		///		The number of bytes that were successfully added to the header.
		///		This will be positive if successful, and zero if the header is complete.
		///		Note that if data past the header is given, it will not be written to the header,
		///		and the return value will reflect this by being smaller than the given length.
		///		
		///		If the header was not created as an empty header capable of accepting raw data,
		///		or invalid data is given, this method returns -1;
		/// </returns>
		public int AppendBytes(byte[] data, int offset, int length)
		{
			if (!isEmpty) return -1;

			if (!IsHeaderComplete())
			{
				String str = Encoding.UTF8.GetString(data, offset, length);
				if (str.Contains("\r\n\r\n") && !str.EndsWith("\r\n\r\n"))
				{
					str = str.Remove(str.IndexOf("\r\n\r\n") + 4);
				}
				buffer.Append(str);
				return Encoding.UTF8.GetByteCount(str);
			}
			else
			{
				return 0;
			}
		}

		/// <summary>
		/// Returns whether or not the full header has been received yet.
		/// If not, then continue appending bytes.  If so, then you may freely extract any needed information.
		/// 
		/// Note: This method only applies to HTTP header objects created using the createEmpty static constructor.
		/// </summary>
		/// <returns>
		///		True if the header has been read to completion, false otherwise.
		/// </returns>
		public bool IsHeaderComplete()
		{
			if (isEmpty)
				return buffer.ToString().EndsWith("\r\n\r\n");
			else
				return true;
		}
		
		/// <summary>
		/// Returns whether or not the received header appears to be a valid HTTP header so far.
		/// If you haven't read in at least the first line of the request/response, this method will return false.
		/// 
		/// Note: This method only applies to HTTP header objects created using the createEmpty static constructor.
		/// </summary>
		/// <returns>
		///		True if the header appears to be valid so far.
		/// 	False otherwise.
		/// </returns>
		public bool IsValid()
		{
			if (!isEmpty) return false;
			
			if(isRequest)
			{
				ParseRequest();
				
				if (requestMethod == null) return false;
				if (requestURL    == null) return false;
				if (httpVersion   == null) return false;
				if (headers       == null) return false;
				
				return true;
			}
			else
			{
				ParseResponse();
				
				if (httpVersion       == null) return false;
				if (statusDescription == null) return false;
				if (headers           == null) return false;
				
				return true;
			}
		}

		/// <summary>
		/// This method will parse the data in the buffer as a request.
		/// </summary>
		private void ParseRequest()
		{
			String[] terms = { "\r\n" };
			String[] lines = buffer.ToString().Split(terms, StringSplitOptions.RemoveEmptyEntries);

			if (lines.Length == 0) return;

			String[] requestComponents = lines[0].Split(' ');
			if (requestComponents.Length == 3)
			{
				requestMethod = requestComponents[0];
				requestURL = requestComponents[1];
				httpVersion = requestComponents[2];
			}
			else
			{
				return;
			}

			// Create a case-insensitive hashtable
			headers = new Dictionary<String, String>(lines.Length - 1, StringComparer.OrdinalIgnoreCase);

			for (int i = 1; i < lines.Length; i++)
			{
				String line = lines[i];
				int firstColonIndex = line.IndexOf(':');

				if (firstColonIndex >= 0)
				{
					String key = line.Substring(0, firstColonIndex);
					String value = line.Substring(firstColonIndex + 1).TrimStart();

					// Notice that we don't use the Add() method.
					// This is because it will throw an exception if key already exists in the dictionary.
					headers[key] = value;
				}
			}
		}

		/// <summary>
		/// Returns the request method for an HTTP header object initialized as a request.
		/// If the object was created empty, this method will return null unless a full header has been received.
		/// </summary>
		/// <returns>
		///		The request method (eg. "GET")
		/// </returns>
		public String GetRequestMethod()
		{
			if (isRequest)
			{
				if (isEmpty)
				{
					if (!IsHeaderComplete()) return null;
					if (headers == null) ParseRequest();
				}
				return requestMethod;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Returns the request URL for an HTTP header object initialized as a request.
		/// If the object was created empty, this method will return null unless a full header has been received.
		/// </summary>
		/// <returns>
		///		The request URL (eg. "index.html")
		/// </returns>
		public String GetRequestURL()
		{
			if (isRequest)
			{
				if (isEmpty)
				{
					if (!IsHeaderComplete()) return null;
					if (headers == null) ParseRequest();
				}
				return requestURL;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// This method will parse the data in the buffer as a response.
		/// </summary>
		private void ParseResponse()
		{
			String[] terms = { "\r\n" };
			String[] lines = buffer.ToString().Split(terms, StringSplitOptions.RemoveEmptyEntries);

			if (lines.Length == 0) return;

			String[] responseComponents = lines[0].Split(' ');
			if (responseComponents.Length >= 3)
			{
				httpVersion = responseComponents[0];
				int.TryParse(responseComponents[1], out statusCode);

				// The status description may contain spaces, so it's everything after the status code
				int startIndex = lines[0].IndexOf(responseComponents[2]);
				statusDescription = lines[0].Substring(startIndex).Trim();
			}
			else
			{
				return;
			}

			// Create a case-insensitive hashtable
			headers = new Dictionary<String, String>(lines.Length - 1, StringComparer.OrdinalIgnoreCase);

			for (int i = 1; i < lines.Length; i++)
			{
				String line = lines[i];
				int firstColonIndex = line.IndexOf(':');

				if (firstColonIndex >= 0)
				{
					String key = line.Substring(0, firstColonIndex);
					String value = line.Substring(firstColonIndex + 1).TrimStart();

					// Notice that we don't use the Add() method.
					// This is because it will throw an exception if key already exists in the dictionary.
					headers[key] = value;
				}
			}
		}

		/// <summary>
		/// Returns the status code for an HTTP header object initialized as a response.
		/// If the object was created empty, this method will return -1 unless a full header has been received.
		/// </summary>
		/// <returns>
		///		The status code (eg. 404)
		/// </returns>
		public int GetStatusCode()
		{
			if (isRequest)
			{
				return 0;
			}
			else
			{
				if (isEmpty)
				{
					if (!IsHeaderComplete()) return 0;
					if (headers == null) ParseResponse();
				}
				return statusCode;
			}
		}

		/// <summary>
		/// Returns the status description for an HTTP header object initialized as a response.
		/// If the object was created empty, this method will return null unless a full header has been received.
		/// </summary>
		/// <returns>
		///		The status description (eg. "Not Found")
		/// </returns>
		public String GetStatusDescription()
		{
			if (isRequest)
			{
				return null;
			}
			else
			{
				if (isEmpty)
				{
					if (!IsHeaderComplete()) return null;
					if (headers == null) ParseResponse();
				}
				return statusDescription;
			}
		}

		/// <summary>
		/// Returns the HTTP version number for an HTTP header object.
		/// If the object was created empty, this method will return null unless a full header has been received.
		/// </summary>
		/// <returns>
		///		The HTTP version string (eg. "HTTP/1.1")
		/// </returns>
		public String GetHTTPVersion()
		{
			if (isRequest)
			{
				if (isEmpty)
				{
					if (!IsHeaderComplete()) return null;
					if (headers == null) ParseRequest();
				}
				return httpVersion;
			}
			else
			{
				if (isEmpty)
				{
					if (!IsHeaderComplete()) return null;
					if (headers == null) ParseResponse();
				}
				return httpVersion;
			}
		}

		/// <summary>
		/// Returns all the header field values in a Dictionary for easy accessibility.
		/// The dictionary keys are case-insensitive, so dict["host"] and dict["Host"] will return the same thing.
		/// </summary>
		public Dictionary<String, String> GetHeaders()
		{
			if (isEmpty)
			{
				if (!IsHeaderComplete()) return null;
				if (headers == null)
				{
					if (isRequest)
						ParseRequest();
					else
						ParseResponse();
				}
			}
			return headers;
		}
		
		/// <summary>
		/// Returns the value for a specific header field value.
		/// The header field values are case-insensitive, so "host" and "Host" will return the same thing.
		/// </summary>
		/// <param name="headerField">
		///		The desired header (eg. "Content-Length")
		/// </param>
		public String GetHeaderFieldValue(String headerField)
		{
			if (isEmpty)
			{
				if (!IsHeaderComplete()) return null;
				if (headers == null)
				{
					if (isRequest)
						ParseRequest();
					else
						ParseResponse();
				}
			}
			String result;
			headers.TryGetValue(headerField, out result);
			return result;
		}

		/// <summary>
		/// Adds a header field to the list of header.
		/// If the header field already exists, it is updated with the new value.
		/// If the object was created empty, this method does nothing.
		/// </summary>
		/// <param name="headerField">
		///		The desired header (eg. "Content-Length")
		/// </param>
		/// <param name="value">
		///		The desired header value (eg. "1245")
		/// </param>
		public void SetHeaderFieldValue(String headerField, String value)
		{
			if (!isEmpty)
			{
				headers[headerField] = value;
			}
		}

		/// <summary>
		/// The string representation of the header as it currently exists.
		/// </summary>
		public override String ToString()
		{
			if (isRequest)
			{
				if (isEmpty) return buffer.ToString();

				StringBuilder request = new StringBuilder();
				request.Append(requestMethod + " " + requestURL + " HTTP/1.1" + "\r\n");

				//Loop through hashtable
				foreach (KeyValuePair<String, String> kvp in headers)
				{
					request.Append(kvp.Key + ": " + kvp.Value + "\r\n");
				}

				// Don't forget to finish the HTTP headers with an empty line
				request.Append("\r\n");

				return request.ToString();
			}
			else
			{
				if (isEmpty) return buffer.ToString();

				StringBuilder response = new StringBuilder();
				response.Append("HTTP/1.1 " + statusCode + " " + statusDescription + "\r\n");

				//Loop through hashtable
				foreach (KeyValuePair<String, String> kvp in headers)
				{
					response.Append(kvp.Key + ": " + kvp.Value + "\r\n");
				}

				// Don't forget to finish the HTTP headers with an empty line
				response.Append("\r\n");

				return response.ToString();
			}
		}
	}
}
