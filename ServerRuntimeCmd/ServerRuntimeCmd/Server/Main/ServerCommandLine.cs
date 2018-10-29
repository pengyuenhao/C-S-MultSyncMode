using Runtime.CmdLine;
using Runtime.Util.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerRuntimeCmd.Server.Main
{
    public class ServerCommandLine
    {
        public static ServerCommandLine Instance { get { return SingletonControl.GetInstance<ServerCommandLine>(); } }

        public void Init()
        {
            CommandLineArgumentProxy.Instance.ArgumentParserEvent += IsQuitArgument;

        }

        void IsQuitArgument(CommandLineArgumentParser arguments,ref int valid)
        {
            if (arguments.StartWith("/quit"))
            {
                Console.WriteLine("退出程序");
                Environment.Exit(0);
                valid += 1;
                return;
            }
            else
            {
                return;
            }
        }
    }
}
