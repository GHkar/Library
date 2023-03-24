using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client;

namespace _5469440_김애리_대여점DB
{
    public partial class TopForm : Form
    {
        private string recv1;
        public TopForm()
        {
            InitializeComponent();
            this.Height = 450;
            this.Width = 500;
            this.MaximizeBox = false;
        }

        public string passValue1
        {
            get { return this.recv1; }
            set { this.recv1 = value; }  // 다른폼(Form1)에서 전달받은 값을 쓰기
        }
        public static DateTime[] GetDatesOfWeek()
        {
            DateTime[] datesOfWeek = new DateTime[7];
            int dayOfWeekValue = (int)DateTime.Now.DayOfWeek - 1;
            if (dayOfWeekValue == -1)
            {
                for (int i = 0; i < 7; i++)
                {
                    datesOfWeek[i] = DateTime.Today.AddDays(-6 + i);
                }
            }
            else
            {
                for (int i = 0; i < 7; i++)
                {
                    datesOfWeek[i] = DateTime.Today.AddDays(-dayOfWeekValue + i);
                }
            }
            return datesOfWeek;
        }

        private void TopForm_Load(object sender, EventArgs e)
        {
           
            this.Text = "언덕 위의 책과 비디오 대여점 - " + recv1 + " 인기 항목 보기";

            goodsTableAdapter1.Fill(dataSet11.GOODS);
            DataTable goodsTable = dataSet11.Tables["GOODS"];
            

            listView1.Columns.Add("순위", 50);
            listView1.Columns.Add("종류", 80);
            listView1.Columns.Add("이름", 120);
            /* * 다섯가지 모양을 가질 수 있다. * 큰아이콘, 작은아이콘, 리스트, 상세히, 타일모양 등 */
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.GridLines = true;

            if (recv1 == "일간")
            {
                oracleConnection1.Open();

                oracleCommand1.CommandText = "CREATE VIEW SALE_COUNT AS SELECT G_NO, GT_NO, COUNT(*) COUNT_NUM FROM RENT WHERE R_DATE >= to_Date('" + DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('"+ DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd') GROUP BY G_NO, GT_NO";
                oracleCommand1.ExecuteNonQuery();
                oracleCommand2.CommandText = "SELECT * FROM SALE_COUNT ORDER BY COUNT_NUM DESC";
                int no = 1;
                OracleDataReader odr = oracleCommand2.ExecuteReader();
                while (odr.Read())
                {
                    if (no > 10) break; // 최대 10개
                    String[] arr = new String[3];
                    arr[0] = no++.ToString();
                    if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 1)
                    {
                        arr[1] = "만화";
                    }
                    else if((Convert.ToInt32(odr["GT_NO"]) / 10) == 2)
                    {
                        arr[1] = "비디오";
                    }
                    else if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 3)
                    {
                        arr[1] = "소설";
                    }
                    DataRow[] goodsRow = goodsTable.Select("G_NO = " + odr["G_NO"]);
                    arr[2] = goodsRow[0]["G_NAME"].ToString();

                    ListViewItem lvt = new ListViewItem(arr);
                    listView1.Items.Add(lvt);
                }
                oracleCommand3.CommandText = "DROP VIEW sale_count";
                oracleCommand3.ExecuteNonQuery();
                oracleConnection1.Close();
            }
            else if(recv1 == "주간")
            {
                oracleConnection1.Open();

                DateTime[] dt = GetDatesOfWeek();
                

                oracleCommand1.CommandText = "CREATE VIEW SALE_COUNT AS SELECT G_NO, GT_NO, COUNT(*) COUNT_NUM FROM RENT WHERE R_DATE >= to_Date('" + dt[0].ToString("yyyy/MM/dd") + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + dt[6].ToString("yyyy/MM/dd") + "', 'yyyy-mm-dd') GROUP BY G_NO, GT_NO";
                oracleCommand1.ExecuteNonQuery();
                oracleCommand2.CommandText = "SELECT * FROM SALE_COUNT ORDER BY COUNT_NUM DESC";
                int no = 1;
                OracleDataReader odr = oracleCommand2.ExecuteReader();
                while (odr.Read())
                {
                    if (no > 10) break; // 최대 10개
                    String[] arr = new String[3];
                    arr[0] = no++.ToString();
                    if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 1)
                    {
                        arr[1] = "만화";
                    }
                    else if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 2)
                    {
                        arr[1] = "비디오";
                    }
                    else if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 3)
                    {
                        arr[1] = "소설";
                    }
                    DataRow[] goodsRow = goodsTable.Select("G_NO = " + odr["G_NO"]);
                    arr[2] = goodsRow[0]["G_NAME"].ToString();

                    ListViewItem lvt = new ListViewItem(arr);
                    listView1.Items.Add(lvt);
                }
                oracleCommand3.CommandText = "DROP VIEW sale_count";
                oracleCommand3.ExecuteNonQuery();
                oracleConnection1.Close();
            }
            else if (recv1 == "월간")
            {
                oracleConnection1.Open();
                DateTime today = DateTime.Now.Date;
                DateTime first = today.AddDays(1 - today.Day);
                DateTime second = first.AddMonths(1).AddDays(-1);

                oracleCommand1.CommandText = "CREATE VIEW SALE_COUNT AS SELECT G_NO, GT_NO, COUNT(*) COUNT_NUM FROM RENT WHERE R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd') GROUP BY G_NO, GT_NO";
                oracleCommand1.ExecuteNonQuery();
                oracleCommand2.CommandText = "SELECT * FROM SALE_COUNT ORDER BY COUNT_NUM DESC";
                int no = 1;
                OracleDataReader odr = oracleCommand2.ExecuteReader();
                while (odr.Read())
                {
                    if (no > 10) break; // 최대 10개
                    String[] arr = new String[3];
                    arr[0] = no++.ToString();
                    if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 1)
                    {
                        arr[1] = "만화";
                    }
                    else if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 2)
                    {
                        arr[1] = "비디오";
                    }
                    else if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 3)
                    {
                        arr[1] = "소설";
                    }
                    DataRow[] goodsRow = goodsTable.Select("G_NO = " + odr["G_NO"]);
                    arr[2] = goodsRow[0]["G_NAME"].ToString();

                    ListViewItem lvt = new ListViewItem(arr);
                    listView1.Items.Add(lvt);
                }
                oracleCommand3.CommandText = "DROP VIEW sale_count";
                oracleCommand3.ExecuteNonQuery();
                oracleConnection1.Close();
            }
            else if (recv1 == "연간")
            {
                oracleConnection1.Open();
                DateTime first = new DateTime(DateTime.Now.Year, 01, 01);
                DateTime second = new DateTime(DateTime.Now.Year, 12,  DateTime.DaysInMonth(DateTime.Now.Year, 12));

                oracleCommand1.CommandText = "CREATE VIEW SALE_COUNT AS SELECT G_NO, GT_NO, COUNT(*) COUNT_NUM FROM RENT WHERE R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd') GROUP BY G_NO, GT_NO";
                oracleCommand1.ExecuteNonQuery();
                oracleCommand2.CommandText = "SELECT * FROM SALE_COUNT ORDER BY COUNT_NUM DESC";
                int no = 1;
                OracleDataReader odr = oracleCommand2.ExecuteReader();
                while (odr.Read())
                {
                    if (no > 10) break; // 최대 10개
                    String[] arr = new String[3];
                    arr[0] = no++.ToString();
                    if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 1)
                    {
                        arr[1] = "만화";
                    }
                    else if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 2)
                    {
                        arr[1] = "비디오";
                    }
                    else if ((Convert.ToInt32(odr["GT_NO"]) / 10) == 3)
                    {
                        arr[1] = "소설";
                    }
                    DataRow[] goodsRow = goodsTable.Select("G_NO = " + odr["G_NO"]);
                    arr[2] = goodsRow[0]["G_NAME"].ToString();

                    ListViewItem lvt = new ListViewItem(arr);
                    listView1.Items.Add(lvt);
                }
                oracleCommand3.CommandText = "DROP VIEW sale_count";
                oracleCommand3.ExecuteNonQuery();
                oracleConnection1.Close();
            }
        }
    }
}
