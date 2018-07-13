Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.Threading

Public Class Form1
    Private IPAddress As IPAddress
    Private StopServer As Boolean = False
    Private AllowServerStart As Boolean = False
    'Private ExitProgram As Boolean = False
    Private thread As Thread
    Private ClosedSocket As Boolean = False

    ' Thread signal.
    Public allDone As New ManualResetEvent(False)

    Public Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'Start the server
        IPAddress = GetIPAddress()

        ' Check if it is possible to retrieve the IPv4 address of the PC
        If IPAddress.Equals(IPAddress.None) Then
            MsgBox("Can't retrieve the correct PC's IP address. Can't start the server")
        Else
            ' Start the main thread
            ClosedSocket = False
            StopServer = False
            thread = New Thread(AddressOf StartServer)
            thread.Start()
            Button1.Enabled = False
        End If
    End Sub

    Private Sub StartServer()
        ' Data buffer for incoming data.
        Dim bytes() As Byte = New [Byte](1023) {}

        ' Establish the local endpoint for the socket.
        Dim ipAddress As IPAddress = Me.IPAddress
        Dim localEndPoint As New IPEndPoint(ipAddress, 11100)

        ' Write the server IP address
        AppendTextBox("Server IP address: " + ipAddress.ToString() + vbCrLf)

        ' Create a TCP/IP socket.
        Dim listener As Socket
        listener = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

        ' Bind the socket to the local endpoint and listen for incoming connections.
        listener.Bind(localEndPoint)
        listener.Listen(100)

        While StopServer = False
            ' Set the event to nonsignaled state.
            allDone.Reset()

            ' Start an asynchronous socket to listen for connections.
            AppendTextBox("Waiting for a connection...")
            listener.BeginAccept(New AsyncCallback(AddressOf AcceptCallback), listener)

            ' Wait until a connection is made and processed before continuing.
            allDone.WaitOne()

            ResetTextBox()
        End While

        ' Close the listener and release all the resources
        ClosedSocket = True
        listener.Close()
    End Sub

    Private Function GetIPAddress() As IPAddress
        'Get the IPv4 Address of the local PC
        Dim host As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())
        For Each address As IPAddress In host.AddressList
            If address.AddressFamily = AddressFamily.InterNetwork Then
                Return address
            End If
        Next
        Return IPAddress.None
    End Function

    Public Sub AcceptCallback(ByVal ar As IAsyncResult)
        ' If the socket has not been closed and/or disposed
        If Not ClosedSocket Then
            ' Get the socket that handles the client request.
            Dim listener As Socket = CType(ar.AsyncState, Socket)
            ' End the operation.
            Dim handler As Socket = listener.EndAccept(ar)

            ' Create the state object for the async receive.
            Dim state As New StateObject
            state.workSocket = handler
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf ReadCallback), state)
        End If
    End Sub 'AcceptCallback


    Public Sub ReadCallback(ByVal ar As IAsyncResult)
        Dim content As String = String.Empty

        ' Retrieve the state object and the handler socket
        ' from the asynchronous state object.
        Dim state As StateObject = CType(ar.AsyncState, StateObject)
        Dim handler As Socket = state.workSocket

        ' Read data from the client socket. 
        Dim bytesRead As Integer = handler.EndReceive(ar)

        If bytesRead > 0 Then
            ' There  might be more data, so store the data received so far.
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead))

            ' Check for end-of-file tag. If it is not there, read 
            ' more data.
            content = state.sb.ToString()
            'If content.IndexOf("9") > -1 Then
            ' All the data has been read from the 
            ' client. Display it on the console.
            AppendTextBox("Read " + content.Length.ToString + " bytes from socket. " + vbLf + " Data : " + content.ToString + vbCrLf)
            ' Echo the data back to the client.
            'Send(handler, content)
            'handler.Shutdown(SocketShutdown.Both)
            'handler.Close()

            ' Signal the main thread to continue.
            'allDone.Set()
            'Else
            ' Not all data received. Get more.
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf ReadCallback), state)
            'End If
        End If
    End Sub 'ReadCallback

    Private Sub Send(ByVal handler As Socket, ByVal data As String)
        ' Convert the string data to byte data using ASCII encoding.
        Dim byteData As Byte() = Encoding.ASCII.GetBytes(data)

        ' Begin sending the data to the remote device.
        handler.BeginSend(byteData, 0, byteData.Length, 0, New AsyncCallback(AddressOf SendCallback), handler)
    End Sub 'Send


    Private Sub SendCallback(ByVal ar As IAsyncResult)
        ' Retrieve the socket from the state object.
        Dim handler As Socket = CType(ar.AsyncState, Socket)

        ' Complete sending the data to the remote device.
        Dim bytesSent As Integer = handler.EndSend(ar)
        AppendTextBox("Sent " + bytesSent + " bytes to client.")

        handler.Shutdown(SocketShutdown.Both)
        handler.Close()

        ' Signal the main thread to continue.
        allDone.Set()
    End Sub 'SendCallback

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Al caricamento del form

        ' Rileva l'indirizzo IP del computer
        IPAddress = GetIPAddress()

        ' Se non è stato possibile rilevare l'indirizzo IP del computer
        If IPAddress.Equals(IPAddress.None) Then
            MsgBox("Can't retrieve the correct PC's IP address")
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ' Stop the main thread and the server
        StopServer = True
        allDone.Set()
        Button1.Enabled = True
    End Sub

    Public Sub AppendTextBox(value As String)
        ' Permits to write text on the TextBox1 from another thread
        If InvokeRequired Then
            Invoke(New Action(Of String)(AddressOf AppendTextBox), New Object() {value})
            Return
        End If
        TextBox1.Text += value
    End Sub

    Public Sub ResetTextBox()
        ' Permits to write text on the TextBox1 from another thread
        If InvokeRequired Then
            Invoke(New Action(Of String)(AddressOf ResetTextBox), New Object() {""})
            Return
        End If
        TextBox1.ResetText()
    End Sub

End Class

' State object for reading client data asynchronously
Public Class StateObject
    ' Client  socket.
    Public workSocket As Socket = Nothing
    ' Size of receive buffer.
    Public Const BufferSize As Integer = 1024
    ' Receive buffer.
    Public buffer(BufferSize) As Byte
    ' Received data string.
    Public sb As New StringBuilder
End Class 'StateObject
