' ###  ToolSchemaValidator.vb - v2.0.0 ### 

' ##########################################################
'  Shelly - v2.0.0
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.IO
Imports System.Linq
Imports System.Collections.Generic

''' <summary>
''' Result of tool validation
''' </summary>
Public Class ToolValidationResult
    Public Property IsValid As Boolean
    Public Property Errors As New List(Of String)
    Public Property Warnings As New List(Of String)
    Public Property ToolName As String
    Public Property ValidatedArgs As Dictionary(Of String, Object)
    
    Public Sub New()
        ValidatedArgs = New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
    End Sub
    
    Public Sub AddError(message As String)
        Errors.Add(message)
        IsValid = False
    End Sub
    
    Public Sub AddWarning(message As String)
        Warnings.Add(message)
    End Sub
    
    Public Function GetSummary() As String
        Dim summary As New Text.StringBuilder()
        summary.AppendLine($"Tool: {ToolName}")
        summary.AppendLine($"Valid: {IsValid}")
        
        If Errors.Count > 0 Then
            summary.AppendLine("Errors:")
            For Each errorMsg In Errors
                summary.AppendLine($"  - {errorMsg}")
            Next
        End If
        
        If Warnings.Count > 0 Then
            summary.AppendLine("Warnings:")
            For Each warnMsg In Warnings
                summary.AppendLine($"  - {warnMsg}")
            Next
        End If
        
        Return summary.ToString()
    End Function
End Class

''' <summary>
''' Validates tool calls against their schemas
''' </summary>
Public Module ToolSchemaValidator
    
    ''' <summary>
    ''' Validate a single step (alias for ValidatePlanStep)
    ''' </summary>
    Public Function ValidateStep(planStep As PlanStep) As ToolValidationResult
        Return ValidatePlanStep(planStep)
    End Function
    
    ''' <summary>
    ''' Validate a plan step against its tool schema
    ''' </summary>
    Public Function ValidatePlanStep(planStep As PlanStep) As ToolValidationResult
        Dim result As New ToolValidationResult With {
            .ToolName = planStep.Tool,
            .IsValid = True
        }
        
        ' 1. Check if tool exists
        If Not ToolSchemaRegistry.IsValidTool(planStep.Tool) Then
            result.AddError($"Unknown tool: {planStep.Tool}")
            Return result
        End If
        
        Dim schema = ToolSchemaRegistry.GetSchema(planStep.Tool)
        
        ' 2. Validate each parameter
        For Each paramSchema In schema.Parameters
            Dim value As Object = Nothing
            Dim hasValue = planStep.Args.TryGetValue(paramSchema.Name, value)
            
            ' Check required parameters
            If paramSchema.IsRequired AndAlso Not hasValue Then
                result.AddError($"Missing required parameter: {paramSchema.Name}")
                Continue For
            End If
            
            If Not hasValue Then Continue For
            
            ' Type validation
            If Not ValidateType(value, paramSchema, result) Then
                Continue For
            End If
            
            ' Allowed values validation
            If paramSchema.AllowedValues IsNot Nothing AndAlso paramSchema.AllowedValues.Count > 0 Then
                Dim strValue = value.ToString().ToLowerInvariant()
                If Not paramSchema.AllowedValues.Any(Function(av) av.ToLowerInvariant() = strValue) Then
                    result.AddError($"Parameter '{paramSchema.Name}' must be one of: {String.Join(", ", paramSchema.AllowedValues)}")
                    Continue For
                End If
            End If
            
            ' Range validation for numeric types
            If IsNumericType(paramSchema.Type) Then
                If Not ValidateNumericRange(value, paramSchema, result) Then
                    Continue For
                End If
            End If
            
            ' Path validation
            If paramSchema.MustBeValidPath Then
                If Not ValidatePath(value.ToString(), paramSchema, result) Then
                    Continue For
                End If
            End If
            
            ' Store validated value
            result.ValidatedArgs(paramSchema.Name) = value
        Next
        
        ' 3. Check for unexpected parameters
        For Each kvp In planStep.Args
            If Not schema.Parameters.Any(Function(p) p.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)) Then
                result.AddWarning($"Unexpected parameter: {kvp.Key}")
            End If
        Next
        
        Return result
    End Function
    
    ''' <summary>
    ''' Validate type compatibility
    ''' </summary>
    Private Function ValidateType(value As Object, paramSchema As ToolParameterSchema, result As ToolValidationResult) As Boolean
        Try
            If value Is Nothing Then
                result.AddError($"Parameter '{paramSchema.Name}' cannot be null")
                Return False
            End If
            
            Dim valueType = value.GetType()
            
            ' Handle string type
            If paramSchema.Type Is GetType(String) Then
                If TypeOf value IsNot String Then
                    ' Try to convert to string
                    value = value.ToString()
                End If
                Return True
            End If
            
            ' Handle integer type
            If paramSchema.Type Is GetType(Integer) Then
                If TypeOf value Is Integer Then
                    Return True
                ElseIf IsNumeric(value) Then
                    ' Try to convert
                    Dim intVal As Integer
                    If Integer.TryParse(value.ToString(), intVal) Then
                        Return True
                    End If
                End If
                result.AddError($"Parameter '{paramSchema.Name}' must be an integer")
                Return False
            End If
            
            ' Handle boolean type
            If paramSchema.Type Is GetType(Boolean) Then
                If TypeOf value Is Boolean Then
                    Return True
                End If
                
                Dim strVal = value.ToString().ToLowerInvariant()
                If strVal = "true" Or strVal = "false" Then
                    Return True
                End If
                
                result.AddError($"Parameter '{paramSchema.Name}' must be a boolean (true/false)")
                Return False
            End If
            
            ' Default: check if types match
            If Not paramSchema.Type.IsAssignableFrom(valueType) Then
                result.AddError($"Parameter '{paramSchema.Name}' must be of type {paramSchema.Type.Name}")
                Return False
            End If
            
            Return True
            
        Catch ex As Exception
            result.AddError($"Type validation failed for parameter '{paramSchema.Name}': {ex.Message}")
            Return False
        End Try
    End Function
    
    ''' <summary>
    ''' Validate numeric ranges
    ''' </summary>
    Private Function ValidateNumericRange(value As Object, paramSchema As ToolParameterSchema, result As ToolValidationResult) As Boolean
        Try
            Dim numValue As Double = Convert.ToDouble(value)
            
            If paramSchema.MinValue IsNot Nothing Then
                Dim minValue As Double = Convert.ToDouble(paramSchema.MinValue)
                If numValue < minValue Then
                    result.AddError($"Parameter '{paramSchema.Name}' must be >= {minValue}")
                    Return False
                End If
            End If
            
            If paramSchema.MaxValue IsNot Nothing Then
                Dim maxValue As Double = Convert.ToDouble(paramSchema.MaxValue)
                If numValue > maxValue Then
                    result.AddError($"Parameter '{paramSchema.Name}' must be <= {maxValue}")
                    Return False
                End If
            End If
            
            Return True
            
        Catch ex As Exception
            result.AddError($"Range validation failed for parameter '{paramSchema.Name}': {ex.Message}")
            Return False
        End Try
    End Function
    
    ''' <summary>
    ''' Validate file/folder paths
    ''' </summary>
    Private Function ValidatePath(pathStr As String, paramSchema As ToolParameterSchema, result As ToolValidationResult) As Boolean
        If String.IsNullOrWhiteSpace(pathStr) Then
            result.AddError($"Parameter '{paramSchema.Name}' path cannot be empty")
            Return False
        End If
        
        ' Check for invalid path characters
        Dim invalidChars = IO.Path.GetInvalidPathChars()
        If pathStr.Any(Function(c) invalidChars.Contains(c)) Then
            result.AddError($"Parameter '{paramSchema.Name}' contains invalid path characters")
            Return False
        End If
        
        ' Check if path must exist
        If paramSchema.PathMustExist Then
            If Not File.Exists(pathStr) AndAlso Not Directory.Exists(pathStr) Then
                result.AddError($"Path does not exist: {pathStr}")
                Return False
            End If
        End If
        
        ' Security: Check for restricted paths
        Dim normalizedPath = IO.Path.GetFullPath(pathStr).ToLowerInvariant()
        
        Dim restrictedPaths As String() = {
            "c:\windows",
            "c:\program files",
            "c:\programdata",
            "c:\system32"
        }
        
        For Each restricted In restrictedPaths
            If normalizedPath.StartsWith(restricted) Then
                result.AddWarning($"Path accesses system folder: {pathStr}")
                Exit For
            End If
        Next
        
        Return True
    End Function
    
    ''' <summary>
    ''' Check if a type is numeric
    ''' </summary>
    Private Function IsNumericType(type As Type) As Boolean
        Return type Is GetType(Integer) OrElse
               type Is GetType(Long) OrElse
               type Is GetType(Double) OrElse
               type Is GetType(Single) OrElse
               type Is GetType(Decimal)
    End Function
    
    ''' <summary>
    ''' Validate an entire plan before execution
    ''' </summary>
    Public Function ValidatePlan(plan As List(Of PlanStep)) As List(Of ToolValidationResult)
        Dim results As New List(Of ToolValidationResult)
        
        If plan Is Nothing OrElse plan.Count = 0 Then
            Dim emptyResult As New ToolValidationResult With {
                .ToolName = "Plan"
            }
            emptyResult.AddError("Plan is empty")
            results.Add(emptyResult)
            Return results
        End If
        
        For Each planStep In plan
            Dim validation = ValidatePlanStep(planStep)
            results.Add(validation)
        Next
        
        Return results
    End Function
    
    ''' <summary>
    ''' Check if all validation results are valid
    ''' </summary>
    Public Function AllValid(results As List(Of ToolValidationResult)) As Boolean
        Return results IsNot Nothing AndAlso results.All(Function(r) r.IsValid)
    End Function
    
    ''' <summary>
    ''' Get summary of all validation errors
    ''' </summary>
    Public Function GetValidationSummary(results As List(Of ToolValidationResult)) As String
        Dim summary As New Text.StringBuilder()
        summary.AppendLine("=== Plan Validation Summary ===")
        
        Dim validCount = results.Where(Function(r) r.IsValid).Count()
        summary.AppendLine($"Valid: {validCount}/{results.Count}")
        summary.AppendLine()
        
        For i = 0 To results.Count - 1
            Dim r = results(i)
            If Not r.IsValid Then
                summary.AppendLine($"Step {i + 1}: {r.ToolName}")
                For Each errorMsg In r.Errors
                    summary.AppendLine($"  ERROR: {errorMsg}")
                Next
                summary.AppendLine()
            End If
        Next
        
        Return summary.ToString()
    End Function
    
    ' ...existing helper methods remain the same (ValidateType, ValidateNumericRange, IsNumericType)...
    
End Module
