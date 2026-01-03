Imports System.Threading

''' <summary>
''' Provides backward-compatible helper functions for local AI.
''' NOTE: Primary entry point is now LocalAISummaryService.Instance.GenerateFinalVoiceSummaryAsync
''' This module provides simple availability checks and direct TTS access.
''' </summary>
Public Module LocalAIPipeline

    ''' <summary>
    ''' Checks if Local AI is available and configured.
    ''' </summary>
    Public Function IsLocalAIAvailable() As Boolean
        Return LocalAISummaryService.Instance.IsLocalAIAvailable()
    End Function

    ''' <summary>
    ''' Checks if Piper TTS is available.
    ''' </summary>
    Public Function IsTTSAvailable() As Boolean
        Return LocalAISummaryService.Instance.IsTTSAvailable()
    End Function

    ''' <summary>
    ''' Stops any currently playing TTS audio.
    ''' </summary>
    Public Sub StopSpeaking()
        LocalAISummaryService.Instance.StopSpeaking()
    End Sub

End Module
