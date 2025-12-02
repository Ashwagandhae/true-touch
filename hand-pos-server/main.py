import socket
import struct
import math
import time


class HandPosServer:
    def __init__(self, host="127.0.0.1", port=8080):
        self.host = host
        self.port = port
        self.server_socket = None
        self.client_connection = None
        self.running = False

    def start(self):
        """Initializes the socket and binds to the port."""
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

        # Allow reuse of the address so we don't get 'Address already in use' errors on quick restarts
        self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

        try:
            self.server_socket.bind((self.host, self.port))
            print(f"Server started on {self.host}:{self.port}")
            self.server_socket.listen(1)
            self.running = True
        except OSError as e:
            print(f"Error binding to port {self.port}: {e}")
            self.running = False

    def wait_for_connection(self):
        """Blocks until a client (Unity) connects."""
        if not self.running:
            print("Server socket not running. Call start() first.")
            return

        print("Waiting for Unity client to connect...")
        try:
            self.client_connection, client_address = self.server_socket.accept()  # type: ignore
            print(f"Connection from {client_address}")
            return True
        except Exception as e:
            print(f"Error accepting connection: {e}")
            return False

    def send_data(self, v0, v1, v2, v3, v4):
        """
        Sends 5 float values to the connected client.
        Returns True if successful, False if connection is lost.
        """
        if self.client_connection:
            try:
                # Pack the 5 floats into bytes (little-endian standard)
                data = struct.pack("<5f", v0, v1, v2, v3, v4)
                self.client_connection.sendall(data)
                return True
            except (ConnectionResetError, BrokenPipeError, OSError):
                print("Client disconnected.")
                self.close_client()
                return False
        return False

    def close_client(self):
        """Closes the current client connection."""
        if self.client_connection:
            self.client_connection.close()
            self.client_connection = None

    def close(self):
        """Shuts down the entire server."""
        self.close_client()
        if self.server_socket:
            self.server_socket.close()
            print("Server closed.")


if __name__ == "__main__":
    # Example usage:
    # 1. Instantiate the server
    server = HandPosServer()
    server.start()

    try:
        while True:
            # 2. Ensure we have a connection
            if server.client_connection is None:
                server.wait_for_connection()

            # 3. Calculate your data (Replace this with Arduino reading logic)
            current_millis = time.time() * 1000

            val0 = (math.sin(current_millis / 1200.0) + 1.0) / 2.0
            val1 = (math.sin(current_millis / 800.0) + 1.0) / 2.0
            val2 = (math.sin(current_millis / 400.0) + 1.0) / 2.0
            val3 = (math.sin(current_millis / 300.0) + 1.0) / 2.0
            val4 = (math.sin(current_millis / 200.0) + 1.0) / 2.0

            # 4. Send the data
            success = server.send_data(val0, val1, val2, val3, val4)

            # If send failed, the loop will catch it at step 2 and wait for reconnection
            if not success:
                time.sleep(1)  # Wait a bit before trying to accept again
            else:
                # Control update rate (remove this sleep if your Arduino read blocks)
                time.sleep(0.016)

    except KeyboardInterrupt:
        print("\nStopping server...")
        server.close()
