using Runtime.Main;
using Runtime.Util;
using ServerRuntimeCmd.Server.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerRuntimeCmd.Server.FSM
{
    public class ServerFSM
    {
        public FiniteStateMachines<ServerRuntimeState> FSM;

        ServerControl ServerCtrl { get { return ServerControl.Instance; } }
        FrameControl FrameCtrl { get { return ControlModel.FrameCtrl; } }

        RuntimeState<ServerRuntimeState> InitState = new RuntimeState<ServerRuntimeState>(ServerRuntimeState.Init);
        RuntimeState<ServerRuntimeState> StartState = new RuntimeState<ServerRuntimeState>(ServerRuntimeState.Start);
        RuntimeState<ServerRuntimeState> UpdateState = new RuntimeState<ServerRuntimeState>(ServerRuntimeState.Update);



        public void Init()
        {
            InitStateFunc();
            StartStateFunc();
            UpdateStateFunc();

            //创建状态机
            FSM = new FiniteStateMachines<ServerRuntimeState>();
            FSM.SwitchState(InitState);
            FSM.AddMotion((int deltaTime) => 
            {
                ServerCtrl.Update();
                //更新网络节点
                ServerCtrl.CheckNodeVerityMapDic();
            });
        }

        void InitStateFunc()
        {
            //初始化状态时执行的方法
            InitState.TransitionMap.Add(StartState, (deltaTime) =>
            {
                return true;
            });
            InitState.AddMotion((deltaTime) =>
            {
                //执行初始化
                ServerCtrl.Init();
            });
        }


        void StartStateFunc()
        {
            //启动状态时执行的方法
            StartState.TransitionMap.Add(UpdateState, (deltaTime) =>
            {
                if (FrameCtrl.isInit)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            });
            StartState.AddMotion((deltaTime) =>
            {
                Console.WriteLine("[Server状态机执行启动状态]");
            });
        }

        void UpdateStateFunc()
        {
            UpdateState.cycleTick = -1;
            UpdateState.AddMotion((deltaTime) =>
            {
                //广播服务端信息
                ServerCtrl.BroadcastServerInfo();
                //收集客户端数据
                ServerCtrl.CheckReceiveCmdStack();
                ServerCtrl.CollectCmdServer();
                ServerCtrl.RemoveOverMaxQueue();
            });
        }
    }
    public enum ServerRuntimeState
    {
        //初始化
        Init,
        //启动状态
        Start,
        //更新
        Update,
    }
}
