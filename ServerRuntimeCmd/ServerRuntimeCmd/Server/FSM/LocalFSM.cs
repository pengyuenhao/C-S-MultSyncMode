using Runtime.Entity;
using Runtime.Main;
using Runtime.Time;
using Runtime.Util;
using ServerRuntimeCmd.Server.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.RuntimeFSM
{
    public class LocalFSM
    {
        public FiniteStateMachines<LocalRuntimeState> FSM;

        RuntimeState<LocalRuntimeState> InitState = new RuntimeState<LocalRuntimeState>(LocalRuntimeState.Init);
        RuntimeState<LocalRuntimeState> StartState = new RuntimeState<LocalRuntimeState>(LocalRuntimeState.Start);
        RuntimeState<LocalRuntimeState> GetInitDataState = new RuntimeState<LocalRuntimeState>(LocalRuntimeState.GetInitData);
        RuntimeState<LocalRuntimeState> GetFrameCmdState = new RuntimeState<LocalRuntimeState>(LocalRuntimeState.GetFrameCmd);
        RuntimeState<LocalRuntimeState> RunFrameCmdState = new RuntimeState<LocalRuntimeState>(LocalRuntimeState.RunFrameCmd);
        RuntimeState<LocalRuntimeState> UpdateDataModelState = new RuntimeState<LocalRuntimeState>(LocalRuntimeState.UpdateDataModel);

        public void Init()
        {
            InitStateFunc();
            StartStateFunc();
            GetInitDataStateFunc();
            GetFrameCmdStateFunc();
            RunFrameCmdStateFunc();
            UpdateDataModelStateFunc();

            //创建状态机
            FSM = new FiniteStateMachines<LocalRuntimeState>();
            RuntimeState<LocalRuntimeState> state;
            if (RuntimeStateManager.Instance.Get(LocalRuntimeState.Init, out state))
            {
                Console.WriteLine("[启动Local状态机系统]");
                FSM.StartupState(state);
            }
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
                FrameControl.Instance.isHost = true;
                Console.WriteLine("[Local状态机执行初始化]");
            });
        }


        void StartStateFunc()
        {
            //启动状态时执行的方法
            StartState.TransitionMap.Add(GetInitDataState, (deltaTime) =>
            {
                return true;
            });
            StartState.AddMotion((deltaTime) =>
            {
                Console.WriteLine("[Local状态机执行启动状态]");
            });
        }

        void GetInitDataStateFunc()
        {
            GetInitDataState.TransitionMap.Add(GetFrameCmdState, (deltaTime) =>
            {
                return true;
            });
            GetInitDataState.AddMotion((deltaTime) =>
            {
                //执行初始化
                ControlModel.FrameCtrl.Init();
                ControlModel.FrameCtrl.isInit = true;
                //启动运行帧
                ControlModel.FrameCtrl.isPasue = false;
                Console.WriteLine("[Local获取初始化数据]");
            });
        }

        void GetFrameCmdStateFunc()
        {
            //直接运行
            //GetFrameCmdState.isDelayRun = false;
            GetFrameCmdState.TransitionMap.Add(RunFrameCmdState, (deltaTime) =>
            {
                //将缓存数据转换为指令
                ControlModel.FrameCtrl.FromBufferToCmd();
                if (ControlModel.FrameCtrl.IsContainsKey(ControlModel.FrameCtrl.CurrentRunFrame))
                {
                    //Console.WriteLine("[Local成功获取指令数据]");
                    return true;
                }
                else
                {
                    //Console.WriteLine("[Local等待指令数据]" + ControlModel.FrameCtrl.CurrentRunFrame + "/" + ControlModel.FrameCtrl.MinFrame);
                    return false;
                }
            });
            GetFrameCmdState.AddMotion((deltaTime) =>
            {
                //Console.WriteLine("[Local正在获取指令数据]");
            });
        }

        void RunFrameCmdStateFunc()
        {
            RunFrameCmdState.isDelayRun = false;
            RunFrameCmdState.TransitionMap.Add(UpdateDataModelState, (deltaTime) =>
            {
                return true;
            });
            RunFrameCmdState.AddMotion((deltaTime) =>
            {
                //运行指令
                int time = ControlModel.FrameCtrl.RunCmd();
                //运行数据模型更新
                if (time > 0 && FrameControl.Instance.CurrentRunFrame % 100==0)
                {
                    //ControlModel.EntityCtrl.UpdateAllEntityModelData(time);
                    //Console.WriteLine("<<时间经过有效>>" + time);
                    //创建运行缓存
                    EntityContent.Instance.UpdateEntityDataCache(FrameControl.Instance.CurrentRunFrame,FrameControl.Instance.LastUpdateTime);
                }
                else
                {
                    //Console.WriteLine("<<时间经过无效>>" + time);
                }

                //Console.WriteLine("[Local运行指令数据]");
            });
        }

        void UpdateDataModelStateFunc()
        {
            //直接运行
            UpdateDataModelState.isDelayRun = false;
            UpdateDataModelState.TransitionMap.Add(GetFrameCmdState, (deltaTime) =>
            {
                return true;
            });
            UpdateDataModelState.AddMotion((deltaTime) =>
            {
                //保存数据模型到硬盘
                ControlModel.EntityCtrl.SaveAllEntityModelData();
                //Console.WriteLine("[Local更新数据模型]");
            });
        }
    }
    //运行本地服务时执行的状态机
    public enum LocalRuntimeState
    {
        //未定义
        Default,
        //初始化
        Init,
        //启动状态
        Start,
        //获取初始化数据
        GetInitData,
        //获取指令帧
        GetFrameCmd,
        //运行帧指令
        RunFrameCmd,
        //更新数据模型
        UpdateDataModel,
    }
}
