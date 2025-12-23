' ###  Strategist.vb - v1.0.0 ###
' Goal Decomposition Engine - Breaks complex requests into achievable sub-goals

Imports Newtonsoft.Json
Imports System.Threading
Imports System.Text.RegularExpressions

Public Class SubGoal
    Public Property GoalID As String = Guid.NewGuid().ToString()
    Public Property Description As String
    Public Property SuccessCriteria As String
    Public Property Dependencies As New List(Of String)
    Public Property Status As GoalStatus = GoalStatus.Pending
    Public Property AttemptedSteps As New List(Of PlanStep)
    Public Property Outcome As ExecutionOutcome
    Public Property AdaptiveStrategy As String
    Public Property Priority As Integer = 0
End Class

Public Enum GoalStatus
    Pending
    InProgress
    Succeeded
    FailedRetryable
    FailedTerminal
End Enum

Public Class Strategist
    ''' <summary>
    ''' Decomposes user request into logical sub-goals with dependencies
    ''' </summary>
    Public Shared Async Function DecomposeRequest(
        userRequest As String,
        ct As CancellationToken
    ) As Task(Of List(Of SubGoal))
        
        Dim decompositionPrompt = $"
You are a strategic task planner. Break down the following user request into logical sub-goals.

USER REQUEST:
{userRequest}

AVAILABLE TOOLS:
- Custom Functions: ReadFileAndAnswer, ImageAnswer, GenerateImages, SearchForTextInsideFiles, etc.
- PowerShell: For file system operations, data retrieval
- FreeResponse: For text-only responses

TASK:
1. Identify ALL distinct actions required
2. Break into sub-goals with clear success criteria
3. Identify dependencies (which goals must complete before others)
4. Assign priority (1=highest)

Return JSON array of goals:
[
  {{
    ""description"": ""Find all files containing keyword"",
    ""successCriteria"": ""List of file paths obtained"",
    ""dependencies"": [],
    ""priority"": 1,
    ""adaptiveStrategy"": ""If no files found, notify user and stop""
  }},
  {{
    ""description"": ""Analyze each found file"",
    ""successCriteria"": ""Each file has analysis result"",
    ""dependencies"": [""Find all files containing keyword""],
    ""priority"": 2,
    ""adaptiveStrategy"": ""Process each file individually, skip corrupted ones""
  }}
]

CRITICAL:
- One goal per distinct action
- Clear, measurable success criteria
- Explicit dependencies
- Adaptive strategy for failures
"
        
        Try
            Dim messages = New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"},
                    {"content", "You are a strategic task decomposition expert. Return ONLY valid JSON."}
                },
                New Dictionary(Of String, String) From {
                    {"role", "user"},
                    {"content", decompositionPrompt}
                }
            }
            
            Dim response = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                messages,
                0.3, ' Lower temperature for structured output
                ct
            )
            
            ' Extract JSON from response
            Dim jsonMatch = Regex.Match(response, "\[[\s\S]*\]")
            If Not jsonMatch.Success Then
                Throw New Exception("Failed to extract JSON from strategist response")
            End If
            
            ' Parse goals
            Dim goalsJson = jsonMatch.Value
            Dim goalDefs = JsonConvert.DeserializeObject(Of List(Of Dictionary(Of String, Object)))(goalsJson)
            
            Dim goals = New List(Of SubGoal)()
            For Each goalDef In goalDefs
                Dim goal As New SubGoal With {
                    .Description = goalDef("description").ToString(),
                    .SuccessCriteria = goalDef("successCriteria").ToString(),
                    .Priority = Convert.ToInt32(goalDef("priority")),
                    .AdaptiveStrategy = goalDef("adaptiveStrategy").ToString()
                }
                
                ' Parse dependencies
                If goalDef.ContainsKey("dependencies") Then
                    Dim deps = TryCast(goalDef("dependencies"), Newtonsoft.Json.Linq.JArray)
                    If deps IsNot Nothing Then
                        For Each dep In deps
                            goal.Dependencies.Add(dep.ToString())
                        Next
                    End If
                End If
                
                goals.Add(goal)
            Next
            
            ' Sort by priority
            goals = goals.OrderBy(Function(g) g.Priority).ToList()
            
            Debug.WriteLine($"[Strategist] Decomposed request into {goals.Count} goals")
            For Each goal In goals
                Debug.WriteLine($"  Goal: {goal.Description}")
                Debug.WriteLine($"    Success: {goal.SuccessCriteria}")
                Debug.WriteLine($"    Dependencies: {String.Join(", ", goal.Dependencies)}")
            Next
            
            Return goals
            
        Catch ex As Exception
            Debug.WriteLine($"[Strategist ERROR] {ex.Message}")
            
            ' Fallback: Create single goal
            Return New List(Of SubGoal) From {
                New SubGoal With {
                    .Description = "Complete user request",
                    .SuccessCriteria = "User request fully satisfied",
                    .Priority = 1,
                    .AdaptiveStrategy = "Try different approaches if first fails"
                }
            }
        End Try
    End Function
    
    ''' <summary>
    ''' Validates goal dependencies are satisfied
    ''' </summary>
    Public Shared Function CanExecuteGoal(
        goal As SubGoal,
        completedGoals As List(Of SubGoal)
    ) As Boolean
        
        If goal.Dependencies.Count = 0 Then
            Return True
        End If
        
        For Each depDescription In goal.Dependencies
            Dim depGoal = completedGoals.FirstOrDefault(
                Function(g) g.Description.Equals(depDescription, StringComparison.OrdinalIgnoreCase)
            )
            
            If depGoal Is Nothing OrElse depGoal.Status <> GoalStatus.Succeeded Then
                Return False
            End If
        Next
        
        Return True
    End Function
End Class
