Imports System
Imports System.IO
Imports System.Linq
Imports System.Net.Http
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Newtonsoft.Json

Module UpdateHelper

    ''' <summary>
    ''' If a newer version is found, prompts, downloads ZIP, extracts EXE,
    ''' runs it elevated (single UAC), waits, then restarts and exits.
    ''' </summary>
    Public Async Function CheckForAndInstallUpdateAsync() As Task
        Try
            ' 1) Read local version (strip any "+" metadata)
            Dim localVerFull = Application.ProductVersion
            Dim localVer = localVerFull.Split("+"c)(0)

            Using client As New HttpClient()
                client.DefaultRequestHeaders.CacheControl =
            New Net.Http.Headers.CacheControlHeaderValue() With {.NoCache = True}

                ' 2) Fetch and parse your JSON manifest
                Dim manifestUrl =
            $"https://greencoders.net/dev/shelly/latest.json?cb={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
                Dim json = Await client.GetStringAsync(manifestUrl)
                Dim info = JsonConvert.DeserializeObject(Of RemoteUpdateInfo)(json)

                ' 3) Compare versions
                Dim remoteVer = New Version(info.Version)
                Dim currentVer = New Version(localVer)
                If remoteVer <= currentVer Then Return

                ' 4) Ask the user via your UpdateForm
                Dim prompt =
            $"A new version ({info.Version}) is available. You have {localVer}." & vbCrLf &
            "Download & install now?"
                Using uf As New UpdateForm()
                    uf.MessageText = prompt
                    ' ShowDialog returns DialogResult.Yes or .No
                    If uf.ShowDialog() <> DialogResult.Yes Then
                        Return
                    End If
                End Using

                ' 5) Download & extract the installer EXE from your ZIP
                Dim installerExe =
            Await DownloadAndExtractInstallerAsync(info.DownloadURL, info.Version)

                ' 6) Launch the installer elevated in a background thread
                Dim installArgs =
            $"/VERYSILENT /SUPPRESSMSGBOXES /NOCANCEL /DIR=""{Application.StartupPath}"""
                Dim runner As New Threading.Thread(Sub()
                                                       ' give this app time to exit and unlock its files
                                                       Threading.Thread.Sleep(500)
                                                       Try
                                                           Process.Start(New ProcessStartInfo(installerExe) With {
                                                       .Arguments = installArgs,
                                                       .UseShellExecute = True,
                                                       .Verb = "runas",                ' single UAC
                                                       .WindowStyle = ProcessWindowStyle.Hidden
                                                   })
                                                       Catch
                                                           ' swallow any errors here
                                                       End Try
                                                   End Sub)

                runner.IsBackground = False  ' keep it alive after Application.Exit()
                runner.Start()

                ' 7) Exit THIS app so the installer can overwrite it
                Application.Exit()
            End Using

        Catch ex As Exception
            ' You can keep using a MessageBox for errors, or create an ErrorForm similarly
            MessageBox.Show(
        $"Update failed: {ex.Message}",
        "Update Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error)
        End Try
    End Function


    ''' <summary>
    ''' Downloads the ZIP, extracts it to Temp\ShellyUpdate_{version},
    ''' and returns the contained EXE path.
    ''' </summary>
    Private Async Function DownloadAndExtractInstallerAsync(
        downloadUrl As String,
        version As String
      ) As Task(Of String)

        ' fetch with cache-buster
        Dim zipUrl = $"{downloadUrl}?cb={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
        Dim zipPath = Path.Combine(Path.GetTempPath(), $"Setup_Shelly_{version}.zip")

        Using client As New HttpClient()
            client.DefaultRequestHeaders.CacheControl =
              New Net.Http.Headers.CacheControlHeaderValue() With {.NoCache = True}
            Dim data = Await client.GetByteArrayAsync(zipUrl)
            File.WriteAllBytes(zipPath, data)
        End Using

        ' extract
        Dim extractDir = Path.Combine(Path.GetTempPath(), $"ShellyUpdate_{version}")
        If Directory.Exists(extractDir) Then Directory.Delete(extractDir, True)
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir)

        ' locate the EXE
        Dim exe = Directory.GetFiles(extractDir, "*.exe", SearchOption.TopDirectoryOnly) _
                      .FirstOrDefault()
        If exe Is Nothing Then
            Throw New FileNotFoundException($"No installer EXE found inside {zipPath}")
        End If
        Return exe
    End Function

    Private Class RemoteUpdateInfo
        Public Property Version As String
        Public Property DownloadURL As String
        Public Property Mandatory As Boolean
        Public Property ChangelogURL As String
    End Class

End Module
