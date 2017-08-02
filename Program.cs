using System;
using System.Collections.Generic;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Classes.Models;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
namespace InstaBatch_Follow
{
    class Program
    {
        static List<UserStruct> mUsers = new List<UserStruct>();
        private static string mPattern = @"([a-zA-Z0-9_\-\.]{3,40}):([a-zA-Z0-9_\-\.]{5,40})";
        private static int mDelay = 500;
        private static int mThreads = 1;
        private static string mTarget = "", mInput = "";
        static void Main(string[] args)
        {
            if (args.Length > 2)
            {
                for (var i = 0; i < args.Length - 2; i++)
                {
                    if (args[i].StartsWith("-delay="))
                    {
                        string delay = args[i].Substring(7);
                        int th = 0;
                        int.TryParse(delay, out th);
                        if (th != 0)
                           mDelay = th;
                    }

                }
            }
            else if (args.Length < 2)
            {
                Console.WriteLine("Usage : InstaBatchFollow [options] -username=Target input");
                Console.WriteLine("options : ");
                Console.WriteLine("-delay=500 \tDelay Between Requests (Bigger is better)");
                Console.WriteLine();
                Console.WriteLine("-username=UserID \tUser to Follow [long]");
                Console.WriteLine("input \tCombo File (Format : [username:password])");
                Console.WriteLine("*** WARNING ***");
                Console.WriteLine("Dont specify file path with whitespace or escape it with \"\"");
                Console.ReadKey();
                Environment.Exit(1);
            }
            mInput = args[args.Length - 1];
            if (args[args.Length - 2].Length < 12)
                Environment.Exit(-1);
            string target = args[args.Length-2].Substring(10);
            if (target.Equals(""))
            {
                Console.WriteLine("[Fatal Error] - Target ID is empty");
                Environment.Exit(1);

            }
            mTarget = target;
            long lTarget = 0;
            long.TryParse(mTarget,out lTarget); 
            if(lTarget == 0)
            {
                Console.WriteLine("[Fatal Error] - Target ID must be numeric");
                Environment.Exit(-1);
            }
            if (!File.Exists(mInput))
            {
                Console.WriteLine("[Fatal Error] - Source File Not Found");
                Environment.Exit(-1);
            }
            string[] rows = File.ReadAllLines(mInput);
            foreach (var r in rows)
            {
                if (Regex.IsMatch(r, mPattern))
                {
                    Match m = Regex.Match(r, mPattern);
                    mUsers.Add(new UserStruct(m.Groups[1].Value, m.Groups[2].Value));
                }
            }
            if (mUsers.Count == 0)
            {
                Console.WriteLine("[Fatal Error] - Source File is Empty");
                Environment.Exit(-1);
            }
            CheckAccount chk = new CheckAccount(mUsers,lTarget,mDelay);
            chk.Start();

            while (true)
            {
                if (Console.ReadLine() == "exit")
                {
                    break;
                }
            }
        }
    }
    class CheckAccount
    {
        private List<UserStruct> mUsers;
        long mTarget;
        int mDelay;
        int mThreads;
        public CheckAccount(List<UserStruct> users, long Target, int Delay)
        {
            mUsers = users;
            mDelay = Delay;
            mTarget = Target;
        }
        public async void Start()
        {
            foreach (var u in mUsers)
            {
                var userSession = new UserSessionData
                {
                    UserName = u.Username,
                    Password = u.Password
                };
                var api = new InstaApiBuilder()
                .SetUser(userSession)
                .Build();
                var logInResult = await api.LoginAsync();
                if (!logInResult.Succeeded)
                {
                    if (logInResult.Info.ResponseType == ResponseType.CheckPointRequired)
                    {
                        Console.WriteLine($"Unable to login: CheckPoint Required");
                    }
                    else if (logInResult.Info.ResponseType == ResponseType.Unknown)
                    {
                        Console.WriteLine($"Unable to login: {logInResult.Info.Message}");
                    }
                    else if (logInResult.Info.ResponseType == ResponseType.RequestsLimit)
                    {
                        Console.WriteLine($"Unable to login: Rate Limit");
                        Environment.Exit(-10);
                    }
                }
                else
                {
                    try
                    {
                        var user = await api.GetCurrentUserAsync();
                        var state = await api.FollowUserAsync(mTarget);          
                        if(state.Succeeded == true)
                        {
                            Console.WriteLine($"{user.Value.UserName} followed!");
                        }
                        else
                        {
                            Console.WriteLine("Failed To Follow :(");
                        }
                        await Task.Delay(mDelay); 
                    }
                    catch (Exception er)
                    {
                        
                    }
                }
                await Task.Delay(mDelay);

            }
            Environment.Exit(0);
        }
    }
}