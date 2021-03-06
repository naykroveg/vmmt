﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VMManagementTool.Services
{
    static class SystemUtils
    {
        const string RESTART_SHTASK_NAME = "VMManagementTool.Restart";
        public static void ScheduleAfterRestart()
        {
            DeleteResumeTask();
            var myExePath = Assembly.GetEntryAssembly().Location;
            var workDirPath = Path.GetDirectoryName(myExePath);
            var psExecExecutable = Environment.Is64BitOperatingSystem ? "PsExec64.exe" : "PsExec.exe";
            var psExecPath = Path.GetFullPath(Path.Combine(Configs.TOOLS_DIR, "PsExec", psExecExecutable));
            var restartCmd = $"\"{psExecPath}\" -d -accepteula -i 1 -w \"{workDirPath}\" \"{myExePath}\" /resume";
            var restartBatchFilePath = Path.GetFullPath("restart.bat");
            File.WriteAllText(restartBatchFilePath, restartCmd);
            //an alternative to task scheduler could be Run/ RunOnce registy keys
            //var schCmd = $"schtasks.exe /create /tn \"{RESTART_SHTASK_NAME}\" /ru SYSTEM /sc ONLOGON /tr \"'{psExecPath}' -d -accepteula -i 1 -w '{workDirPath}' '{myExePath}' '/resume'\"";
            var schCmd = $"schtasks.exe /create /tn \"{RESTART_SHTASK_NAME}\" /ru SYSTEM /sc ONLOGON /tr \"'{restartBatchFilePath}'\"";
            
            var cmd = new ShellCommand(schCmd);
            if (!cmd.TryExecute(out _))
            {
                //throw new Exception("Unable to schedule after restart resume");
                Log.Error("SystemUtils.ScheduleAfterRestart", "Unable to schedule resume task");
            }
        }

        public static void DeleteResumeTask()
        {
            var schCmd = $"schtasks.exe /delete /tn \"{RESTART_SHTASK_NAME}\" /f";
            var cmd = new ShellCommand(schCmd);
            if (!cmd.TryExecute(out _))
            {
                //Log.Error("SystemUtils.DeleteResumeTask", "Unable to delete resume task");
            }

            File.Delete("restart.bat");
        }

        public static void RestartSystem()
        {
            var cmdStr = "shutdown /r";
            var cmd = new ShellCommand(cmdStr);
            if (!cmd.TryExecute(out _))
            {
                Log.Error("SystemUtils.RestartSystem", "Unable to perform PC restart");
            }
        }

        public static string GetSystem32Path(bool avoidRedirect) 
        {
            
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess && avoidRedirect)
            {
                return Environment.ExpandEnvironmentVariables("%windir%\\sysnative");
            }
            else
            {
                return Environment.ExpandEnvironmentVariables("%windir%\\system32");
            }
        }
    }
}
