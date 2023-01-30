﻿using CommandLine;
using Rubberduck.RPC.Platform;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Rubberduck.Server.LocalDb
{
    internal static class Program
    {
        private static TimeSpan _exclusiveAccessTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static async Task<int> Main(string[] args)
        {
            #region Global Mutex https://stackoverflow.com/a/229567/1188513

            // get application GUID as defined in AssemblyInfo.cs
            var appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(GuidAttribute), false)
                .GetValue(0)).Value
                .ToString();

            // unique id for global mutex - Global prefix means it is global to the machine
            var mutexId = $"Global\\{{{appGuid}}}";

            // edited by Jeremy Wiebe to add example of setting up security for multi-user usage
            // edited by 'Marc' to work also on localized systems (don't use just "Everyone") 
            var allowEveryoneRule = new MutexAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                MutexRights.FullControl,
                AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            // edited by MasonGZhwiti to prevent race condition on security settings via VanNguyen
            using (var mutex = new Mutex(false, mutexId, out _, securitySettings))
            {
                // edited by acidzombie24
                var hasHandle = false;
                try
                {
                    try
                    {
                        // edited by acidzombie24
                        hasHandle = mutex.WaitOne(_exclusiveAccessTimeout.Milliseconds, false);
                        if (!hasHandle) throw new TimeoutException("Timeout waiting for exclusive access");
                    }
                    catch (AbandonedMutexException)
                    {
                        // Log the fact that the mutex was abandoned in another process,
                        // it will still get acquired
                        hasHandle = true;
                    }

                    var startupArgs = Parser.Default.ParseArguments<StartupOptions>(args)
                        .WithNotParsed(errors =>
                        {
                            Console.WriteLine("Errors have occurred processing command-line arguments. Server will not be started.");
                        })
                        .WithParsed(options =>
                        {
                            Console.WriteLine("Command-line arguments successfully parsed. Proceeding with startup...");
                        });

                    if (startupArgs.Errors.Any())
                    {
                        throw new ArgumentException("Invalid command-line arguments were supplied.");
                    }

                    Console.WriteLine("Startup checks completed. Starting server application...");
                    try
                    {
                        await StartAsync(startupArgs.Value);
                    }
                    catch (OperationCanceledException)
                    {
                        // normal exit
                        return ExitCode(RpcServerProcessExitCode.OK);
                    }
                    catch (Exception exception)
                    {
                        // any other exception type exits with an error code
                        await Console.Error.WriteLineAsync(exception.ToString());
                        return ExitCode(RpcServerProcessExitCode.Error);
                    }
                }
                finally
                {
                    // edited by acidzombie24, added if statement
                    if (hasHandle)
                    {
                        mutex.ReleaseMutex();
                    }
                }

                // not a normal exit
                return ExitCode(RpcServerProcessExitCode.Error);
            }
            #endregion
        }

        private static int ExitCode(RpcServerProcessExitCode code)
        {
            return (int)code;
        }

        private static async Task StartAsync(StartupOptions startupOptions)
        {
            /* TODO resolve app from IoC container here? */
            var config = LocalDbServerCapabilities.GetDefaultConfiguration(startupOptions);
            var app = new App(config);
            await app.StartAsync();
        }
    }
}
