using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using BatchMonitor.Models;

namespace BatchMonitor.Services
{
    public class PowerShellSchedulerService
    {
        public void ScheduleBatch(string batchName, string batchPath, DateTime startTime)
        {
            ScheduleBatch(batchName, batchPath, startTime, BatchType.FixedTime);
        }

        public void ScheduleBatch(string batchName, string batchPath, DateTime startTime, BatchType batchType)
        {
            try
            {
                var taskName = $"{batchName}";
                var time = startTime.ToString("HH:mm");
                
                // Create PowerShell command to schedule the task
                var command = new StringBuilder();
                command.AppendLine($"$taskName = '{taskName}'");
                command.AppendLine($"$batchPath = '{batchPath}'");
                command.AppendLine($"$time = '{time}'");
                command.AppendLine();
                
                if (batchPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    // For PowerShell files
                    command.AppendLine("$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument \"-ExecutionPolicy Bypass -File `\"$batchPath`\"\"");
                }
                else
                {
                    // For batch files
                    command.AppendLine("$action = New-ScheduledTaskAction -Execute $batchPath");
                }
                
                // Configure trigger based on batch type
                if (batchType == BatchType.Hourly)
                {
                    command.AppendLine("$trigger = New-ScheduledTaskTrigger -Once -At $time -RepetitionInterval (New-TimeSpan -Hours 1) -RepetitionDuration (New-TimeSpan -Days 1)");
                }
                else
                {
                    command.AppendLine("$trigger = New-ScheduledTaskTrigger -Daily -At $time");
                }
                
                command.AppendLine("$settings = New-ScheduledTaskSettingsSet -WakeToRun -StartWhenAvailable -AllowStartIfOnBatteries");
                
                // Use current user instead of SYSTEM to avoid permission issues
                var currentUser = Environment.UserName;
                var currentDomain = Environment.UserDomainName;
                command.AppendLine($"$principal = New-ScheduledTaskPrincipal -UserId '{currentDomain}\\{currentUser}' -LogonType Interactive");
                
                // Register task with error handling and verification
                command.AppendLine("try {");
                command.AppendLine("    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue");
                command.AppendLine("    $task = Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force");
                command.AppendLine("} catch {");
                command.AppendLine("    # Error will be handled by verification after execution");
                command.AppendLine("}");

                // Execute the PowerShell command with elevated permissions
                ExecutePowerShellCommand(command.ToString());
                
                // Since we can't capture output with runas, we'll verify success by checking if the task exists
                // Wait a moment for the task to be registered
                System.Threading.Thread.Sleep(2000);
                
                // Verify the task was created successfully
                if (!IsScheduled(batchName))
                {
                    throw new Exception("Task was not successfully registered in Windows Task Scheduler. Please ensure the application is running with administrator privileges.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to schedule batch: {ex.Message}");
            }
        }

        public void UpdateSchedule(string batchName, DateTime newStartTime)
        {
            try
            {
                var taskName = $"{batchName}";
                var time = newStartTime.ToString("HH:mm");
                
                var command = new StringBuilder();
                command.AppendLine($"$taskName = '{taskName}'");
                command.AppendLine($"$time = '{time}'");
                command.AppendLine();
                command.AppendLine("try {");
                command.AppendLine("    $task = Get-ScheduledTask -TaskName $taskName");
                command.AppendLine("    $task.Triggers[0].StartBoundary = (Get-Date).Date.AddHours([int]$time.Split(':')[0]).AddMinutes([int]$time.Split(':')[1]).ToString('yyyy-MM-ddTHH:mm:ss')");
                command.AppendLine("    Set-ScheduledTask -InputObject $task");
                command.AppendLine("    Write-Output 'Schedule updated successfully'");
                command.AppendLine("} catch {");
                command.AppendLine("    Write-Error $_.Exception.Message");
                command.AppendLine("}");

                ExecutePowerShellCommand(command.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update schedule: {ex.Message}");
            }
        }

        public void DeleteSchedule(string batchName)
        {
            try
            {
                var taskName = $"{batchName}";
                
                var command = $@"
                    try {{
                        Unregister-ScheduledTask -TaskName '{taskName}' -Confirm:$false
                        Write-Output 'Task deleted successfully'
                    }} catch {{
                        Write-Error $_.Exception.Message
                    }}
                ";

                ExecutePowerShellCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete schedule: {ex.Message}");
            }
        }

        public DateTime? GetNextRunTime(string batchName)
         {
            try
            {
                var taskName = $"{batchName}";
                
                var command = $@"
                    try {{
                        $task = Get-ScheduledTask -TaskName '{taskName}' -ErrorAction Stop
                        $taskInfo = Get-ScheduledTaskInfo -TaskName '{taskName}' -ErrorAction Stop
                        if ($taskInfo.NextRunTime) {{
                            $taskInfo.NextRunTime.ToString('yyyy-MM-ddTHH:mm:ss')
                        }} else {{
                            'No NextRunTime'
                        }}
                    }} catch {{
                        'Task not found'
                    }}
                ";

                var result = ExecutePowerShellCommandNonElevated(command);
                
                // Debug output
                System.Diagnostics.Debug.WriteLine($"GetNextRunTime for '{taskName}': Result='{result?.Trim()}'");
                
                if (DateTime.TryParse(result?.Trim(), out var nextRun))
                {
                    return nextRun;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetNextRunTime error for '{batchName}': {ex.Message}");
                return null;
            }
        }

        public bool IsScheduled(string batchName)
        {
            try
            {
                var taskName = $"{batchName}";
                
                var command = $@"
                    try {{
                        $task = Get-ScheduledTask -TaskName '{taskName}' -ErrorAction Stop
                        if ($task.State -eq 'Ready') {{
                            'True'
                        }} else {{
                            'False'
                        }}
                    }} catch {{
                        'False'
                    }}
                ";

                var result = ExecutePowerShellCommandNonElevated(command);
                var isScheduled = result?.Trim().Equals("True", StringComparison.OrdinalIgnoreCase) == true;
                
                // Debug output
                System.Diagnostics.Debug.WriteLine($"IsScheduled check for '{taskName}': Result='{result?.Trim()}', IsScheduled={isScheduled}");
                
                return isScheduled;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsScheduled error for '{batchName}': {ex.Message}");
                return false;
            }
        }

        private string? ExecutePowerShellCommand(string command)
        {
            try
            {
                // Create a temporary PowerShell script file
                var tempScriptPath = Path.Combine(Path.GetTempPath(), $"BatchMonitor_{Guid.NewGuid()}.ps1");
                
                // Write the command to the temporary script file
                File.WriteAllText(tempScriptPath, command);
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas", // Run as administrator
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                
                // Clean up the temporary script file
                try
                {
                    File.Delete(tempScriptPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
                
                // Since we can't capture output with runas, we'll return a success indicator
                // The actual verification will be done by checking if the task exists
                return "Command executed";
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute PowerShell command: {ex.Message}");
            }
        }

        private string? ExecutePowerShellCommandNonElevated(string command)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -Command \"{command.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                
                var output = process?.StandardOutput.ReadToEnd();
                var error = process?.StandardError.ReadToEnd();
                
                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception($"PowerShell error: {error}");
                }
                
                return output;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute PowerShell command: {ex.Message}");
            }
        }
    }
}
