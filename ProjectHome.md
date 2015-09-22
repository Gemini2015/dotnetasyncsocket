AsyncSocket is a TCP/IP socket networking library for .Net.  It's fully asynchronous, with complete delegate support.  Here are the key features:

  * Queued non-blocking reads and writes, with optional timeouts. You tell it what to read or write, and it will call you when it's done.

  * Automatic socket acceptance. If you tell it to accept connections, it will call you with new instances of itself for each connection.

  * Delegate support. Errors, connections, accepts, read completions, write completions, progress, and disconnections all result in a call to your delegate method(s).

  * Self-contained in one class. You don't need to worry about streams or sockets. The class handles all of that.

  * Support for TCP streams over IPv4 and IPv6.

  * Support for encryption via SSL/TLS

The project contains a sample Echo Server and HTTP Client app.

Please support this free and open source project <a href='https://www.paypal.com/us/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9405132'>
<img src='http://www.paypal.com/en_US/i/btn/btn_donate_SM.gif' alt='donation' /></a>