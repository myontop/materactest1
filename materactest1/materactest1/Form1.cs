using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public SerialPort mySerialPort;
        DataTable table;
        Timer timer;
        IList tableDataSource = null;
        int tara_tens;
        int previous_offset=-1;

        public Form1()
        {
            InitializeComponent();

            table = new DataTable("measurement");

            mySerialPort = new SerialPort("COM4");

            mySerialPort.BaudRate = 57600;
            mySerialPort.Parity = Parity.None;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.DataBits = 8;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.RtsEnable = true;

            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            mySerialPort.Open();
            timer =  new Timer();
            timer.Interval = 100;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

           
            table.Columns.Clear();
            table.Columns.Add("time", typeof(int));
            table.Columns.Add("tens", typeof(int));
            table.Columns.Add("offset", typeof(int));
            table.Columns.Add("ra", typeof(int));

            chart1.Series.Clear();
            chart1.Series.Add("tens");
            chart1.Series["tens"].XValueMember = "time";
            chart1.Series["tens"].YValueMembers = "tens";

            chart1.Series.Add("offset");
            chart1.Series["offset"].XValueMember = "time";
            chart1.Series["offset"].YValueMembers = "offset";

            chart1.Series.Add("ra");
            chart1.Series["ra"].XValueMember = "time";
            chart1.Series["ra"].YValueMembers = "ra";
            chart1.DataSource = table;
            chart1.DataBind();

            chart2.Series.Clear();
            chart2.Series.Add("tensoffset");
            chart2.Series["tensoffset"].XValueMember = "offset";
            chart2.Series["tensoffset"].YValueMembers = "tens";
            chart2.DataSource = table;
            chart2.DataBind();

            //tableDataSource = (table as IListSource).GetList();
            // chart1.DataBindTable(tableDataSource, "time");
            chart1.Series["tens"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series["offset"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series["ra"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            // chart1.Series[2].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

            chart2.Series["tensoffset"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

            tara_tens = 0;


        }
        void timer_Tick(object sender, EventArgs e)
        {

            //table.Rows.Add(1, 2,3);
            //chart1.Update();
            chart1.DataBind(); // to chyba nie jest thread safe
            chart2.DataBind();
        }

        private void chart1_DoubleClick(object sender, EventArgs e)
        {
            var m_dbConnection =
               new SQLiteConnection(@"Data Source=c:\Go\workspace\src\strojak\serial\mydb.db;Version=3;");
            m_dbConnection.Open();

            string sql = "select time, tens from measurement limit 100;";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
                Console.WriteLine("tems: " + reader["tens"] + "\ttime: " + reader["time"]);


            var sqlAdapter = new SQLiteDataAdapter("select time, tens  from measurement order by time asc limit 100;", m_dbConnection);
            DataTable table = new DataTable();
            sqlAdapter.AcceptChangesDuringFill = false;
            sqlAdapter.Fill(table);

           

            chart1.DataSource = table.DataSet;
            var tableDataSource = (table as IListSource).GetList();
            chart1.DataBindTable(tableDataSource, "time");
            chart1.Series[1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Update();


        }
       
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadLine();
            //Console.WriteLine("Data Received:");
            //Console.Write(indata);

            if (indata.StartsWith("s1>"))
            {
                string js = indata.Substring(3);
                dynamic stuff;
                try
                {
                    stuff = JsonConvert.DeserializeObject(js);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
               
               // Console.WriteLine(stuff.time);
                //Console.WriteLine(stuff.tens);
                //Console.WriteLine(stuff.offset);


                int time, tens,offset,ra = 0;

                var exists = Int32.TryParse((string)stuff.time, out time);
                exists &= Int32.TryParse((string)stuff.tens, out tens);
                exists &= Int32.TryParse((string)stuff.offset, out offset);
                exists &= Int32.TryParse((string)stuff.ra, out ra);
                if (exists)
                {

                    // if (offset % 5 == 0)
                    //{
                    Console.WriteLine(offset);
                    if (offset * previous_offset <0)
                    {
                        previous_offset = offset;
                        tara_tens = tens;
                    }
                    table.Rows.Add(time, tens-tara_tens, offset,ra);
                    
                    //}
                }

                
                //date = date.AddDays(1);


            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            mySerialPort.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            table.Rows.Clear();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
