using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Timers;

using System.IO.Ports;

using Ding.Log;
using Ding.Config;

namespace ReadSn
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private MyConfig myConfig;
        private MyLog myLog;
        private ComboBox[] portnameComboBoxs;
        private ComboBox[] baudrateCombos;
        private SerialPort relaySerialPort=new SerialPort(); //继电器控制板串口对象
        private SerialPort dutSerialPort=new SerialPort(); //被测评串口对象
        private Parity parity = Parity.None; //奇偶校验位
        private int dataBit = 8; //数据位
        private StopBits stopBits = StopBits.One; //停止位
        private string sn;
        private Timer myTimer; //定时器
        private long frequency=30*1000; //测试频率30S一次

        public MainWindow()
        {
            InitializeComponent();
            myConfig = new MyConfig();
            InitPortNameBox();
            InitBaudrateBox();
        }


        /// <summary>
        /// 设置波特率选项(4800、9600、19200、38400、115200)
        /// </summary>
        private void InitBaudrateBox()
        {
            string[] baudrateArr = new string[] { "4800", "9600", "19200", "38400", "115200" };
            baudrateCombos = new ComboBox[] { relayBaudrateBox, dutBaudrateBox}; ///有多个波特率窗口时，直接添加到数组即可
            foreach (ComboBox baudrateCombo in baudrateCombos)
            {
                baudrateCombo.ItemsSource = baudrateArr;
                InitCombBox(baudrateCombo);
            }
        }

        //初始化串口名称
        private void InitPortNameBox()
        {
            string[] portnameArr = SerialPort.GetPortNames();
            portnameComboBoxs = new ComboBox[] { relayPortNameBox, dutPortNameBox}; //初始化多个串口名称
            foreach (ComboBox portnameCombo in portnameComboBoxs)
            {
                portnameCombo.ItemsSource = portnameArr;
                InitCombBox(portnameCombo);
            }
        }

        //初始化ComboBox
        private void InitCombBox(ComboBox combo)
        {
            string key = combo.Name;
            if (myConfig.ReadConfig(key) == "")
            {
                combo.SelectedIndex = 0;
                myConfig.WriteDownConfig(key, (string)combo.SelectedValue);
            }
            else
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (combo.Items[i].Equals(myConfig.ReadConfig(key)))
                    {
                        combo.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        //Combox发生变更时，保存配置
        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string key = ((ComboBox)sender).Name;
            if ((string)((ComboBox)sender).SelectedValue != "")
            {
                myConfig.WriteDownConfig(key, (string)((ComboBox)sender).SelectedValue);
            }
        }

        //开始按钮被点击,开始测试
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!relaySerialPort.IsOpen) {
                MessageBox.Show("继电器串口未打开");
                return;
            }
            if (!dutSerialPort.IsOpen) {
                MessageBox.Show("被测品串口未打开");
                return;
            }
            sn = snBox.Text.Trim().ToUpper(); //获取SN的正确值
            if (sn.Equals("")) {
                MessageBox.Show("输入序列号为空");
                return;
            }
            //开始测试将相关控件全部锁定
            CloseRelayButton.IsEnabled = false;
            CloseDutButton.IsEnabled = false;
            snBox.IsEnabled = false;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            string logfilename = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            myLog = new MyLog(logfilename); //新建一个日志文件
            myTimer = new Timer(frequency);
            UpdateTestCount(0, 0);
            myTimer.Elapsed += new ElapsedEventHandler(GetValue); //启动定时器执行测试
            myTimer.Start();
        }

        //获取值
        private void GetValue(object obj, ElapsedEventArgs e)
        {
            //吸合继电器，给DUT供电
            byte[] closeRelay = { 0x00 };
            relaySerialPort.Write(closeRelay, 0, closeRelay.Length);
            //停顿10S,等待RTC配置完成
            System.Threading.Thread.Sleep(10000);
            //读取序列号
            dutSerialPort.Write("SN:?\r\n");
            System.Threading.Thread.Sleep(100);
            string readStr = dutSerialPort.ReadExisting().Trim().ToUpper();
            int testCount = GetTestCount()+ 1;
            int failCount;
            if (readStr.EndsWith(sn)) //如果读取到的字符串以指定序列号结尾，则认为正确，fail数量不变，否则fail数量+1
            {
                failCount = GetFailCount();
                myLog.WriteDownLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " "+readStr+" "+"pass");
            }
            else {
                failCount = GetFailCount()+ 1;
                myLog.WriteDownLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + readStr + " " + "fail");
            }
            UpdateTestCount(testCount,failCount);

            //10秒后断开继电器，即断电
            System.Threading.Thread.Sleep(10000);
            byte[] openRelay = { 0x01 };
            relaySerialPort.Write(openRelay, 0, openRelay.Length);
        }


        //获取界面中的异常数量
        private int GetFailCount()
        {
            int count = 0;
            this.Dispatcher.Invoke(new Action(() =>
            {
                count = Convert.ToInt32(errorCount.Content);
            }));
            return count;
        }

        //获取界面中的测试数量
        private int GetTestCount()
        {
            int count=0;
            this.Dispatcher.Invoke(new Action(() =>
            {
                count= Convert.ToInt32(readCount.Content);
            }));
            return count;
        }

        //刷新测试界面中的测试数量
        private void UpdateTestCount(int testCount,int failCount)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                readCount.Content = ""+ testCount;
                errorCount.Content = "" + failCount;
            }));
        }


        //停止测试按钮按下停止测试,释放控件
        private void StopButton_Click(object sender, RoutedEventArgs e) {
            myTimer.Stop();
            CloseRelayButton.IsEnabled = true;
            CloseDutButton.IsEnabled = true;
            snBox.IsEnabled = true;
            StartButton.IsEnabled = true;
        }

        //打开继电器串口
        private void OpenRelayButton_Click(object sender, RoutedEventArgs e)
        {
            relaySerialPort.PortName = myConfig.ReadConfig("relayPortNameBox");
            relaySerialPort.BaudRate = Convert.ToInt32(myConfig.ReadConfig("relayBaudrateBox"));
            relaySerialPort.Parity = parity;
            relaySerialPort.DataBits = dataBit;
            relaySerialPort.StopBits = stopBits;
            try
            {
                relaySerialPort.Open();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString());
                return;
            }
            OpenRelayButton.IsEnabled = false;
            relayPortNameBox.IsEnabled = false;
            relayBaudrateBox.IsEnabled = false;
            CloseRelayButton.IsEnabled = true;
        }

        //关闭继电器串口
        private void CloseRelayButton_Click(object sender, RoutedEventArgs e)
        {
            try {
                relaySerialPort.Close();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
            OpenRelayButton.IsEnabled = true;
            relayPortNameBox.IsEnabled = true;
            relayBaudrateBox.IsEnabled = true;
            CloseRelayButton.IsEnabled = false;
        }

        //打开DUT串口
        private void OpenDutButton_Click(object sender, RoutedEventArgs e)
        {
            dutSerialPort.PortName = myConfig.ReadConfig("dutPortNameBox");
            dutSerialPort.BaudRate = Convert.ToInt32(myConfig.ReadConfig("dutBaudrateBox"));
            dutSerialPort.Parity = parity;
            dutSerialPort.DataBits = dataBit;
            dutSerialPort.StopBits = stopBits;
            try
            {
                dutSerialPort.Open();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString());
                return;
            }
            OpenDutButton.IsEnabled = false;
            dutPortNameBox.IsEnabled = false;
            dutBaudrateBox.IsEnabled = false;
            CloseDutButton.IsEnabled = true;
        }

        //关闭继电器串口
        private void CloseDutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dutSerialPort.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            OpenDutButton.IsEnabled = true;
            dutPortNameBox.IsEnabled = true;
            dutBaudrateBox.IsEnabled = true;
            CloseDutButton.IsEnabled = false;
        }
    }
}
