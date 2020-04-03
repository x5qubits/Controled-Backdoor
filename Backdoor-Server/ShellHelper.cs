using LLFU;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

public static class ShellHelper
{
    public static void Reboot()
    {
        if (!Program.IsExecuteCommands)
            return;

        if (Program.AllowReboot)
            Process.Start(new ProcessStartInfo() { FileName = "sudo", Arguments = "reboot" });
    }

    public static string ExecuteComand(this string cmd, bool isTask = false)
    {
        string result = "";

        if (!Program.IsExecuteCommands)
            return cmd.ToString();

        if (isTask)
        {
            Task.Factory.StartNew(() => {
                using (var process = new Process())
                {
                    try
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = "/bin/bash",
                            Arguments = "-c \"" + cmd + "\"",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            //CreateNoWindow = true,
                        };
                        process.Start();
                        result = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                    }
                    catch (Exception e) { result = e.ToString(); }
                }
            });
            return "";
        }

        using (var process = new Process())
        {
            try
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"" + cmd + "\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    //CreateNoWindow = true,
                };
                process.Start();
                result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e) { result = e.ToString(); }
        }
        return result;
    }

    public static List<string> GetLines(this string cmd)
    {
        List<string> result = new List<string>();

        if (!Program.IsExecuteCommands)
            return result;

        using (var process = new Process())
        {
            try
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = cmd,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    //CreateNoWindow = true,
                };
                process.Start();
                string line = "";
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    result.Add(line);
                }
                process.WaitForExit();
            }
            catch (Exception e) { result.Add(e.ToString()); }
        }
        return result;
    }
}