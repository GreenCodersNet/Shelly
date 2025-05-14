' ###  SecureStorage.vb - v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Security.Cryptography
Imports System.Text

Public Module SecureStorage
    ' Encrypts and stores the API Key securely
    Public Sub SaveApiKey(UserApiKey As String)
        Try
            Dim encryptedKey As String = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(UserApiKey), Nothing, DataProtectionScope.CurrentUser))
            My.Settings.UserApiKey = encryptedKey
            My.Settings.Save()
        Catch ex As Exception
            Debug.WriteLine("Error encrypting API key: " & ex.Message)
        End Try
    End Sub

    ' Retrieves and decrypts the API Key
    Public Function GetApiKey() As String
        Try
            Dim encryptedKey As String = My.Settings.UserApiKey
            If String.IsNullOrEmpty(encryptedKey) Then Return String.Empty ' Handle empty key
            Dim decryptedKey As String = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encryptedKey), Nothing, DataProtectionScope.CurrentUser))
            Return decryptedKey
        Catch ex As Exception
            Debug.WriteLine("Error decrypting API key: " & ex.Message)
            Return String.Empty
        End Try
    End Function
End Module
