namespace Assets.POIMatchmaking
{
    /// <summary>
    /// Abstraction of the low level networking functionality.  Supply an implementation of this interface to
    /// define how two peers connect to each other, using whichever low level networking system you want for 
    /// the rest of your code.
    /// </summary>
    public interface INetworkInterface
    {
        /// <summary>
        /// Set to 'true' if this interface is busy dealing with a connection request, and 'false' when 
        /// the connection attempt either succeeds or fails
        /// </summary>
        bool Connecting { get; }

        /// <summary>
        /// Set to 'true' when a peer is connected (i.e. a connection attempt, or listening, succeeded)
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// String description of the last network error to occur, for debugging and user informational purposes
        /// </summary>
        string NetworkError { get; }
        
        /// <summary>
        /// Get a string describing globally how to connect to this peer, when this peer is listening.  The 
        /// format is user-defined - it just gets passed back into the 'StartConnecting' function on another peer.
        /// </summary>
        /// <returns>A connection-info string</returns>
        string GetConnectionInfo();
        
        /// <summary>
        /// Be a host - start listening for clients connecting, expecting a specific client to connect.  This method
        /// must not block, but should arrange to keep listening in the background until the right client does connect.
        /// 
        /// The background events initiated by this method should set 'Connected' to 'true' if/when the correct client
        /// connects.
        /// 
        /// You need to arrange for the client to communicate its UUID to the server somehow, so that the server knows 
        /// when it has the right client and can disconnect any wrong clients.
        /// </summary>
        /// <param name="expectedClientUuid">The UUID of the client to wait for</param>
        /// <returns>'true' if the host is now listening</returns>
        void StartListening(string expectedClientUuid);

		/// <summary>
		/// moved the call of Network.InitializeServer() from StartListening() to InitServer();
		/// </summary>
		bool InitServer();

        /// <summary>
        /// Stop being a host, and get ready to either StartListening or StartConnecting again
        /// </summary>
        void StopListening();

        /// <summary>
        /// Be a client - connect to a host.  This method must not block, but should arrange to attempt to connect in 
        /// the background. It should leave 'Connecting' set to 'true'.
        /// 
        /// The background task should set 'Connecting' to 'false' if it succeeds or gives up.  In addition, if it 
        /// succeeds it should set 'Connected' to 'true'.
        /// </summary>
        /// <param name="connectionInfo">The connection-info string describing the target host</param>
        /// <param name="localUuid">The UUID string for the local peer, which you must send to the host when connecting</param>
        /// <returns>'true' if the client is now attempting to connect</returns>
        bool StartConnecting(string connectionInfo, string localUuid);

        /// <summary>
        /// Stop trying to connect to a host.
        /// </summary>
        void StopConnecting();

		/// <summary>
		/// Continue being a host. Prepare variables to connect to an additional player
		/// </summary>
		void AddAnotherClient();

    }
}