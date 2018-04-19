﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WebSocketSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;

namespace HduRemoteLab
{
    /// <summary>
    /// OperateWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OperateWindow : Window
    {

        public delegate void ActivateGrid();
        public event ActivateGrid activateGrid;

        public WebSocket wsOperate;
        public string server;
        public string account_id;
        public string mode;
        public List<Slaves> slavesList;//设备信息
        public List<Experiments> experimentsList;//实验信息
        public Slaves selectSlaves;//选中的从机复杂类-接收自服务器
        public Slave selectSlave;//选中从机简单类-发送往服务器
        public string selectExperiment;//选中实验

        public OperateWindow(string server,string id)
        {
            this.server = server;
            this.account_id = id;
            InitializeComponent();
            GridOperate.IsEnabled = false;
            mode = "info";
            wsOperate = new WebSocket("ws://" + server + "/mode=" + mode);
            wsOperate.OnMessage += (s, ee) => {
                var recData = JsonConvert.DeserializeObject<SlavesData>(ee.Data);
                if (recData.code == "300")
                { 
                    slavesList = new List<Slaves>();
                    foreach (JObject i in recData.data)
                    {
                        var temp = JsonConvert.DeserializeObject<Slaves>(i.ToString());
                        if (temp.state == "free") slavesList.Add(temp);

                    }
                    //AppendLog("控制及实验获取完毕");

                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        ComboSlaves.ItemsSource = slavesList;
                        ComboSlaves.DisplayMemberPath = "name";
                    }));
                }
                else
                {
                    AppendLog("错误代码：" + recData.code + ",错误信息：" + recData.mes);
                }
            };
            wsOperate.OnClose += (s, ee) => {
               // AppendLog("服务器通讯结束!");
            };
            wsOperate.Connect();

            var data = new Info
            {
                flag = "slaves",
                id = account_id
            };
            wsOperate.Send(JsonConvert.SerializeObject(data));
        }

        private void AppendLog(string text)
        {
            var len = 30;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                ListResult.Items.Add("[" + DateTime.Now.ToString() + "] ");
                string temp;
                if (text.Length > len)
                    for (int i = 0; text.Length > len; i++)
                    {
                        temp = text.Substring(0, len);
                        ListResult.Items.Add(temp);
                        text = text.Substring(len);
                    }
               ListResult.Items.Add(text);
               ListResult.SelectedIndex = ListResult.Items.Count - 1;
               ListResult.ScrollIntoView(ListResult.SelectedItem);
            }));
        }

        private void ComboSalves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectSlave = new Slave();
            selectSlaves = slavesList[ComboSlaves.SelectedIndex];
            selectSlave.name = selectSlaves.name;
            selectSlave.id = selectSlaves.id;
            selectSlave.kind = selectSlaves.kind;
            selectSlave.state = selectSlaves.state;
            experimentsList = new List<Experiments>();
            foreach (JObject i in selectSlaves.experiments)
            {
                experimentsList.Add(JsonConvert.DeserializeObject<Experiments>(i.ToString()));
            }
            if (selectSlaves.user_experiment!=null)
                experimentsList.Add(JsonConvert.DeserializeObject<Experiments>(selectSlaves.user_experiment.ToString()));
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                ComboExperiment.ItemsSource = experimentsList;
                ComboExperiment.DisplayMemberPath = "name";
            }));
        }

        private void ComboExperiment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectExperiment = experimentsList[ComboExperiment.SelectedIndex].name;
            AppendLog("选中从机为："+selectSlave.name+"   选中实验为：" + selectExperiment);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                GridOperate.IsEnabled = true;
                GridModbus.IsEnabled = false;
                BtnStop.IsEnabled = false;
            }));
        }

        private void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            //上传文件，先不实现
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            mode = "operate";
            wsOperate = new WebSocket("ws://" + server + "/mode=" + mode);
            wsOperate.OnMessage += (s, ee) => {
                var recData = JsonConvert.DeserializeObject<BasicData>(ee.Data);
                if (recData.code == "401")
                {
                    File.WriteAllText("D:\\" + selectExperiment, recData.data, Encoding.UTF8);
                    AppendLog(recData.mes);
                }
                else
                {
                    AppendLog("错误代码：" + recData.code + ",错误信息：" + recData.mes);
                }
            };
            wsOperate.OnClose += (s, ee) => {
                // AppendLog("服务器通讯结束!");
            };
            wsOperate.Connect();
            var data = new DownLoadData
            {
                id = account_id,
                slave = selectSlave,
                experiment = selectExperiment
            };
            wsOperate.Send(JsonConvert.SerializeObject(data));
        }

        

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                GridChoice.IsEnabled = false;
                GridModbus.IsEnabled = true;
                BtnStop.IsEnabled = true;
                BtnStart.IsEnabled = false;
                BtnDownload.IsEnabled = false;
            }));
            mode = "operate";
            wsOperate = new WebSocket("ws://" + server + "/mode=" + mode);
            wsOperate.OnMessage += (s, ee) => {
                var recData = JsonConvert.DeserializeObject<BasicData>(ee.Data);
                if (recData.code == "402")
                {
                    AppendLog("开启："+recData.mes);
                }
                else
                {
                    AppendLog("错误代码：" + recData.code + ",错误信息：" + recData.mes);
                }
            };
            wsOperate.OnClose += (s, ee) => {
                // AppendLog("服务器通讯结束!");
            };
            wsOperate.Connect();
            selectSlave.state = "busy";
            var data = new StartData
            {
                id = account_id,
                slave = selectSlave,
                experiment = selectExperiment
            };
            wsOperate.Send(JsonConvert.SerializeObject(data));
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                GridModbus.IsEnabled = false;
                BtnStop.IsEnabled = false;
                BtnStart.IsEnabled = true;
                BtnDownload.IsEnabled = true;
            }));
            mode = "operate";
            wsOperate = new WebSocket("ws://" + server + "/mode=" + mode);
            wsOperate.OnMessage += (s, ee) => {
                var recData = JsonConvert.DeserializeObject<BasicData>(ee.Data);
                if (recData.code == "404")
                {
                    AppendLog("关闭" + recData.mes);
                }
                else
                {
                    AppendLog("错误代码：" + recData.code + ",错误信息：" + recData.mes);
                }
            };
            wsOperate.OnClose += (s, ee) => {
                // AppendLog("服务器通讯结束!");
            };
            wsOperate.Connect();
            selectSlave.state = "free";
            var data = new StopData
            {
                id = account_id,
                slave = selectSlave,
                experiment=selectExperiment
            };
            wsOperate.Send(JsonConvert.SerializeObject(data));
        }

        private void WindowsOperate_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (selectSlave.state == "busy")
            {
                mode = "operate";
                wsOperate = new WebSocket("ws://" + server + "/mode=" + mode);
                wsOperate.OnMessage += (s, ee) => {
                    var recData = JsonConvert.DeserializeObject<BasicData>(ee.Data);
                };
                wsOperate.OnClose += (s, ee) => {
                    // AppendLog("服务器通讯结束!");
                };
                wsOperate.Connect();
                selectSlave.state = "free";
                var data = new StopData
                {
                    id = account_id,
                    slave = selectSlave,
                    experiment = selectExperiment
                };
                wsOperate.Send(JsonConvert.SerializeObject(data));
            }
        }

        private void WindowsOperate_Closed(object sender, EventArgs e)
        {
            
            activateGrid();
        }

       
    }
}
