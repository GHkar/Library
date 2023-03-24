using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Mail;
using System.Net;
using Oracle.ManagedDataAccess.Client;
using System.Windows.Forms.DataVisualization.Charting;


namespace _5469440_김애리_대여점DB
{
    public partial class Form1 : Form
    {

        string customerRank;
        bool accessTab = false;

        DataRow identityRow;//회원 정보
        DataRow[] myFindRow; // 계속 사용할 것
        DataRow passRowGoods;

        DataTable staffTable;
        DataTable customerTable;
        DataTable whiteListTable;
        DataTable rentTable;
        DataTable goodsTable;
        DataTable fineTable;
        DataTable goodsTypeTable;
        DataTable blackListTable;
        DataTable reserveTable;
        DataTable reviewTable;

        DataRelation GR;
        DataRelation GTG;
        DataRelation RF;

        public Form1()
        {
            InitializeComponent();
            this.Text = "언덕 위의 책과 비디오 대여점";
            this.Height = 600;
            this.Width = 1000;
            this.MaximizeBox = false;
        }

        private void reRoad()
        {
            this.rEVIEWTableAdapter.Fill(this.dataSet11.REVIEW);
            reviewTable = dataSet11.Tables["REVIEW"];

            // TODO: 이 코드는 데이터를 'dataSet11.GOODS' 테이블에 로드합니다. 필요 시 이 코드를 이동하거나 제거할 수 있습니다.
            this.gOODSTableAdapter.Fill(this.dataSet11.GOODS);
            goodsTable = dataSet11.Tables["GOODS"];

            customerTableAdapter1.Fill(dataSet11.CUSTOMER);
            customerTable = dataSet11.Tables["CUSTOMER"];

            whitE_LISTTableAdapter1.Fill(dataSet11.WHITE_LIST);
            whiteListTable = dataSet11.Tables["WHITE_LIST"];

            rentTableAdapter1.Fill(dataSet11.RENT);
            rentTable = dataSet11.Tables["RENT"];

            fineTableAdapter1.Fill(dataSet11.FINE);
            fineTable = dataSet11.Tables["FINE"];

            goodS_TYPETableAdapter1.Fill(dataSet11.GOODS_TYPE);
            goodsTypeTable = dataSet11.Tables["GOODS_TYPE"];

            blacK_LISTTableAdapter1.Fill(dataSet11.BLACK_LIST);
            blackListTable = dataSet11.Tables["BLACK_LIST"];

            reserveTableAdapter1.Fill(dataSet11.RESERVE);
            reserveTable = dataSet11.Tables["RESERVE"];
            reserveTable.DefaultView.Sort = "RS_NO asc";

        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            reRoad();

            GR = dataSet11.Relations["SYS_C0019227"];
            GTG = dataSet11.Relations["SYS_C0019226"];
            RF = dataSet11.Relations["SYS_C0019229"];
            
            staffTableAdapter1.Fill(dataSet11.STAFF);
            staffTable = dataSet11.Tables["STAFF"];


        }


        //오늘의 업데이트
        private void button110_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("업데이트 하시겠습니까?", "업데이트하기", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                oracleConnection1.Open();
                //예약 날짜 지나면 삭제하기 --> 재고 올리기
                DataRow[] dt = reserveTable.Select("RS_ReserveDate is not null");
                foreach (DataRow myrow in dt)
                {
                    DateTime dtt = Convert.ToDateTime(myrow["RS_AlarmDate"]).AddDays(2);
                    if (DateTime.Compare(dtt, DateTime.Now) == -1)
                    {
                        oracleCommand1.CommandText = "DELETE FROM RESERVE WHERE RS_NO =" +
                            myrow["RS_NO"].ToString();
                        oracleCommand1.ExecuteNonQuery();
                        oracleCommand2.CommandText = "UPDATE GOODS SET G_STOCK = G_STOCK+1" +
                                    " WHERE G_NO =" + myrow["G_NO"];
                        oracleCommand2.ExecuteNonQuery();
                    }
                }
                reserveTableAdapter1.Fill(dataSet11.RESERVE);
                reserveTable = dataSet11.Tables["RESERVE"];
               gOODSTableAdapter.Fill(dataSet11.GOODS);
                goodsTable = dataSet11.Tables["GOODS"];

                //품절여부 처리
                DataRow[] dt6 = goodsTable.Select("G_Stock > 0 and G_IsSoldOut = 1");
                if (dt6.Length > 0)
                {
                    foreach (DataRow myrow in dt6)
                    {
                        oracleCommand3.CommandText = "UPDATE Goods SET G_IsSoldOut = 0 " +
                                        "WHERE G_NO = " + myrow["G_NO"].ToString() + " and GT_NO = "
                                        + myrow["GT_NO"].ToString();
                        oracleCommand3.ExecuteNonQuery();
                    }
                }
                DataRow[] dt7 = goodsTable.Select("G_Stock <= 0 and G_IsSoldOut = 0");
                if (dt7.Length > 0)
                {
                    foreach (DataRow myrow in dt7)
                    {
                        oracleCommand3.CommandText = "UPDATE Goods SET G_IsSoldOut = 1 " +
                                        "WHERE G_NO = " + myrow["G_NO"].ToString() + " and GT_NO = "
                                        + myrow["GT_NO"].ToString();
                        oracleCommand3.ExecuteNonQuery();
                    }
                }

                gOODSTableAdapter.Fill(dataSet11.GOODS);
                goodsTable = dataSet11.Tables["GOODS"];

                //대여일수 감소
                foreach (DataRow myrow in rentTable.Rows)
                {
                    oracleCommand3.CommandText = "UPDATE RENT SET R_NDAY = R_NDAY - 1 " +
                                        "WHERE G_NO = " + myrow["G_NO"].ToString() + " and GT_NO = "
                                        + myrow["GT_NO"].ToString() + " and C_NO = " + myrow["C_NO"].ToString();
                    oracleCommand3.ExecuteNonQuery();
                }

                rentTableAdapter1.Fill(dataSet11.RENT);
                rentTable = dataSet11.Tables["RENT"];

                //벌금 테이블로 넘기기
                foreach (DataRow myrow in rentTable.Rows)
                {
                    if (Convert.ToInt32(myrow["R_NDAY"]) < 0)
                    {
                        DataRow[] dt5 = fineTable.Select("C_NO = " + myrow["C_NO"].ToString() +
                            " and G_NO = " + myrow["G_NO"].ToString() + " and GT_NO = " + myrow["GT_NO"].ToString());
                        if (dt5.Length <= 0)
                        {
                            DataRow[] dt8 = goodsTable.Select("G_NO = " + myrow["G_NO"].ToString() + " and GT_NO = " + myrow["GT_NO"].ToString());
                            oracleCommand3.CommandText = "Insert into fine values( " +
                                myrow["C_NO"].ToString() + ", " + myrow["G_NO"].ToString() + ", " + myrow["GT_NO"].ToString() +
                                ", 1, 300, " + dt8[0]["G_RANK"].ToString() + ", null)";
                            oracleCommand3.ExecuteNonQuery();

                            //메일 보내기
                            DataRow[] dt2 = customerTable.Select("C_NO = " + myrow["C_NO"].ToString());
                            string d = dt2[0]["C_Email"].ToString();
                            MailMessage msg = new MailMessage("dofl0119@gmail.com", "dofl011@naver.com",
                            "Subject : 안녕하세요, 언덕위의 책과 비디오 대여점입니다.",
                            "안녕하세요, 고객님. \n 반납기간이 연체되어 이메일 드립니다.\n금일부터 벌금이 부과되오니 빠른 시일내에 반납 부탁드리겠습니다.");

                            // SmtpClient 셋업 (Live SMTP 서버, 포트)
                            SmtpClient smtp = new SmtpClient("smtp.live.com", 587);
                            smtp.EnableSsl = true;

                            // Live 또는 Hotmail 계정과 암호 필요
                            smtp.Credentials = new NetworkCredential("dofl0119@gmail.com", "6602gjqm19!");

                            // 발송
                            smtp.Send(msg);
                        }
                    }
                }

                fineTableAdapter1.Fill(dataSet11.FINE);
                fineTable = dataSet11.Tables["FINE"];

                //연체 자동 증가 - 데이트 없는 것
                foreach (DataRow myrow in fineTable.Rows)
                {
                    if (myrow["F_RETURNDATE"].ToString() == "")
                    {
                        oracleCommand3.CommandText = "UPDATE FINE SET F_OVERDATE = F_OVERDATE + 1 " +
                                        "WHERE G_NO = " + myrow["G_NO"].ToString() + " and GT_NO = "
                                        + myrow["GT_NO"].ToString() + " and C_NO = " + myrow["C_NO"].ToString();
                        oracleCommand3.ExecuteNonQuery();
                    }
                }

                fineTableAdapter1.Fill(dataSet11.FINE);
                fineTable = dataSet11.Tables["FINE"];

                int normal = 0;
                //벌금 증가 - 데이트 없는 것
                foreach (DataRow myrow in fineTable.Rows)
                {
                    normal = 300;
                    if (myrow["F_RETURNDATE"].ToString() == "")
                    {
                        if (myrow["F_G_RANK"].ToString() != "1")
                        {
                            normal = 400;
                        }
                        normal = (normal * Convert.ToInt32(myrow["F_OVERDATE"]));
                        oracleCommand3.CommandText = "UPDATE FINE SET F_Fine = " + normal +
                                        "WHERE G_NO = " + myrow["G_NO"].ToString() + " and GT_NO = "
                                        + myrow["GT_NO"].ToString() + " and C_NO = " + myrow["C_NO"].ToString();
                        oracleCommand3.ExecuteNonQuery();
                    }
                }
                fineTableAdapter1.Fill(dataSet11.FINE);
                fineTable = dataSet11.Tables["FINE"];

                //블랙리스트 킵데이트 자동 감소 --> 삭제
                foreach (DataRow myrow in blackListTable.Rows)
                {
                    oracleCommand3.CommandText = "UPDATE BLACK_LIST SET BL_KEEPDAY = BL_KEEPDAY - 1 " +
                                        "WHERE C_NO = " + myrow["C_NO"].ToString() + " and BL_No = " + myrow["BL_NO"].ToString();
                    oracleCommand3.ExecuteNonQuery();
                }
                blacK_LISTTableAdapter1.Fill(dataSet11.BLACK_LIST);
                blackListTable = dataSet11.Tables["BLACK_LIST"];
                //삭제
                foreach (DataRow myrow in blackListTable.Rows)
                {
                    oracleCommand1.CommandText = "DELETE FROM BLACK_LIST WHERE BL_KEEPDAY <= 0";
                    oracleCommand1.ExecuteNonQuery();
                }

                oracleConnection1.Close();

                MessageBox.Show("업데이트가 완료 되었습니다.");
            }
            else { }
        }
        
        /************************메인 화면*****************************/

        //일반 버튼 클릭 시
        private void button1_Click(object sender, EventArgs e)
        {
            panel2.Visible = true;
        }
        //직원 버튼 클릭 시
        private void button2_Click(object sender, EventArgs e)
        {
            loginPanel.Visible = true;
        }

        /************************일반 로그인 화면*****************************/
        //back 버튼 클릭 시
        private void button6_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
        }

        //로그인 버튼 클릭 시
        private void button5_Click(object sender, EventArgs e)
        {
            bool isTrue = false;
            label40.Visible = false;
            label41.Visible = false;

            DataRow[] foundRows = customerTable.Select("C_Name = '" + textBox3.Text + "'");

            if (textBox4.Text == "")
            {
                label41.Visible = true;
                label41.Text = "이메일을 입력해주세요.";
            }
            else
            {
                label41.Visible = false;
            }

            if (textBox3.Text == "")
            {
                label40.Visible = true;
                label40.Text = "이름을 입력해주세요.";
            }
            else
            {
                if (foundRows.Length == 0)
                {
                    label40.Visible = true;
                    label40.Text = "일치하는 이름이 없습니다. 이름을 다시 확인해주세요.";
                    //MessageBox.Show("입력하신 이름과 일치하는 사용자가 없습니다. \n 입력하신 이름을 다시 확인해주세요.");
                }
                else if (foundRows.Length > 0)
                {
                    foreach (DataRow mydataRow in foundRows)
                    {
                        //로그인 성공
                        label40.Visible = false;
                        if (mydataRow["C_Email"].ToString() == textBox4.Text)
                        {
                            identityRow = mydataRow;
                            panel1.Visible = true;
                            panel2.Visible = false;
                            isTrue = true;
                            loginSuccess();
                            break;
                        }
                    }
                    if (textBox4.Text == "")
                    {
                        label41.Visible = true;
                        label41.Text = "이메일을 입력해주세요.";
                    }
                    else if (isTrue == false)
                    {
                        label41.Visible = true;
                        label41.Text = "입력하신 이메일이 일치하지 않습니다. 다시 입력해주세요.";
                        //MessageBox.Show("입력하신 이름과 이메일이 일치하지 않습니다. \n이메일 주소를 다시 입력해주세요.");

                    }
                }
            }
        }

        //로그인 성공시 함수

        private void loginSuccess()
        {
            nameLabel.Text = identityRow["C_Name"].ToString();
            DataRow[] foundRows = whiteListTable.Select("C_No = " + identityRow["C_No"].ToString());
            if (foundRows.Length == 0)
            {
                customerRank = "일반 고객";
            }
            else if (foundRows.Length == 1)
            {
                customerRank = "우수 고객";
            }

            comboBox3.Items.Clear();
            int a = 0;
            goodsTypeTable.DefaultView.Sort = "GT_NO asc";
            comboBox3.Items.Add("전체 보기");
            foreach (DataRow myrow in goodsTypeTable.Rows)
            {
                if (a != Convert.ToInt32(myrow["GT_NO"]) / 10)
                {
                    string c = myrow["GT_NAME"].ToString();
                    string[] b = c.Split(' ');
                    comboBox3.Items.Add(b[1]);
                    a = Convert.ToInt32(myrow["GT_NO"]) / 10;
                }
            }
            //분류 기본 값 0으로 주기
            comboBox3.SelectedIndex = 0;
        }
        /************************직원 로그인 화면*****************************/
        // 로그인 패널에서 back 버튼 클릭 시
        private void button4_Click(object sender, EventArgs e)
        {
            loginPanel.Visible = false;
        }

        //로그인 패널에서 로그인 클릭 시
        private void button3_Click(object sender, EventArgs e)
        {
            bool isTrue = false;
            if (textBox1.Text == "")
            {
                label57.Visible = true;
                label57.Text = "아이디를 입력해주세요.";
            }
            else
            {
                label57.Visible = false;
            }

            if (textBox2.Text == "")
            {
                label58.Visible = true;
                label58.Text = "비밀 번호를 입력해주세요.";
            }
            else
            {
                label58.Visible = false;
            }
            if (comboBox1.SelectedIndex == -1)
            {
                label59.Visible = true;
            }
            else
            {
                label59.Visible = false;
            }
            if (textBox1.Text != "" && textBox2.Text != "" && comboBox1.SelectedIndex != -1)
            {
                if (comboBox1.Text == "직원")
                {
                    DataRow[] foundRows = staffTable.Select("S_RANK = 1 ");
                    foreach (DataRow mydataRow in foundRows)
                    {
                        if (mydataRow["S_ID"].ToString() == textBox1.Text && mydataRow["S_PW"].ToString() == textBox2.Text)
                        {
                            MessageBox.Show("직원으로 로그인 되었습니다.");
                            isTrue = true;
                            identityRow = mydataRow;
                            break;
                        }
                    }
                }
                else if (comboBox1.Text == "매니저")
                {
                    DataRow[] foundRows = staffTable.Select("S_RANK = 2");
                    foreach (DataRow mydataRow in foundRows)
                    {
                        if (mydataRow["S_ID"].ToString() == textBox1.Text && mydataRow["S_PW"].ToString() == textBox2.Text)
                        {
                            MessageBox.Show("매니저로 로그인 되었습니다.");
                            isTrue = true;
                            identityRow = mydataRow;
                            accessTab = true;
                            break;
                        }
                    }
                }
                else if (comboBox1.Text == "사장")
                {
                    DataRow[] foundRows = staffTable.Select("S_RANK = 3");
                    foreach (DataRow mydataRow in foundRows)
                    {
                        if (mydataRow["S_ID"].ToString() == textBox1.Text && mydataRow["S_PW"].ToString() == textBox2.Text)
                        {
                            MessageBox.Show("사장으로 로그인 되었습니다.");
                            isTrue = true;
                            identityRow = mydataRow;
                            accessTab = true;
                            break;
                        }
                    }
                }
                if (isTrue == false)
                {
                    MessageBox.Show("접근 권한이 없거나, 아이디 또는 패스워드가 잘못 입력되었습니다. \n다시 확인하고 로그인 해주세요.");
                }
                else
                {
                    panel5.Visible = true;
                    loginPanel.Visible = false;
                    staffLoginSuccess();
                }
            }
        }
        /************************일반 화면*****************************/
        //분류 콤보박스 필터링(만화, 비디오, 소설)

        string check1 = "";
        string check2 = "";
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == 0)
            {
                comboBox5.Visible = false;
                label53.Visible = false;
                comboBox5.SelectedIndex = -1;
                if (checkBox3.Checked && checkBox2.Checked)
                {
                    gOODSBindingSource.Filter = "g_issale = 1 and g_issoldout = 0";
                }
                else if (checkBox3.Checked && !checkBox2.Checked)
                {
                    gOODSBindingSource.Filter = " g_issoldout = 0";
                }
                else if (checkBox2.Checked && !checkBox3.Checked)
                {
                    gOODSBindingSource.Filter = " g_issale = 1";
                }
                else if (!checkBox2.Checked && !checkBox3.Checked)
                {
                    gOODSBindingSource.RemoveFilter();
                }
            }
            else
            {
                // 장르 아이템 추가
                comboBox5.Items.Clear();
                int a = comboBox3.SelectedIndex + 1;
                goodsTypeTable.DefaultView.Sort = "GT_NO asc";
                DataRow[] dt = goodsTypeTable.Select("gt_no/10 <= " + a + "and gt_no/10 >= " + (a - 1));
                foreach (DataRow myrow in dt)
                {
                    string c = myrow["GT_NAME"].ToString();
                    string[] b = c.Split(' ');
                    comboBox5.Items.Add(b[0]);
                }
                // 필터링
                gOODSBindingSource.Filter = "gt_no < " + (a * 10) + " and gt_no > " + ((a - 1) * 10) + check1 + check2;
                comboBox5.Visible = true;
                label53.Visible = true;
                comboBox5.SelectedIndex = -1;
            }
        }
        //장르 콤보 박스 인덱스 체인지 시
        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            int a = comboBox3.SelectedIndex;
            int b = comboBox5.SelectedIndex + 1;
            gOODSBindingSource.Filter = "GT_NO = " + ((a * 10) + b) + check1 + check2;
        }
        //품절 항목 보기
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                check1 = " and g_issoldout = 0";
            }
            else
            {
                check1 = "";
            }

            if (comboBox5.SelectedIndex >= 0)
            {
                int b = comboBox5.SelectedIndex;
                comboBox5.SelectedIndex = -1;
                comboBox5.SelectedIndex = b;
            }
            else
            {
                int a = comboBox3.SelectedIndex;
                comboBox3.SelectedIndex = -1;
                comboBox3.SelectedIndex = a;
            }


        }
        //세일 항목 보기
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                check2 = " and g_issale = 1";
            }
            else
            {
                check2 = "";
            }
            if (comboBox5.SelectedIndex >= 0)
            {
                int b = comboBox5.SelectedIndex;
                comboBox5.SelectedIndex = -1;
                comboBox5.SelectedIndex = b;
            }
            else
            {
                int a = comboBox3.SelectedIndex;
                comboBox3.SelectedIndex = -1;
                comboBox3.SelectedIndex = a;
            }
        }


        //검색하기 버튼 클릭 시
        private void button10_Click(object sender, EventArgs e)
        {
            //이름
            if (comboBox4.SelectedIndex == 0)
            {
                gOODSBindingSource.Filter = "g_name like '%" + textBox5.Text + "%'";
            }
            //연령제한
            else if (comboBox4.SelectedIndex == 1)
            {
                gOODSBindingSource.Filter = "g_ageLimit >= " + textBox5.Text;
            }
            //작가/감독
            else if (comboBox4.SelectedIndex == 2)
            {
                gOODSBindingSource.Filter = "g_wnd like '%" + textBox5.Text + "%'";
            }
            //출판사 / 제작사
            else if (comboBox4.SelectedIndex == 3)
            {
                gOODSBindingSource.Filter = "g_pnp like '%" + textBox5.Text + "%'";
            }
            //출시년도
            else if (comboBox4.SelectedIndex == 4)
            {
                DateTime dt1 = new DateTime(Convert.ToInt32(textBox5.Text), 01, 01);
                DateTime dt2 = new DateTime(Convert.ToInt32(textBox5.Text), 12, 31);
                gOODSBindingSource.Filter = string.Format("g_createdate >= #{0:yyyy/MM/dd}# And g_createdate <= #{1:yyyy/MM/dd}#", dt1, dt2);
            }
            //출시국가
            else if (comboBox4.SelectedIndex == 5)
            {
                gOODSBindingSource.Filter = "g_nation like '%" + textBox5.Text + "%'";
            }
        }
        //예약하기 버튼
        private void button16_Click(object sender, EventArgs e)
        {
            DataRow[] dt = blackListTable.Select("C_NO = " + identityRow["C_NO"].ToString());
            DataRow[] dt1 = reserveTable.Select("C_NO = " + identityRow["C_NO"].ToString() + " and G_NO = " + dataGridView1.CurrentRow.Cells[0].Value.ToString() + " and GT_NO = " + dataGridView1.CurrentRow.Cells[1].Value.ToString());
            DataRow[] dt2 = rentTable.Select("C_NO = " + identityRow["C_NO"].ToString() + " and G_NO = " + dataGridView1.CurrentRow.Cells[0].Value.ToString() + " and GT_NO = " + dataGridView1.CurrentRow.Cells[1].Value.ToString());
            if (dt1.Length > 0)
            {
                MessageBox.Show("이미 예약을 하셨습니다.");
            }
            else {
                if (dt.Length > 0)
                {
                    MessageBox.Show("현재 블랙리스트 상태이십니다. 예약이 불가 합니다.");
                }
                else
                {
                    if (dt2.Length > 0)
                    {
                        MessageBox.Show("현재 해당 물품을 빌리신 상태이십니다. 예약이 불가 합니다.");
                    }
                    else
                    {
                        if (Convert.ToInt32(dataGridView1.CurrentRow.Cells[7].Value) <= 0)
                        {
                            oracleConnection1.Open();
                            oracleCommand1.CommandText = "SELECT rv_seq.nextval FROM DUAL";
                            DataRow mynewreserveRow = reserveTable.NewRow();
                            mynewreserveRow["C_No"] = identityRow["C_No"].ToString();
                            mynewreserveRow["GT_No"] = dataGridView1.CurrentRow.Cells[1].Value.ToString();
                            mynewreserveRow["G_No"] = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                            mynewreserveRow["RS_No"] = oracleCommand1.ExecuteScalar().ToString();
                            mynewreserveRow["RS_Number"] = 1;
                            mynewreserveRow["RS_ReserveDate"] = DateTime.Now.ToShortDateString();

                            if (MessageBox.Show(dataGridView1.CurrentRow.Cells[2].Value.ToString() + "을(를) 예약하시겠습니까?", "예약하기", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                reserveTable.Rows.Add(mynewreserveRow);
                                reserveTableAdapter1.Update(dataSet11.RESERVE);



                                oracleCommand2.CommandText = "UPDATE GOODS SET G_STOCK = G_STOCK - 1" + 
                                    " WHERE G_NO = " + dataGridView1.CurrentRow.Cells[0].Value.ToString() + "and GT_NO = " + dataGridView1.CurrentRow.Cells[1].Value.ToString();
                                oracleCommand2.ExecuteNonQuery();

                                gOODSTableAdapter.Fill(dataSet11.GOODS);
                                goodsTable = dataSet11.Tables["GOODS"];

                                MessageBox.Show("예약에 성공하셨습니다.\n예약한 물품 대여 시에는 포인트 사용과 적립이 불가합니다.");
                            }
                            else
                            {
                            }
                            oracleConnection1.Close();
                        }
                        else
                        {
                            MessageBox.Show("해당 물품은 재고가 있어, 예약이 불가합니다.");
                        }
                    }
                }
            }
        }

        //리뷰 보기 버튼 클릭 시
        private void button15_Click(object sender, EventArgs e)
        {
            panel4.Visible = true;
            rEVIEWTableAdapter.Fill(dataSet11.REVIEW);
            reviewTable = dataSet11.Tables["REVIEW"];
            label55.Text = dataGridView1.CurrentRow.Cells[2].Value.ToString();
            rEVIEWBindingSource.Filter = "G_No = " + dataGridView1.CurrentRow.Cells[0].Value.ToString()
                 + " and GT_No = " + dataGridView1.CurrentRow.Cells[1].Value.ToString();

            textBox7.Text = "내용을 보고 싶으시면 내용 보기 버튼을 클릭해주세요.";
        }
        /************************리뷰 보기화면*****************************/
        //back 버튼 클릭시
        private void button18_Click(object sender, EventArgs e)
        {
            panel4.Visible = false;
        }
        //별점 순으로 보기 체크박스
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                rEVIEWBindingSource.Sort = "RV_Star DESC";
            }
            else
            {
                rEVIEWBindingSource.RemoveSort();
            }
        }
        //리뷰 내용 보기 버튼
        private void button19_Click(object sender, EventArgs e)
        {
            textBox7.Text = dataGridView2.CurrentRow.Cells[6].Value.ToString();
        }

        // 일간 확인하기 버튼
        private void button14_Click(object sender, EventArgs e)
        {
            TopForm topform = new TopForm();
            topform.passValue1 = "일간";

            topform.Show();
        }
        //주간
        private void button11_Click(object sender, EventArgs e)
        {
            TopForm topform = new TopForm();
            topform.passValue1 = "주간";

            topform.Show();
        }
        //월간
        private void button12_Click(object sender, EventArgs e)
        {
            TopForm topform = new TopForm();
            topform.passValue1 = "월간";

            topform.Show();
        }
        //연간
        private void button13_Click(object sender, EventArgs e)
        {
            TopForm topform = new TopForm();
            topform.passValue1 = "연간";

            topform.Show();
        }
        //전체 보기 버튼
        private void button17_Click(object sender, EventArgs e)
        {
            gOODSBindingSource.RemoveFilter();
        }


        //이름 라벨 클릭 시, 마이 페이지 열기
        private void nameLabel_DoubleClick(object sender, EventArgs e)
        {
            panel3.Visible = true;

            //마이페이지 정보 로드
            label20.Text = identityRow["C_Name"].ToString();
            label21.Text = identityRow["C_Email"].ToString();
            label22.Text = identityRow["C_Phone"].ToString();
            label23.Text = identityRow["C_Addr"].ToString();
            label24.Text = customerRank;
            label39.Text = identityRow["C_Point"].ToString();
            myFindRow = rentTable.Select("C_No = " + identityRow["C_No"].ToString());
            checkBox1.Checked = false;

            //블랙리스트 상태라면
            DataRow[] foundRows2 = blackListTable.Select("C_No = " + identityRow["C_No"].ToString());
            if (foundRows2.Length > 0)
            {
                DateTime bldt = Convert.ToDateTime(foundRows2[0]["BL_RegisterDate"]);
                bldt = bldt.AddDays(Convert.ToInt32(foundRows2[0]["BL_KeepDay"]));
                int remainDay = bldt.Day - DateTime.Now.Day;

                if (foundRows2.Length > 0 && remainDay >= 0)
                {
                    label42.Visible = true;
                    label43.Visible = true;
                    label44.Visible = true;
                    label45.Visible = true;

                    label42.Text = "현재 블랙리스트 상태입니다.";
                    label43.Text = "사유 : " + foundRows2[0]["BL_TYPE"].ToString();
                    label44.Text = "앞으로 " + remainDay + " 일 동안 물품대여가 불가합니다.";
                    label45.Text = "상세 사유가 궁금하시거나, 해당 사항으로 건의하실게 있으시면 카운터를 찾아주세요.";
                }
            }

            comboBox2.Items.Clear();
            int a = 0;
            goodsTypeTable.DefaultView.Sort = "GT_NO asc";
            comboBox2.Items.Add("전체 보기");
            foreach (DataRow myrow in goodsTypeTable.Rows)
            {
                if (a != Convert.ToInt32(myrow["GT_NO"]) / 10)
                {
                    string c = myrow["GT_NAME"].ToString();
                    string[] b = c.Split(' ');
                    comboBox2.Items.Add(b[1]);
                    a = Convert.ToInt32(myrow["GT_NO"]) / 10;
                }
            }
            //분류 기본 값 0으로 주기
            comboBox2.SelectedIndex = 0;
        }

        /************************마이 페이지*****************************/
        //back 버튼
        private void button7_Click(object sender, EventArgs e)
        {
            panel3.Visible = false;
            listBox1.ClearSelected();
        }

        //반납 여부 체크 박스
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            pictureBox2.Visible = true;
            //반납된 책을 보고 있다면 리뷰 작성하기 버튼 활성화
            if (checkBox1.Checked)
            {
                button9.Visible = true;
            }
            else
            {
                button9.Visible = false;
            }
            int a = comboBox2.SelectedIndex;
            comboBox2.SelectedIndex = -1;
            comboBox2.SelectedIndex = a;

        }

        //화면 아무데나 누르면 리스트박스 선택해제
        private void panel3_MouseDown(object sender, MouseEventArgs e)
        {
            listBox1.ClearSelected();
        }

        //빨갛게 칠하기

        public class MyListBoxItem
        {
            public MyListBoxItem(Color c, string m)
            {
                ItemColor = c;
                Message = m;
            }
            public Color ItemColor { get; set; }
            public string Message { get; set; }
        }
        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1) return;    //아이템이 없는 경우 는 할 일이 없습니다.
            MyListBoxItem item = listBox1.Items[e.Index] as MyListBoxItem;
            if (item != null)
            {
                e.Graphics.DrawString(
                    item.Message,
                    listBox1.Font,
                    new SolidBrush(item.ItemColor),
                    0,
                    e.Index * listBox1.ItemHeight
                );
            }
            else
            {
                // The item isn't a MyListBoxItem, do something about it
            }
        }

        // 콤보박스의 물품 종류 변경시
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listBox1.ClearSelected();
            int b = listBox1.SelectedIndex;

            //전체 보기
            if (comboBox2.SelectedIndex == 0)
            {
                foreach (DataRow mydatarow in myFindRow)
                {
                    DataRow tmpRow = mydatarow.GetParentRow(GR);
                    DataRow tmpRow2 = tmpRow.GetParentRow(GTG);
                    if (checkBox1.Checked && mydatarow["R_IsReturn"].ToString() == "1")
                    {
                        if (Convert.ToInt32(mydatarow["R_NDay"]) > Convert.ToInt32(tmpRow2["GT_ReturnDate"]))
                        {
                            listBox1.Items.Add(new MyListBoxItem(Color.Red, tmpRow["G_Name"].ToString()));
                        }
                        else
                        {
                            listBox1.Items.Add(new MyListBoxItem(Color.Black, tmpRow["G_Name"].ToString()));
                        }
                    }
                    else if (!checkBox1.Checked && mydatarow["R_IsReturn"].ToString() == "0")
                    {
                        if (Convert.ToInt32(mydatarow["R_NDay"]) > Convert.ToInt32(tmpRow2["GT_ReturnDate"]))
                        {
                            listBox1.Items.Add(new MyListBoxItem(Color.Red, tmpRow["G_Name"].ToString()));
                        }
                        else
                        {
                            listBox1.Items.Add(new MyListBoxItem(Color.Black, tmpRow["G_Name"].ToString()));
                        }
                    }
                }
            }
            //만화
            int h = comboBox2.SelectedIndex;
            foreach (DataRow mydatarow in myFindRow)
            {
                int type = Convert.ToInt32(mydatarow["GT_No"]) / 10;
                if (type == h)
                {
                    DataRow tmpRow = mydatarow.GetParentRow(GR);
                    DataRow tmpRow2 = tmpRow.GetParentRow(GTG);
                    if (checkBox1.Checked && mydatarow["R_IsReturn"].ToString() == "1")
                    {
                        if (Convert.ToInt32(mydatarow["R_NDay"]) > Convert.ToInt32(tmpRow2["GT_ReturnDate"]))
                        {
                            listBox1.Items.Add(new MyListBoxItem(Color.Red, tmpRow["G_Name"].ToString()));
                        }
                        else
                        {
                            listBox1.Items.Add(new MyListBoxItem(Color.Black, tmpRow["G_Name"].ToString()));
                        }
                    }
                    else if (!checkBox1.Checked && mydatarow["R_IsReturn"].ToString() == "0")
                    {
                        if (Convert.ToInt32(mydatarow["R_NDay"]) > Convert.ToInt32(tmpRow2["GT_ReturnDate"]))
                        {
                            listBox1.Items.Add(new MyListBoxItem(Color.Red, tmpRow["G_Name"].ToString()));
                        }
                        else
                        {
                            listBox1.Items.Add(new MyListBoxItem(Color.Black, tmpRow["G_Name"].ToString()));
                        }
                    }
                }
            }
        }
        //물품 선택 시
        //리스트 박스 내에서 선택했다면
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (index == -1)
            {
                pictureBox2.Visible = true;
            }
            else
            {
                MyListBoxItem item = listBox1.Items[index] as MyListBoxItem;
                string tmpGoods = item.Message;
                DataRow[] tmpRows = goodsTable.Select("G_Name = '" + tmpGoods + "'");
                //리뷰 작성시 패스밸류
                passRowGoods = tmpRows[0];
                DataRow gtRow = tmpRows[0].GetParentRow(GTG);
                foreach (DataRow mydatarow in myFindRow)
                {
                    if (mydatarow["G_No"].ToString() == tmpRows[0]["G_No"].ToString())
                    {
                        pictureBox2.Visible = false;

                        label32.Text = gtRow["GT_Name"].ToString();
                        label33.Text = tmpRows[0]["G_Name"].ToString();
                        DateTime rentdt = Convert.ToDateTime(mydatarow["R_Date"]);
                        label34.Text = rentdt.ToShortDateString();
                        int rtdate = Convert.ToInt32(gtRow["GT_ReturnDate"]);
                        DateTime returndt = rentdt.AddDays(rtdate);
                        label35.Text = returndt.ToShortDateString();
                        int dday = DateTime.Now.Day - returndt.Day;
                        if (dday > 0 && mydatarow["R_IsReturn"].ToString() == "0")
                        {
                            label31.Text = "(반납일을 " + dday.ToString() + "일 초과하셨습니다.)";
                            label29.Visible = true;
                            label30.Visible = true;
                            label36.Visible = true;
                            label37.Visible = true;

                            DataRow[] fineRow = mydatarow.GetChildRows(RF);
                            label36.Text = fineRow[0]["F_OverDate"].ToString();
                            label37.Text = fineRow[0]["F_Fine"].ToString();

                        }
                        else if (dday == 0 && mydatarow["R_IsReturn"].ToString() == "0")
                        {
                            label31.Text = "(오늘이 반납일 입니다.)";
                            label29.Visible = false;
                            label30.Visible = false;
                            label36.Visible = false;
                            label37.Visible = false;
                        }
                        else if (dday < 0 && mydatarow["R_IsReturn"].ToString() == "0")
                        {
                            dday *= -1;
                            label31.Text = "(반납일까지 " + dday.ToString() + "일 남았습니다.)";
                            label29.Visible = false;
                            label30.Visible = false;
                            label36.Visible = false;
                            label37.Visible = false;
                        }
                        //반납 완료된 물품이라면
                        if (mydatarow["R_IsReturn"].ToString() == "1")
                        {
                            label28.Visible = false;
                            label35.Visible = false;
                            label29.Visible = false;
                            label30.Visible = false;
                            label36.Visible = false;
                            label37.Visible = false;
                            label31.Visible = false;
                        }
                        else
                        {
                            label28.Visible = true;
                            label35.Visible = true;
                            label31.Visible = true;
                        }
                        //dt.ToString("dd");
                    }
                }
            }
        }
        //로그아웃 버튼 클릭 시
        private void button8_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("로그아웃 하시겠습니까?", "로그아웃", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                listBox1.ClearSelected();
                identityRow = null;
                panel1.Visible = false;
                panel3.Visible = false;
                comboBox1.SelectedIndex = -1;
                gOODSBindingSource.RemoveFilter();
            }
            else
            {
            }
        }
        //리뷰 작성하기 버튼 클릭 시
        private void button9_Click(object sender, EventArgs e)
        {
            DataRow[] rvRow = reviewTable.Select("C_No = " + identityRow["C_No"]);
            if (rvRow.Length > 0)
            {
                MessageBox.Show("이미 리뷰를 등록하셨습니다. 리뷰는 하나만 작성 가능합니다.");
            }
            else
            {
                ReviewForm reviewForm = new ReviewForm();
                reviewForm.passValue1 = passRowGoods["G_Name"].ToString();
                reviewForm.passValue2 = identityRow["C_No"].ToString();

                reviewForm.ShowDialog();
            }

        }

        //직원 로그인 성공 시
        private void staffLoginSuccess()
        {
            label62.Text = identityRow["S_NAME"].ToString();
            if (identityRow["S_RANK"].ToString() == "1")
            {
                label63.Text = "직급 : 직원";
            }
            else if (identityRow["S_RANK"].ToString() == "2")
            {
                label63.Text = "직급 : 매니저";
            }
            else if (identityRow["S_RANK"].ToString() == "3")
            {
                label63.Text = "직급 : 사장";
                button100.Visible = true;
            }

            label72.Text = identityRow["S_NO"].ToString();
            label73.Text = identityRow["S_ID"].ToString();
            label74.Text = identityRow["S_NAME"].ToString();
            if (identityRow["S_RANK"].ToString() == "1")
            {
                label75.Text = "직원";
            }
            else if (identityRow["S_RANK"].ToString() == "2")
            {
                label75.Text = "매니저";
            }
            else if (identityRow["S_RANK"].ToString() == "3")
            {
                label75.Text = "사장";
            }
            label76.Text = identityRow["S_ADDR"].ToString();
            label77.Text = identityRow["S_PHONE"].ToString();
        }

        /************************직원 화면*****************************/
        //회원 버튼 클릭하면
        private void button22_Click(object sender, EventArgs e)
        {
            button22.Visible = false;
            button23.Visible = true;
        }
        //용품 버튼 클릭하면
        private void button23_Click(object sender, EventArgs e)
        {
            button23.Visible = false;
            button22.Visible = true;
        }
        //패널 클릭하면
        private void panel5_Click(object sender, EventArgs e)
        {
            button22.Visible = true;
            button23.Visible = true;
        }
        //회원 이름 클릭 시
        private void label62_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            button100.Visible = false;
            button110.Visible = false;
            button22.Visible = true;
            button23.Visible = true;
            panel6.Visible = true;
            
        }
        /************************직원 마이 페이지 화면*****************************/
        //back 버튼 클릭 시
        private void button30_Click(object sender, EventArgs e)
        {
            if (identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            panel6.Visible = false;
        }
        //로그아웃 버튼 클릭 시
        private void button31_Click(object sender, EventArgs e)
        {
            panel5.Visible = false;
            panel6.Visible = false;
            identityRow = null;
            accessTab = false;
            button100.Visible = false;

            comboBox1.SelectedIndex = -1;
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
        }
        //정보 수정 버튼
        private void button32_Click(object sender, EventArgs e)
        {
            panel7.Visible = true;
        }
        //수정하기 버튼 클릭시
        private void button34_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("정보를 수정하시겠습니까?", "정보수정", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (textBox12.Text == identityRow["S_PW"].ToString())
                {
                    oracleConnection1.Open();
                    oracleCommand2.CommandText = "UPDATE staff SET S_ADDR = '" + textBox8.Text + "' WHERE S_NO =" + identityRow["S_NO"].ToString();
                    oracleCommand3.CommandText = "UPDATE staff SET S_PHONE = '" + textBox9.Text + "' WHERE S_NO = " + identityRow["S_NO"].ToString();
                    oracleCommand2.ExecuteNonQuery();
                    oracleCommand3.ExecuteNonQuery();
                    oracleConnection1.Close();

                    staffTableAdapter1.Fill(dataSet11.STAFF);
                    staffTable = dataSet11.Tables["STAFF"];
                    DataRow[] foundRows = staffTable.Select("S_No = " + label72.Text);
                    identityRow = foundRows[0];
                    label76.Text = identityRow["S_ADDR"].ToString();
                    label77.Text = identityRow["S_PHONE"].ToString();

                    MessageBox.Show("정보 수정이 완료되었습니다.");
                    panel7.Visible = false;
                }
                else
                {
                    MessageBox.Show("비밀번호가 틀렸습니다. 다시 입력해주세요.");
                }
            }
            else
            {
            }
        }
        //비밀 번호 변경 버튼
        private void button33_Click(object sender, EventArgs e)
        {
            panel8.Visible = true;
            textBox11.Text = "";
            textBox10.Text = "";
            textBox13.Text = "";

        }
        //변경하기 버튼 클릭 시
        private void button35_Click(object sender, EventArgs e)
        {
            if (textBox13.Text == "" || textBox10.Text == "" || textBox11.Text == "")
            {
                MessageBox.Show("입력이 제대로 되지 않았습니다.");
            }
            else
            {
                if (textBox13.Text == identityRow["S_PW"].ToString())
                {
                    if (textBox10.Text == textBox11.Text)
                    {
                        if (MessageBox.Show("비밀번호를 변경하시겠습니까?", "비밀번호 변경", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            oracleConnection1.Open();
                            oracleCommand2.CommandText = "UPDATE staff SET S_PW = '" + textBox11.Text + "' WHERE S_NO =" + identityRow["S_NO"].ToString();
                            oracleCommand2.ExecuteNonQuery();
                            oracleConnection1.Close();

                            staffTableAdapter1.Fill(dataSet11.STAFF);
                            staffTable = dataSet11.Tables["STAFF"];
                            DataRow[] foundRows = staffTable.Select("S_No = " + label72.Text);
                            identityRow = foundRows[0];
                            MessageBox.Show("비밀번호 변경이 완료 되었습니다.");
                            panel8.Visible = false;
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        MessageBox.Show("비밀번호가 재입력한 비밀번호와 일치하지 않습니다. 다시 확인해주세요.");
                    }
                }
                else
                {
                    MessageBox.Show("현재 비밀번호를 잘못 입력하셨습니다. 확인후 다시 입력해주세요.");
                }
            }
        }
        //대여 버튼 클릭
        private void button20_Click(object sender, EventArgs e)
        {
            panel9.Visible = true;
            button100.Visible = false;
            button110.Visible = false;
            button22.Visible = true;
            button23.Visible = true;
        }
        /************************대여 화면*****************************/
        //back 버튼 클릭
        private void button42_Click(object sender, EventArgs e)
        {
            if (identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            panel9.Visible = false;
        }
        //물품 이름 검색
        private void button36_Click(object sender, EventArgs e)
        {
            if (textBox15.Text != "")
                gOODSBindingSource.Filter = "g_name like '%" + textBox15.Text + "%'";
        }
        //고객 이름 검색
        private void button37_Click(object sender, EventArgs e)
        {
            if (textBox14.Text != "")
                cUSTOMERBindingSource.Filter = "C_name like '%" + textBox14.Text + "%'";
        }
        //물품 전체 보기
        private void button43_Click(object sender, EventArgs e)
        {
            gOODSBindingSource.RemoveFilter();
        }
        //고객 전체 보기
        private void button44_Click(object sender, EventArgs e)
        {
            cUSTOMERBindingSource.RemoveFilter();
        }

        int result = 0;
        //물품 현재 선택된 셀이 바뀔 때
        private void dataGridView3_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            result = Convert.ToInt32(dataGridView3.CurrentRow.Cells[3].Value);
            label88.Text = result.ToString();
        }
        //포인트 사용 버튼
        private void button39_Click(object sender, EventArgs e)
        {
            if (textBox16.Text != "")
            {
                if (button39.Text == "포인트 사용")
                {
                    if (Convert.ToInt32(dataGridView4.CurrentRow.Cells[5].Value) >= Convert.ToInt32(textBox16.Text))
                    {
                        result -= Convert.ToInt32(textBox16.Text);
                        button39.Text = "포인트 사용 해제";
                    }
                    else
                    {
                        MessageBox.Show("포인트가 부족합니다.");
                    }
                }
                else if (button39.Text == "포인트 사용 해제")
                {
                    result = Convert.ToInt32(dataGridView3.CurrentRow.Cells[3].Value);
                    button39.Text = "포인트 사용";
                }
                label88.Text = result.ToString();
            }
            else
            {
                MessageBox.Show("포인트를 입력해주세요.");
            }
        }
        int realPoint = 0;
        //대여 버튼
        private void button38_Click(object sender, EventArgs e)
        {
            oracleConnection1.Open();
            //재고 없으면 대여 불가
            if (Convert.ToInt32(dataGridView3.CurrentRow.Cells[7].Value) <= 0)
            {
                MessageBox.Show("재고가 없습니다.");
            }
            else
            {
                //블랙리스트 대여 불가
                DataRow[] dt = blackListTable.Select("C_NO = " + dataGridView4.CurrentRow.Cells[0].Value.ToString());
                if (dt.Length > 0)
                {
                    MessageBox.Show("해당 고객은 블랙리스트 입니다.");
                }
                else
                {
                    //고객 포인트 차감
                    if (button39.Text == "포인트 사용 해제") //포인트를 사용했다는 의미
                    {
                        int point = Convert.ToInt32(dataGridView4.CurrentRow.Cells[5].Value) - Convert.ToInt32(textBox16.Text);
                        oracleCommand1.CommandText = "UPDATE Customer SET C_Point = '" + point.ToString() + "' WHERE C_NO =" + dataGridView4.CurrentRow.Cells[0].Value.ToString();
                        oracleCommand1.ExecuteNonQuery();
                    }
                    //재고 줄이기
                    int num = Convert.ToInt32(dataGridView3.CurrentRow.Cells[7].Value) - 1;
                    oracleCommand2.CommandText = "UPDATE Goods SET g_stock = " + num + " WHERE g_NO =" + dataGridView3.CurrentRow.Cells[0].Value.ToString() + " and GT_NO = " + dataGridView3.CurrentRow.Cells[1].Value.ToString();
                    oracleCommand2.ExecuteNonQuery();

                    
                    
                    //대여에 등록
                    DataRow[] dt1 = goodsTable.Select("G_NO = " + dataGridView3.CurrentRow.Cells[0].Value.ToString() + " and GT_NO = " + dataGridView3.CurrentRow.Cells[1].Value.ToString());
                    DataRow gttRow = dt1[0].GetParentRow(GTG);
                    oracleCommand1.CommandText = "Insert into rent values(" +
                        dataGridView4.CurrentRow.Cells[0].Value.ToString() + ", " +
                        dataGridView3.CurrentRow.Cells[0].Value.ToString() + ", " + dataGridView3.CurrentRow.Cells[1].Value.ToString() + ", to_date('" +
                        DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd'), 1, " + gttRow["GT_RETURNDATE"].ToString() + ", " +
                        label88.Text + ", 0 )";
                    oracleCommand1.ExecuteNonQuery();

                    //대여 담당자 등록
                    oracleCommand3.CommandText = "Insert into managerent values('" + identityRow["S_ID"].ToString() + "', to_date('" + DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd'), "
                        + dataGridView4.CurrentRow.Cells[0].Value.ToString() + ", " + dataGridView3.CurrentRow.Cells[0].Value.ToString() + ", " + dataGridView3.CurrentRow.Cells[1].Value.ToString() + ")";
                    oracleCommand3.ExecuteNonQuery();

                    // 포인트 주기
                    double defaultP = 0.01;
                    DataRow[] dt3 = whiteListTable.Select("C_NO = " + dataGridView4.CurrentRow.Cells[0].Value.ToString());
                    if (dt3.Length > 0)
                    {
                        if (dt3[0]["WL_POINTCLASS"].ToString() == "1")
                        {
                            defaultP = 0.03;
                        }
                        else if (dt3[0]["WL_POINTCLASS"].ToString() == "2")
                        {
                            defaultP = 0.04;
                        }
                        else if (dt3[0]["WL_POINTCLASS"].ToString() == "3")
                        {
                            defaultP = 0.05;
                        }
                    }
                    double giveP = Convert.ToDouble(label88.Text);
                    double plus = giveP * defaultP;
                    oracleCommand5.CommandText = "SELECT C_POINT FROM CUSTOMER WHERE  C_NO = " + dataGridView4.CurrentRow.Cells[0].Value.ToString();

                    double nowP = Convert.ToDouble(oracleCommand5.ExecuteScalar());
                    realPoint = Convert.ToInt32(nowP);
                    nowP += plus;
                    oracleCommand4.CommandText = "UPDATE Customer SET C_Point = '" + Convert.ToInt32(nowP).ToString() + "' WHERE C_NO =" + dataGridView4.CurrentRow.Cells[0].Value.ToString();
                    oracleCommand4.ExecuteNonQuery();
                    //재 바인딩
                    string a = identityRow["S_NO"].ToString();
                    reRoad();
                    DataRow[] dt6 = goodsTable.Select("G_Stock <= 0 and G_IsSoldOut = 0");
                    if (dt6.Length > 0)
                    {
                        foreach (DataRow myrow in dt6)
                        {
                            oracleCommand3.CommandText = "UPDATE Goods SET G_IsSoldOut = 1 " +
                                            "WHERE G_NO = " + myrow["G_NO"].ToString() + " and GT_NO = "
                                            + myrow["GT_NO"].ToString();
                            oracleCommand3.ExecuteNonQuery();
                        }
                    }
                    gOODSTableAdapter.Fill(dataSet11.GOODS);
                    goodsTable = dataSet11.Tables["GOODS"];
                    MessageBox.Show("대여가 완료되었습니다.");

                    DataRow[] dt4 = staffTable.Select("S_NO = " + a);
                    identityRow = dt4[0];
                    textBox16.Text = "";
                }
            }
            oracleConnection1.Close();
        }
        //대여 목록 탭
        //담당자 확인
        private void button41_Click(object sender, EventArgs e)
        {
            oracleConnection1.Open();
            oracleCommand1.CommandText = "SELECT S_ID FROM MANAGERENT WHERE C_NO = " +
                dataGridView5.CurrentRow.Cells[0].Value.ToString() + " and G_NO = " +
                dataGridView5.CurrentRow.Cells[1].Value.ToString() + " and GT_NO = " +
                dataGridView5.CurrentRow.Cells[2].Value.ToString();
            string id = oracleCommand1.ExecuteScalar().ToString();
            DataRow[] dt = staffTable.Select("S_ID = '" + id + "'");

            MessageBox.Show("해당 대여 항목의 담당자는 " + dt[0]["S_NAME"].ToString() + " (" + id + ") 입니다.");
            oracleConnection1.Close();
        }

        //예약 탭
        //대여 버튼
        private void button40_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("해당 예약 물품을 대여 하겠습니까?", "예약 물품 대여", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                oracleConnection1.Open();
                //재고 처리
                oracleCommand4.CommandText = "UPDATE goods SET g_stock = g_stock + 1 WHERE g_no = " + dataGridView6.CurrentRow.Cells[2].Value.ToString() + " and GT_NO = " + dataGridView6.CurrentRow.Cells[3].Value.ToString();
                oracleCommand4.ExecuteNonQuery();
                
                //대여에 등록
                oracleCommand5.CommandText = "SELECT G_PRICE FROM GOODS WHERE g_no = " + dataGridView6.CurrentRow.Cells[2].Value.ToString() + " and GT_NO = " + dataGridView6.CurrentRow.Cells[3].Value.ToString();
                string price = oracleCommand5.ExecuteScalar().ToString();
                DataRow[] dt1 = goodsTable.Select("G_NO = " + dataGridView6.CurrentRow.Cells[2].Value.ToString() + " and GT_NO = " + dataGridView6.CurrentRow.Cells[3].Value.ToString());
                DataRow gttRow = dt1[0].GetParentRow(GTG);
                oracleCommand1.CommandText = "Insert into rent values(" +
                    dataGridView6.CurrentRow.Cells[1].Value.ToString() + ", " +
                    dataGridView6.CurrentRow.Cells[2].Value.ToString() + ", " + dataGridView6.CurrentRow.Cells[3].Value.ToString() + ", to_date('" +
                    DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd'), 1, " + gttRow["GT_RETURNDATE"].ToString() + ", " +
                    price + ", 0 )";
                oracleCommand1.ExecuteNonQuery();

                //대여 담당자 등록
                oracleCommand3.CommandText = "Insert into managerent values('" + identityRow["S_ID"].ToString() + "', to_date('" + DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd'), "
                    + dataGridView6.CurrentRow.Cells[1].Value.ToString() + ", " + dataGridView6.CurrentRow.Cells[2].Value.ToString() + ", " + dataGridView6.CurrentRow.Cells[3].Value.ToString() + ")";
                oracleCommand3.ExecuteNonQuery();

                oracleConnection1.Close();


                gOODSTableAdapter.Fill(dataSet11.GOODS);
                goodsTable = dataSet11.Tables["GOODS"];

                rentTableAdapter1.Fill(dataSet11.RENT);
                rentTable = dataSet11.Tables["RENT"];

                rESERVEBindingSource.RemoveCurrent();
                rESERVEBindingSource.EndEdit();
                reserveTableAdapter1.Update(dataSet11.RESERVE);
            }
            else { }
        }
        //예약 취소 버튼
        private void button46_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("예약을 취소하시겠습니까?", "예약 취소", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                rESERVEBindingSource.RemoveCurrent();
                rESERVEBindingSource.EndEdit();
                reserveTableAdapter1.Update(dataSet11.RESERVE);

                oracleConnection1.Open();
                //재고 처리
                oracleCommand2.CommandText = "UPDATE goods SET g_stock = g_stock + 1 WHERE g_no = " + dataGridView6.CurrentRow.Cells[2].Value.ToString() + " and GT_NO = " + dataGridView6.CurrentRow.Cells[3].Value.ToString();
                oracleCommand2.ExecuteNonQuery();
                oracleConnection1.Close();

                gOODSTableAdapter.Fill(dataSet11.GOODS);
                goodsTable = dataSet11.Tables["GOODS"];
            }
            else { }
        }
        //반납 버튼 클릭 시
        private void button21_Click(object sender, EventArgs e)
        {
            button22.Visible = true;
            button23.Visible = true;
            panel10.Visible = true;
            button100.Visible = false;
            button110.Visible = false;
            rENTBindingSource1.Filter = "R_ISRETURN = 0";
            checkBox5.Checked = false;
        }

        /************************반납 화면*****************************/
        //back 버튼 클릭
        private void button50_Click(object sender, EventArgs e)
        {
            if (identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            panel10.Visible = false;
        }
        //반납 패널
        string filterIt = "";
        string isre = "R_ISRETURN = 0";
        //검색 버튼
        private void button48_Click(object sender, EventArgs e)
        {
            if (textBox6.Text != "")
            {
                rENTBindingSource1.RemoveFilter();
                filterIt = "";
                DataRow[] dt = customerTable.Select("C_NAME = '" + textBox6.Text + "'");
                if (dt.Length > 0)
                {
                    foreach (DataRow myrow in dt)
                    {
                        if (filterIt != "")
                        {
                            filterIt += " or C_NO = " + myrow["C_NO"].ToString();
                        }
                        else
                        {
                            filterIt += "C_NO = " + myrow["C_NO"].ToString();
                        }
                    }

                    rENTBindingSource1.Filter = isre + " and " + filterIt;
                }
                else
                {
                    MessageBox.Show("등록된 이름이 없습니다.");
                }
            }
        }
        //데이터 그리드 뷰 셀 클릭 시
        private void dataGridView7_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataRow[] dt1 = customerTable.Select("C_NO = " + dataGridView7.CurrentRow.Cells[0].Value.ToString());
            label100.Text = dt1[0]["C_NAME"].ToString();
            label101.Text = dt1[0]["C_Email"].ToString();

            DataRow[] dt2 = goodsTable.Select("G_NO = " + dataGridView7.CurrentRow.Cells[1].Value.ToString() + " and GT_NO = " + dataGridView7.CurrentRow.Cells[2].Value.ToString());
            label94.Text = dt2[0]["G_NAME"].ToString();
            DataRow dt3 = dt2[0].GetParentRow(GTG);
            label96.Text = dt3["GT_NAME"].ToString();
        }
        //반납 완료된 것 보기
        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
            {
                isre = "R_ISRETURN = 1";
                if (filterIt != "")
                {
                    rENTBindingSource1.Filter = isre + " and " + filterIt;
                }
                else
                {
                    rENTBindingSource1.Filter = isre;
                }
            }
            else
            {
                isre = "R_ISRETURN = 0";
                if (filterIt != "")
                {
                    rENTBindingSource1.Filter = isre + " and " + filterIt;
                }
                else
                {
                    rENTBindingSource1.Filter = isre;
                }
            }
        }
        //전체 보기 버튼
        private void button51_Click(object sender, EventArgs e)
        {
            rENTBindingSource1.RemoveFilter();
            rENTBindingSource1.Filter = isre;
            filterIt = "";
        }
        //반납 완료 버튼
        private void button45_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("반납 하시겠습니까?", "반납", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string z = "";
                bool g = false;
                //반납 처리 하기 + 반납 후 재고 올리기
                if (listBox2.Items.Count != 0)
                {
                    oracleConnection1.Open();
                    for (int i = 0; i < listBox2.Items.Count; i++)
                    {
                        string a = listBox2.Items[i].ToString();
                        string[] b = a.Split('\t');
                        //연체된 항목이 있으면 반납처리하지 않고, 연체 탭으로 이동
                        DataRow[] dt3 = fineTable.Select("C_NO = " + b[0] + "and G_NO = " + b[1] + " and GT_NO = " + b[2]);
                        if (dt3.Length > 0)
                        {
                            z += a + "\n";
                            g = true;
                        }
                        else
                        {
                            oracleCommand1.CommandText = "UPDATE rent SET r_isReturn =  1 WHERE c_no = " +
                                b[0] + " and G_NO = " + b[1] + " and GT_NO = " + b[2] + " and R_DATE = to_date('" + b[3] + "','yyyy-mm-dd')";
                            oracleCommand1.ExecuteNonQuery();
                            //재고 올리기
                            reserveTable.DefaultView.Sort = "RS_NO asc";
                            DataRow[] dt = reserveTable.Select("G_NO = " + b[1] + " and GT_NO = " + b[2]);
                            if (dt.Length == 0)
                            {
                                oracleCommand2.CommandText = "UPDATE goods SET g_Stock =  g_STock + 1 WHERE G_NO = " + b[1] + " and GT_NO = " + b[2];
                                oracleCommand2.ExecuteNonQuery();
                            }
                            else // 예약 항목에 있으면
                            {
                                //메일 보내기
                                DataRow[] dt2 = customerTable.Select("C_NO = " + dt[0]["C_NO"].ToString());
                                string d = dt2[0]["C_Email"].ToString();
                                MailMessage msg = new MailMessage("dofl0119@gmail.com", "dofl011@naver.com",
                                "Subject : 안녕하세요, 언덕위의 책과 비디오 대여점입니다.",
                                "안녕하세요, 고객님. \n 예약하셨던 물품이 입고되어 메일 드립니다. 2일 내에 찾아와주시면 감사하겠습니다. \n 이틀이 지나면 자동으로 예약이 취소됩니다.");

                                // SmtpClient 셋업 (Live SMTP 서버, 포트)
                                SmtpClient smtp = new SmtpClient("smtp.live.com", 587);
                                smtp.EnableSsl = true;

                                // Live 또는 Hotmail 계정과 암호 필요
                                smtp.Credentials = new NetworkCredential("dofl0119@gmail.com", "6602gjqm19!");

                                // 발송
                                smtp.Send(msg);

                                oracleCommand4.CommandText = "UPDATE reserve SET RS_ALRAMDATE = to_date('" +
                                    DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd') WHERE G_NO = " + dt[0]["G_NO"] + " and GT_NO = " + dt[0]["GT_NO"] +
                                    " and C_NO = " + dt[0]["C_NO"];
                                oracleCommand4.ExecuteNonQuery();
                                MessageBox.Show("예약되어있던 항목이 있어 이메일을 보냈습니다.");
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("반납 목록에 반납 항목을 추가 해주세요.");
                }

                if (g)
                {
                    MessageBox.Show(z + "연체 항목이 있습니다.\n이를 제외하고 반납처리 되었습니다.\n연체 탭으로 이동합니다.");
                    this.tabControl2.SelectedIndex = 1;
                }
                //반납 동기화&반영
                rentTableAdapter1.Fill(dataSet11.RENT);
                rentTable = dataSet11.Tables["RENT"];

                gOODSTableAdapter.Fill(dataSet11.GOODS);
                goodsTable = dataSet11.Tables["GOODS"];

                reserveTableAdapter1.Fill(dataSet11.RESERVE);
                reserveTable = dataSet11.Tables["RESERVE"];

                rENTBindingSource1.RemoveFilter();
                rENTBindingSource1.Filter = isre;

                DataRow[] dt6 = goodsTable.Select("G_Stock > 0 and G_IsSoldOut = 1");
                if (dt6.Length > 0)
                {
                    foreach (DataRow myrow in dt6)
                    {
                        oracleCommand3.CommandText = "UPDATE Goods SET G_IsSoldOut = 0 " +
                                        "WHERE G_NO = " + myrow["G_NO"].ToString() + " and GT_NO = "
                                        + myrow["GT_NO"].ToString();
                        oracleCommand3.ExecuteNonQuery();
                    }
                }
                gOODSTableAdapter.Fill(dataSet11.GOODS);
                goodsTable = dataSet11.Tables["GOODS"];

                oracleConnection1.Close();
                listBox2.Items.Clear();
            }
            else { }
        }
        //반납 목록에 추가하기
        private void button52_Click(object sender, EventArgs e)
        {
            string a = dataGridView7.CurrentRow.Cells[0].Value.ToString() + "\t" + dataGridView7.CurrentRow.Cells[1].Value.ToString() + "\t" + dataGridView7.CurrentRow.Cells[2].Value.ToString() + "\t" + Convert.ToDateTime(dataGridView7.CurrentRow.Cells[3].Value).ToShortDateString();
            bool c = false;
            if (listBox2.Items.Count != 0)
            {
                for (int i = 0; i < listBox2.Items.Count; i++)
                {
                    if (listBox2.Items[i].ToString() == a)
                    {
                        c = true;
                        break;
                    }
                }
                if (c)
                {
                    MessageBox.Show("이미 존재하는 항목입니다.");
                }
                else
                {
                    listBox2.Items.Add(a);
                }
            }
            else
            {
                listBox2.Items.Add(a);
            }
        }
        //반납 목록에서 삭제하기
        private void button53_Click(object sender, EventArgs e)
        {
            listBox2.Items.Remove(listBox2.SelectedItem);
        }
        //벌금 탭
        string filterIt1 = "";
        string isre1 = "F_RETURNDATE IS NULL";
        //이름 검색하기
        private void button49_Click(object sender, EventArgs e)
        {
            if (textBox17.Text != "")
            {
                fINEBindingSource.RemoveFilter();
                filterIt1 = "";
                DataRow[] dt = customerTable.Select("C_NAME = '" + textBox17.Text + "'");
                if (dt.Length > 0)
                {
                    foreach (DataRow myrow in dt)
                    {
                        if (filterIt1 != "")
                        {
                            filterIt1 += " or C_NO = " + myrow["C_NO"].ToString();
                        }
                        else
                        {
                            filterIt1 += "C_NO = " + myrow["C_NO"].ToString();
                        }
                    }

                    fINEBindingSource.Filter = isre1 + " and " + filterIt1;
                }
                else
                {
                    MessageBox.Show("등록된 이름이 없습니다.");
                }
            }
        }
        //셀 클릭 시
        private void dataGridView8_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataRow[] dt1 = customerTable.Select("C_NO = " + dataGridView8.CurrentRow.Cells[0].Value.ToString());
            label104.Text = dt1[0]["C_NAME"].ToString();
            label103.Text = dt1[0]["C_Email"].ToString();

            DataRow[] dt2 = goodsTable.Select("G_NO = " + dataGridView8.CurrentRow.Cells[1].Value.ToString() + " and GT_NO = " + dataGridView8.CurrentRow.Cells[2].Value.ToString());
            label110.Text = dt2[0]["G_NAME"].ToString();
            DataRow dt3 = dt2[0].GetParentRow(GTG);
            label108.Text = dt3["GT_NAME"].ToString();
        }
        //반납 완료 된 것 보기
        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked)
            {
                isre1 = "F_RETURNDATE IS NOT NULL";
                if (filterIt1 != "")
                {
                    fINEBindingSource.Filter = isre1 + " and " + filterIt1;
                }
                else
                {
                    fINEBindingSource.Filter = isre1;
                }
            }
            else
            {
                isre1 = "F_RETURNDATE IS NULL";
                if (filterIt1 != "")
                {
                    fINEBindingSource.Filter = isre1 + " and " + filterIt1;
                }
                else
                {
                    fINEBindingSource.Filter = isre1;
                }
            }
        }
        //전체보기
        private void button54_Click(object sender, EventArgs e)
        {
            fINEBindingSource.RemoveFilter();
            fINEBindingSource.Filter = isre1;
            filterIt1 = "";
        }
        //반납 완료
        private void button47_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("반납 하시겠습니까?", "반납", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                oracleConnection1.Open();
                string gtno = dataGridView8.CurrentRow.Cells[2].Value.ToString();
                string gno = dataGridView8.CurrentRow.Cells[1].Value.ToString();
                string cno = dataGridView8.CurrentRow.Cells[0].Value.ToString();
                int a = Convert.ToInt32(dataGridView8.CurrentRow.Cells[3].Value);

                DataRow[] dt4 = goodsTable.Select("G_NO = " + gno + " and GT_NO = " + gtno);
                DataRow c = dt4[0].GetParentRow(GTG);
                int b = Convert.ToInt32(c["GT_RETURNDATE"]);
                string realdate = DateTime.Now.AddDays((a + b) * -1).ToShortDateString();

                //재고 올리기
                reserveTable.DefaultView.Sort = "RS_NO asc";
                DataRow[] dt = reserveTable.Select("G_NO = " + gno + " and GT_NO = " + gtno);
                if (dt.Length == 0)
                {
                    oracleCommand2.CommandText = "UPDATE goods SET g_Stock =  g_STock + 1 WHERE G_NO = " + gno + " and GT_NO = " + gtno;
                    oracleCommand2.ExecuteNonQuery();
                }
                else // 예약 항목에 있으면
                {
                    //메일 보내기
                    DataRow[] dt2 = customerTable.Select("C_NO = " + dt[0]["C_NO"].ToString());
                    string d = dt2[0]["C_Email"].ToString();
                    MailMessage msg = new MailMessage("dofl0119@gmail.com", "dofl011@naver.com",
                    "Subject : 안녕하세요, 언덕위의 책과 비디오 대여점입니다.",
                    "안녕하세요, 고객님. \n 예약하셨던 책이 입고되어 메일 드립니다. 2일 내에 찾아와주시면 감사하겠습니다. \n 이틀이 지나면 자동으로 예약이 취소됩니다.");

                    // SmtpClient 셋업 (Live SMTP 서버, 포트)
                    SmtpClient smtp = new SmtpClient("smtp.live.com", 587);
                    smtp.EnableSsl = true;

                    // Live 또는 Hotmail 계정과 암호 필요
                    smtp.Credentials = new NetworkCredential("dofl0119@gmail.com", "6602gjqm19!");

                    // 발송
                    smtp.Send(msg);

                    oracleCommand4.CommandText = "UPDATE reserve SET RS_ALRAMDATE = to_date('" +
                        DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd') WHERE G_NO = " + dt[0]["G_NO"] + " and GT_NO = " + dt[0]["GT_NO"] +
                        " and C_NO = " + dt[0]["C_NO"];
                    oracleCommand4.ExecuteNonQuery();
                    MessageBox.Show("예약되어있던 항목이 있어 이메일을 보냈습니다.");
                }
                oracleCommand1.CommandText = "UPDATE fine SET F_RETURNDATE = to_date('" +
                                    DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd') WHERE G_NO = " + dataGridView8.CurrentRow.Cells[1].Value.ToString() +
                                    " and GT_NO = " + dataGridView8.CurrentRow.Cells[2].Value.ToString() +
                                    " and C_NO = " + dataGridView8.CurrentRow.Cells[0].Value.ToString();
                oracleCommand1.ExecuteNonQuery();

                oracleCommand3.CommandText = "UPDATE rent SET R_ISRETURN = 1 WHERE G_NO = " +
                    dataGridView8.CurrentRow.Cells[1].Value.ToString() +
                                    " and GT_NO = " + dataGridView8.CurrentRow.Cells[2].Value.ToString() +
                                    " and C_NO = " + dataGridView8.CurrentRow.Cells[0].Value.ToString() +
                                    " and R_DATE = to_date('" + realdate + "', 'yyyy-mm-dd')";
                oracleCommand3.ExecuteNonQuery();


                oracleConnection1.Close();

                gOODSTableAdapter.Fill(dataSet11.GOODS);
                goodsTable = dataSet11.Tables["GOODS"];

                fineTableAdapter1.Fill(dataSet11.FINE);
                fineTable = dataSet11.Tables["FINE"];

                rentTableAdapter1.Fill(dataSet11.RENT);
                rentTable = dataSet11.Tables["RENT"];

                fINEBindingSource.RemoveFilter();
                fINEBindingSource.Filter = isre1;
            }
            else { }
        }
        //회원 조회 버튼 클릭 시
        private void button24_Click(object sender, EventArgs e)
        {
            panel11.Visible = true;
            textBox18.Text = "이유 상세보기를 원하시면 항목을 선택해주세요.";
            button100.Visible = false;
            button110.Visible = false;
        }

        /************************회원 조회 화면*****************************/
        //back 버튼
        private void button55_Click(object sender, EventArgs e)
        {
            panel11.Visible = false;
            if (identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            cUSTOMERBindingSource1.RemoveFilter();
            bLACKLISTBindingSource.RemoveFilter();
            wHITELISTBindingSource.RemoveFilter();
            button22.Visible = true;
        }
        //블랙리스트 셀 클릭 시
        private void dataGridView10_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            textBox18.Text = dataGridView10.CurrentRow.Cells[4].Value.ToString();
        }
        //블랙 리스트 탭이면 초기화
        private void tabControl3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl3.SelectedIndex == 1)
            {
                textBox18.Text = "이유 상세보기를 원하시면 항목을 선택해주세요.";
            }
        }
        //일반 회원 검색
        private void button59_Click(object sender, EventArgs e)
        {
            if (textBox20.Text != "")
                cUSTOMERBindingSource1.Filter = "C_name like '%" + textBox20.Text + "%'";
        }
        //일반 회원 전체 보기
        private void button62_Click(object sender, EventArgs e)
        {
            cUSTOMERBindingSource1.RemoveFilter();
        }
        //블랙 리스트 검색
        private void button60_Click(object sender, EventArgs e)
        {
            if (textBox21.Text != "")
                bLACKLISTBindingSource.Filter = "BL_C_name like '%" + textBox21.Text + "%'";
        }
        //블랙 리스트 전체 보기
        private void button63_Click(object sender, EventArgs e)
        {
            bLACKLISTBindingSource.RemoveFilter();
        }
        //화이트 리스트 검색
        private void button61_Click(object sender, EventArgs e)
        {
            if (textBox22.Text != "")
                wHITELISTBindingSource.Filter = "WL_C_name like '%" + textBox22.Text + "%'";
        }
        //화이트 리스트 전체보기
        private void button64_Click(object sender, EventArgs e)
        {
            wHITELISTBindingSource.RemoveFilter();
        }

        //회원 관리 버튼 클릭 시
        private void button25_Click(object sender, EventArgs e)
        {
            panel12.Visible = true;
            button100.Visible = false;
            button110.Visible = false;
        }
        
        /************************회원 관리 화면*****************************/
        //back 버튼
        private void button65_Click(object sender, EventArgs e)
        {
            panel12.Visible = false;
            button22.Visible = true;
            if (identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            cUSTOMERBindingSource1.RemoveFilter();
            bLACKLISTBindingSource1.RemoveFilter();
            wHITELISTBindingSource1.RemoveFilter();
        }
        // 탭 접근 권한 막기
        private void tabControl4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((accessTab == true) && (tabControl4.SelectedTab == tabPage10))
            {
                tabControl4.SelectedTab = tabPage10;
            }
            else if ((accessTab == true) && (tabControl4.SelectedTab == tabPage11))
            {
                tabControl4.SelectedTab = tabPage11;
            }
            else if ((accessTab == false) && (tabControl4.SelectedTab == tabPage10))
            {
                MessageBox.Show("접근 권한이 없습니다.");
                tabControl4.SelectedTab = tabPage9;
            }
            else if ((accessTab == false) && (tabControl4.SelectedTab == tabPage11))
            {
                MessageBox.Show("접근 권한이 없습니다.");
                tabControl4.SelectedTab = tabPage9;
            }
        }
        //이름 검색
        private void button56_Click(object sender, EventArgs e)
        {
            if (textBox19.Text != "")
                cUSTOMERBindingSource1.Filter = "C_name like '%" + textBox19.Text + "%'";
        }
        //전체 보기버튼
        private void button71_Click(object sender, EventArgs e)
        {
            cUSTOMERBindingSource1.RemoveFilter();
        }
        //회원 정보 수정
        private void button57_Click(object sender, EventArgs e)
        {
            cUSTOMERBindingSource1.EndEdit();
            int a = customerTableAdapter1.Update(dataSet11.CUSTOMER);
            if (a > 0)
            {
                MessageBox.Show("회원 정보 수정이 완료되었습니다.");
            }
        }
        //회원 가입
        private void button58_Click(object sender, EventArgs e)
        {
            panel13.Visible = true;
        }
        //취소 버튼
        private void button72_Click(object sender, EventArgs e)
        {
            panel13.Visible = false;
            textBox25.Text = "";
            textBox26.Text = "";
            textBox27.Text = "";
            textBox28.Text = "";
        }
        //중복 확인
        bool same = true;
        private void button74_Click(object sender, EventArgs e)
        {
            DataRow[] dt = customerTable.Select("C_EMAIL = '" + textBox28.Text + "'");
            if (dt.Length > 0)
            {
                MessageBox.Show("존재하는 이메일 입니다. 다시 입력해주세요.");
                same = true;
            }
            else
            {
                MessageBox.Show("사용하셔도 되는 이메일 입니다.");
                same = false;
            }
        }
        // 신규 회원 등록 버튼
        private void button73_Click(object sender, EventArgs e)
        {
            if (same)
            {
                MessageBox.Show("중복 검사를 해주세요.");
            }
            else
            {
                oracleConnection1.Open();
                oracleCommand4.CommandText = "Insert into customer values(c_seq.nextval, '" + textBox25.Text +
                    "', '" + textBox26.Text + "', '" + textBox27.Text + "', '" + textBox28.Text + "', 300)";
                oracleCommand4.ExecuteNonQuery();
                MessageBox.Show("회원가입이 완료되었습니다.");

                customerTableAdapter1.Fill(dataSet11.CUSTOMER);
                customerTable = dataSet11.Tables["CUSTOMER"];

                panel13.Visible = false;
                textBox25.Text = "";
                textBox26.Text = "";
                textBox27.Text = "";
                textBox28.Text = "";
                same = true;
                oracleConnection1.Close();
            }
        }
        //블랙리스트 이름 검색
        private void button75_Click(object sender, EventArgs e)
        {
            if (textBox29.Text != "")
                bLACKLISTBindingSource1.Filter = "C_name like '%" + textBox29.Text + "%'";
        }
        //전체보기
        private void button76_Click(object sender, EventArgs e)
        {
            bLACKLISTBindingSource1.RemoveFilter();
        }
        // 블랙리스트 회원 이름 검색
        private void button66_Click(object sender, EventArgs e)
        {
            if (textBox24.Text != "")
                cUSTOMERBindingSource.Filter = "C_name like '%" + textBox24.Text + "%'";
        }
        // 수정하기 버튼
        private void button67_Click(object sender, EventArgs e)
        {
            bLACKLISTBindingSource1.EndEdit();
            int a = blacK_LISTTableAdapter1.Update(dataSet11.BLACK_LIST);
            if (a > 0)
            {
                MessageBox.Show("수정이 완료되었습니다.");
            }
        }
        bool lockthis = false;
        //블랙리스트 항목 클릭 시
        private void dataGridView13_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!lockthis)
            {
                textBox23.Text = dataGridView13.CurrentRow.Cells[4].Value.ToString();
            }
        }
        //새로 등록하기
        private void button69_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("등록 하시겠습니까?", "블랙리스트 등록", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                lockthis = true;
                button77.Visible = true;
                bLACKLISTBindingSource1.AddNew();
                oracleConnection1.Open();
                oracleCommand3.CommandText = "SELECT bl_seq.nextval FROM DUAL";
                dataGridView13.CurrentRow.Cells[0].Value = "연체";
                dataGridView13.CurrentRow.Cells[1].Value = oracleCommand3.ExecuteScalar().ToString();
                dataGridView13.CurrentRow.Cells[2].Value = dataGridView14.CurrentRow.Cells[0].Value;
                dataGridView13.CurrentRow.Cells[3].Value = dataGridView14.CurrentRow.Cells[1].Value;
                dataGridView13.CurrentRow.Cells[5].Value = DateTime.Now.ToShortDateString();
                oracleConnection1.Close();
            }
            else { }
        }
        //삭제 하기
        private void button68_Click(object sender, EventArgs e)
        {
            if (!lockthis)
            {
                if (MessageBox.Show("삭제 하시겠습니까?", "블랙리스트 삭제", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    bLACKLISTBindingSource1.RemoveCurrent();
                    bLACKLISTBindingSource1.EndEdit();
                    blacK_LISTTableAdapter1.Update(dataSet11.BLACK_LIST);
                }
                else { }
            }
            else
            {
                MessageBox.Show("새로 등록하고 있어서 삭제가 불가합니다.");
            }
        }
        //선택 하기
        private void button77_Click(object sender, EventArgs e)
        {
            dataGridView13.CurrentRow.Cells[2].Value = dataGridView14.CurrentRow.Cells[0].Value;
            dataGridView13.CurrentRow.Cells[3].Value = dataGridView14.CurrentRow.Cells[1].Value;
        }
        //등록 완료
        private void button70_Click(object sender, EventArgs e)
        {
            dataGridView13.CurrentRow.Cells[4].Value = textBox23.Text;
            bLACKLISTBindingSource1.EndEdit();
            int a = blacK_LISTTableAdapter1.Update(dataSet11.BLACK_LIST);
            if (a > 0)
            {
                MessageBox.Show("등록이 완료되었습니다.");
            }
            lockthis = false;
        }
        //화이트 리스트
        //이름 검색
        private void button79_Click(object sender, EventArgs e)
        {
            if (textBox30.Text != "")
            {
                wHITELISTBindingSource1.Filter = "C_name like '%" + textBox30.Text + "%'";
            }
        }
        //전체 보기
        private void button80_Click(object sender, EventArgs e)
        {
            wHITELISTBindingSource1.RemoveFilter();
        }
        //삭제하기
        private void button78_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("삭제 하시겠습니까?", "화이트리스트 삭제", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                wHITELISTBindingSource1.RemoveCurrent();
                wHITELISTBindingSource1.EndEdit();
                whitE_LISTTableAdapter1.Update(dataSet11.WHITE_LIST);
            }
            else { }
        }


        //물품 조회 버튼
        private void button27_Click(object sender, EventArgs e)
        {
            panel14.Visible = true;
            button100.Visible = false;
            button110.Visible = false;
            comboBox6.Items.Clear();
            comboBox8.Items.Clear();
            int a = 0;
            goodsTypeTable.DefaultView.Sort = "GT_NO asc";
            foreach (DataRow myrow in goodsTypeTable.Rows)
            {
                if (a != Convert.ToInt32(myrow["GT_NO"]) / 10)
                {
                    string c = myrow["GT_NAME"].ToString();
                    string[] b = c.Split(' ');
                    comboBox6.Items.Add(b[1]);
                    comboBox8.Items.Add(b[1]);
                    a = Convert.ToInt32(myrow["GT_NO"]) / 10;
                }
            }
            rENTBindingSource.Filter = "G_no = 0";
        }
        //물품 관리 버튼
        private void button28_Click(object sender, EventArgs e)
        {
            button100.Visible = false;
            button110.Visible = false;
            if (Convert.ToInt32(identityRow["S_RANK"]) > 1)
            {
                panel15.Visible = true;
            }
            else
            {
                MessageBox.Show("접근 권한이 없습니다.");
            }
        }

        /************************물품 조회 화면*****************************/
        //back 버튼
        private void button81_Click(object sender, EventArgs e)
        {
            if(identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            panel14.Visible = false;
            gOODSBindingSource.RemoveFilter();
            gOODSTYPEBindingSource1.RemoveFilter();
            rENTBindingSource.RemoveFilter();
            gOODSBindingSource1.RemoveFilter();
            gOODSBindingSource2.RemoveFilter();
        }
        //물품
        //물품 셀 클릭 시
        private void dataGridView16_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            rENTBindingSource.Filter = "G_no = " + dataGridView16.CurrentRow.Cells[0].Value.ToString() +
                " and GT_NO = " + dataGridView16.CurrentRow.Cells[1].Value.ToString();
        }
        //검색 버튼
        private void button82_Click(object sender, EventArgs e)
        {
            if (textBox31.Text != "")
            {
                gOODSBindingSource.Filter = "G_name like '%" + textBox31.Text + "%'";
            }
        }
        //전체보기
        private void button83_Click(object sender, EventArgs e)
        {
            gOODSBindingSource.RemoveFilter();
        }
        //종류 콤보박스 선택 시
        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 장르 아이템 추가
            comboBox7.Items.Clear();
            int a = comboBox6.SelectedIndex + 2;
            goodsTypeTable.DefaultView.Sort = "GT_NO asc";
            DataRow[] dt = goodsTypeTable.Select("gt_no/10 <= " + a + "and gt_no/10 >= " + (a - 1));
            foreach (DataRow myrow in dt)
            {
                string c = myrow["GT_NAME"].ToString();
                string[] b = c.Split(' ');
                comboBox7.Items.Add(b[0]);
            }
            // 필터링
            gOODSBindingSource.Filter = "gt_no < " + (a * 10) + " and gt_no > " + ((a - 1) * 10);
        }
        //물품 장르 콤보 박스 체인지
        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            int a = comboBox6.SelectedIndex + 1;
            int b = comboBox7.SelectedIndex + 1;
            gOODSBindingSource.Filter = "GT_NO = " + ((a * 10) + b);
        }
        //물품 종류
        //물품 종류 콤보박스 체인지
        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            int a = comboBox8.SelectedIndex + 2;
            gOODSTYPEBindingSource1.Filter = "gt_no < " + (a * 10) + " and gt_no > " + ((a - 1) * 10);
        }
        //전체보기
        private void button84_Click(object sender, EventArgs e)
        {
            gOODSTYPEBindingSource1.RemoveFilter();
        }
        //이름 검색하기
        private void button85_Click(object sender, EventArgs e)
        {
            if (textBox32.Text != "")
            {
                gOODSTYPEBindingSource1.Filter = "GT_name like '%" + textBox32.Text + "%'";
            }
        }
        /************************물품  관리 화면*****************************/
        //back 버튼
        private void button86_Click(object sender, EventArgs e)
        {
            if (identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            panel15.Visible = false;
            gOODSBindingSource1.RemoveFilter();
        }
        //이름 검색
        private void button88_Click(object sender, EventArgs e)
        {
            if (textBox33.Text != "")
            {
                gOODSBindingSource1.Filter = "G_name like '%" + textBox33.Text + "%'";
            }
        }
        //전체보기
        private void button89_Click(object sender, EventArgs e)
        {
            gOODSBindingSource1.RemoveFilter();
        }
        //수정완료
        private void button87_Click(object sender, EventArgs e)
        {
            int n = 0;
            int h = 0;
            if (checkBox7.Checked)
            {
                n = 1;
            }
            if (checkBox8.Checked)
            {
                h = 1;
            }

            oracleConnection1.Open();
            oracleCommand5.CommandText = "UPDATE GOODS SET G_NO = " + textBox34.Text +
                ", GT_NO = " + textBox35.Text + ", G_NAME = '" + textBox36.Text + "', G_Price = " +
                textBox37.Text + ", G_Length = " + textBox38.Text + ", G_BOC = '" + textBox39.Text +
                "', G_AgeLimit = " + textBox40.Text + ", G_Stock = " + textBox41.Text + ", G_StoreDate = to_date('" +
                textBox42.Text + "', 'yyyy-mm-dd'), G_isSoldOut = " + n + ", G_isSale = " + h + ", G_Rank = " +
                textBox43.Text + ", G_WND = '" + textBox44.Text + "', G_PNP = '" + textBox45.Text +
                "', G_CreateDate = to_date('" + textBox46.Text + "', 'yyyy-mm-dd'), G_Nation = '" + textBox47.Text +
                "' WHERE G_NO =" + dataGridView19.CurrentRow.Cells[0].Value.ToString() + " and GT_NO = " +
                dataGridView19.CurrentRow.Cells[1].Value.ToString();
            oracleCommand5.ExecuteNonQuery();
            oracleConnection1.Close();
            MessageBox.Show("수정이 완료되었습니다.");

            gOODSTableAdapter.Fill(dataSet11.GOODS);
            goodsTable = dataSet11.Tables["GOODS"];
        }
        //입고하기
        private void button90_Click(object sender, EventArgs e)
        {
            textBox34.Text = "";
            textBox35.Text = "";
            textBox36.Text = "";
            textBox37.Text = "";
            textBox38.Text = "";
            textBox39.Text = "";
            textBox40.Text = "";
            textBox41.Text = "";
            textBox42.Text = DateTime.Now.ToShortDateString();
            textBox43.Text = "";
            textBox44.Text = "";
            textBox45.Text = "";
            textBox46.Text = "";
            textBox47.Text = "";
            checkBox7.Checked = false;
            checkBox8.Checked = false;
        }
        //입고 완료 버튼
        private void button91_Click(object sender, EventArgs e)
        {
            int n = 0;
            int h = 0;
            if (checkBox7.Checked)
            {
                n = 1;
            }
            if (checkBox8.Checked)
            {
                h = 1;
            }

            oracleConnection1.Open();
            oracleCommand5.CommandText = "INSERT INTO GOODS VALUES( " + textBox34.Text +
                ", " + textBox35.Text + ", '" + textBox36.Text + "', " +
                textBox37.Text + ", " + textBox38.Text + ", '" + textBox39.Text +
                "', " + textBox40.Text + ", " + textBox41.Text + ", to_date('" +
                textBox42.Text + "', 'yyyy-mm-dd'), " + n + ", " + h + ", " +
                textBox43.Text + ", '" + textBox44.Text + "', '" + textBox45.Text +
                "', to_date('" + textBox46.Text + "', 'yyyy-mm-dd'), '" + textBox47.Text + "')";
            oracleCommand5.ExecuteNonQuery();
            oracleConnection1.Close();
            MessageBox.Show("등록이 완료되었습니다.");

            gOODSTableAdapter.Fill(dataSet11.GOODS);
            goodsTable = dataSet11.Tables["GOODS"];
        }
        //셀 클릭 시
        private void dataGridView19_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            textBox34.Text = dataGridView19.CurrentRow.Cells[0].Value.ToString();
            textBox35.Text = dataGridView19.CurrentRow.Cells[1].Value.ToString();
            textBox36.Text = dataGridView19.CurrentRow.Cells[2].Value.ToString();
            textBox37.Text = dataGridView19.CurrentRow.Cells[3].Value.ToString();
            textBox38.Text = dataGridView19.CurrentRow.Cells[4].Value.ToString();
            textBox39.Text = dataGridView19.CurrentRow.Cells[5].Value.ToString();
            textBox40.Text = dataGridView19.CurrentRow.Cells[6].Value.ToString();
            textBox41.Text = dataGridView19.CurrentRow.Cells[7].Value.ToString();
            textBox42.Text = Convert.ToDateTime(dataGridView19.CurrentRow.Cells[8].Value).ToShortDateString();

            if (dataGridView19.CurrentRow.Cells[9].Value.ToString() == "0")
            {
                checkBox7.Checked = false;
            }
            else
            {
                checkBox7.Checked = true;
            }
            if (dataGridView19.CurrentRow.Cells[10].Value.ToString() == "0")
            {
                checkBox8.Checked = false;
            }
            else
            {
                checkBox8.Checked = true;
            }
            textBox43.Text = dataGridView19.CurrentRow.Cells[11].Value.ToString();
            textBox44.Text = dataGridView19.CurrentRow.Cells[12].Value.ToString();
            textBox45.Text = dataGridView19.CurrentRow.Cells[13].Value.ToString();
            textBox46.Text = Convert.ToDateTime(dataGridView19.CurrentRow.Cells[14].Value).ToShortDateString();
            textBox47.Text = dataGridView19.CurrentRow.Cells[15].Value.ToString();
        }
        //종류 수정/ 등록
        //새 항목
        private void button93_Click(object sender, EventArgs e)
        {
            gOODSTYPEBindingSource.AddNew();
        }
        //동기화
        private void button92_Click(object sender, EventArgs e)
        {
            gOODSTYPEBindingSource.EndEdit();
            int a = gOODSTableAdapter.Update(dataSet11.GOODS);
            if (a > 0)
            {
                MessageBox.Show("동기화가 완료되었습니다.");
            }
        }
        //삭제
        private void button94_Click(object sender, EventArgs e)
        {
            DataRow[] dt = goodsTable.Select("GT_NO = " + dataGridView20.CurrentRow.Cells[0].Value.ToString());
            if (dt.Length > 0)
            {
                MessageBox.Show("등록된 물품들을 삭제 후에 삭제해주세요.");
            }
            else
            {
                gOODSTYPEBindingSource.RemoveCurrent();
            }
        }
        //폐기
        //물품 이름 검색
        private void button96_Click(object sender, EventArgs e)
        {
            if (textBox48.Text != "")
            {
                gOODSBindingSource2.Filter = "G_name like '%" + textBox48.Text + "%'";
            }
        }
        //전체보기
        private void button95_Click(object sender, EventArgs e)
        {
            gOODSBindingSource2.RemoveFilter();
        }
        //항목 추가 버튼
        private void button97_Click(object sender, EventArgs e)
        {
            string gno = dataGridView21.CurrentRow.Cells[0].Value.ToString();
            string gtno = dataGridView21.CurrentRow.Cells[1].Value.ToString();
            string gname = dataGridView21.CurrentRow.Cells[2].Value.ToString();
            DataRow[] dt = rentTable.Select("G_no = " + gno + " and GT_NO = " + gtno + " and R_IsReturn = 0");
            if (dt.Length > 0)
            {
                MessageBox.Show("현재 항목은 고객이 대여중이기에 폐기할 수 없습니다.");
            }
            else
            {
                bool c = false;
                if (listBox3.Items.Count != 0)
                {
                    for (int i = 0; i < listBox3.Items.Count; i++)
                    {
                        if (listBox3.Items[i].ToString() == gno + "\t" + gtno + "\t" + gname)
                        {
                            c = true;
                            break;
                        }
                    }
                    if (c)
                    {
                        MessageBox.Show("이미 존재하는 항목입니다.");
                    }
                    else
                    {
                        listBox3.Items.Add(gno + "\t" + gtno + "\t" + gname);
                    }
                }
                else
                {
                    listBox3.Items.Add(gno + "\t" + gtno + "\t" + gname);
                }
            }
        }
        //항목 빼기
        private void button98_Click(object sender, EventArgs e)
        {
            listBox3.Items.Remove(listBox3.SelectedItem);
        }
        // 삭제하기
        private void button99_Click(object sender, EventArgs e)
        {
            if (listBox3.Items.Count != 0)
            {
                for (int i = 0; i < listBox3.Items.Count; i++)
                {
                    string j = listBox3.Items[i].ToString();
                    string[] k = j.Split('\t');
                    string gno = k[0];
                    string gtno = k[1];

                    if (MessageBox.Show("폐기 하시겠습니까?\n\n**시간이 조금 소요됩니다.", "물품 폐기", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        oracleConnection1.Open();
                        oracleCommand1.CommandText = "DELETE FROM REVIEW WHERE G_NO = " + gno +
                            " and GT_no = " + gtno;
                        oracleCommand2.CommandText = "DELETE FROM managerent WHERE G_NO = " + gno +
                            " and GT_no = " + gtno;
                        oracleCommand3.CommandText = "DELETE FROM goods_Stats WHERE G_NO = " + gno +
                            " and GT_no = " + gtno;

                        //예약 항목에 있으면 물품이 폐기되어 예약이 취소되었다는 메일 발송
                        DataRow[] dt = reserveTable.Select("G_NO = " + gno + " and GT_no = " + gtno);
                        //메일 보내기
                        foreach (DataRow myrow in dt)
                        {
                            DataRow[] dt2 = customerTable.Select("C_NO = " + myrow["C_NO"].ToString());
                            string d = dt2[0]["C_Email"].ToString();
                            MailMessage msg = new MailMessage("dofl0119@gmail.com", "dofl011@naver.com",
                            "Subject : 안녕하세요, 언덕위의 책과 비디오 대여점입니다.",
                            "안녕하세요, 고객님. \n 예약하셨던 물품이 폐기 처리되어 메일 드립니다.\n" +
                            "물품 폐기로 인해 예약이 취소되었습니다. \n 죄송합니다.");

                            // SmtpClient 셋업 (Live SMTP 서버, 포트)
                            SmtpClient smtp = new SmtpClient("smtp.live.com", 587);
                            smtp.EnableSsl = true;

                            // Live 또는 Hotmail 계정과 암호 필요
                            smtp.Credentials = new NetworkCredential("dofl0119@gmail.com", "6602gjqm19!");

                            // 발송
                            smtp.Send(msg);
                        }
                        oracleCommand4.CommandText = "DELETE FROM reserve WHERE G_NO = " + gno +
                            " and GT_no = " + gtno;
                        //렌트->렌트2
                        DataRow[] dt4 = rentTable.Select("G_NO = " + gno + " and GT_no = " + gtno);
                        foreach (DataRow myrow in dt4)
                        {
                            oracleCommand5.CommandText = "Insert into rent2 values(" + myrow["C_no"].ToString() +
                                ", " + myrow["G_NO"].ToString() + ", " + myrow["GT_NO"].ToString() + ", to_date('" +
                                Convert.ToDateTime(myrow["R_Date"]).ToShortDateString() + "', 'yyyy-mm-dd'), " + myrow["R_Money"].ToString() + ")";
                        }
                        oracleCommand6.CommandText = "DELETE FROM rent WHERE G_NO = " + gno +
                            " and GT_no = " + gtno;
                        //파인->파인2
                        DataRow[] dt5 = fineTable.Select("G_NO = " + gno + " and GT_no = " + gtno);
                        foreach (DataRow myrow in dt5)
                        {
                            oracleCommand7.CommandText = "Insert into fine2 values(" + myrow["C_no"].ToString() +
                                ", " + myrow["G_NO"].ToString() + ", " + myrow["GT_NO"].ToString() + ", " + myrow["F_FINE"].ToString() +
                                ", to_date('" + Convert.ToDateTime(myrow["F_ReturnDate"]).ToShortDateString() + "', 'yyyy-mm-dd'))";
                        }
                        oracleCommand8.CommandText = "DELETE FROM fine WHERE G_NO = " + gno +
                            " and GT_no = " + gtno;
                        oracleCommand1.ExecuteNonQuery();
                        oracleCommand2.ExecuteNonQuery();
                        oracleCommand3.ExecuteNonQuery();
                        oracleCommand4.ExecuteNonQuery();
                        oracleCommand7.ExecuteNonQuery();
                        oracleCommand8.ExecuteNonQuery();
                        oracleCommand5.ExecuteNonQuery();
                        oracleCommand6.ExecuteNonQuery();

                        oracleConnection1.Close();

                        MessageBox.Show("삭제가 완료 되었습니다.\n해당 물품 관련 항목은 모두 삭제 되었으며, 필요 항목 일부만 남겨두었습니다.");
                        rentTableAdapter1.Fill(dataSet11.RENT);
                        rentTable = dataSet11.Tables["RENT"];

                        rEVIEWTableAdapter.Fill(dataSet11.REVIEW);
                        reviewTable = dataSet11.Tables["REVIEW"];

                        fineTableAdapter1.Fill(dataSet11.FINE);
                        fineTable = dataSet11.Tables["FINE"];

                        gOODSTableAdapter.Fill(dataSet11.GOODS);
                        goodsTable = dataSet11.Tables["GOODS"];
                    }
                }
            }
            else { }
        }

        //사장 스페셜
        //직원 관리 버튼 클릭 시
        private void button100_Click(object sender, EventArgs e)
        {
            panel16.Visible = true;
            button100.Visible = false;
            button100.Visible = false;
            button110.Visible = false;
        }
        /************************직원  관리 화면*****************************/
        //back 버튼
        private void button101_Click(object sender, EventArgs e)
        {
            if (identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            panel16.Visible = false;
            sTAFFBindingSource.RemoveFilter();
            button100.Visible = true;
        }
        //추가
        private void button103_Click(object sender, EventArgs e)
        {
            sTAFFBindingSource.AddNew();
        }
        //동기화
        private void button102_Click(object sender, EventArgs e)
        {
            sTAFFBindingSource.EndEdit();
            int a = staffTableAdapter1.Update(dataSet11.STAFF);
            if (a > 0)
            {
                MessageBox.Show("동기화가 완료되었습니다.");
            }

        }
        //삭제 버튼
        private void button104_Click(object sender, EventArgs e)
        {
            sTAFFBindingSource.RemoveCurrent();
        }

        //물품 통계 버튼
        private void button29_Click(object sender, EventArgs e)
        {
            if (Convert.ToInt32(identityRow["S_RANK"]) > 1)
            {
                panel17.Visible = true;
            }
            else
            {
                MessageBox.Show("접근 권한이 없습니다.");
            }

            listView1.Columns.Add("고객번호", 70);
            listView1.Columns.Add("물품번호", 70);
            listView1.Columns.Add("종류번호", 70);
            listView1.Columns.Add("날짜", 80);
            listView1.Columns.Add("금액", 80);
            /* * 다섯가지 모양을 가질 수 있다. * 큰아이콘, 작은아이콘, 리스트, 상세히, 타일모양 등 */
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.GridLines = true;

            listView2.Columns.Add("고객 번호", 70);
            listView2.Columns.Add("물품 번호", 70);
            listView2.Columns.Add("종류 번호", 70);
            listView2.Columns.Add("날짜", 80);
            listView2.Columns.Add("금액", 80);
            /* * 다섯가지 모양을 가질 수 있다. * 큰아이콘, 작은아이콘, 리스트, 상세히, 타일모양 등 */
            listView2.View = View.Details;
            listView2.FullRowSelect = true;
            listView2.GridLines = true;
            /*
            listView3.Columns.Add("물품 번호", 70);
            listView3.Columns.Add("종류 번호", 70);
            listView3.Columns.Add("이름", 100);
            listView3.Columns.Add("대여량", 60);
            listView3.Columns.Add("연체량", 60);*/
            /* * 다섯가지 모양을 가질 수 있다. * 큰아이콘, 작은아이콘, 리스트, 상세히, 타일모양 등 */
            listView3.View = View.Details;
            listView3.FullRowSelect = true;
            listView3.GridLines = true;

            dateTimePicker1.Value = new DateTime(int.Parse(DateTime.Now.ToString("yyyy")),
                                     int.Parse(DateTime.Now.ToString("MM")),
                                     int.Parse(DateTime.Now.ToString("dd")));

            dateTimePicker2.Value = new DateTime(int.Parse(DateTime.Now.ToString("yyyy")),
                                     int.Parse(DateTime.Now.ToString("MM")),
                                     int.Parse(DateTime.Now.ToString("dd")));

            comboBox10.Items.Clear();

            int a = 0;
            goodsTypeTable.DefaultView.Sort = "GT_NO asc";
            comboBox10.Items.Add("전체 보기");
            foreach (DataRow myrow in goodsTypeTable.Rows)
            {
                if (a != Convert.ToInt32(myrow["GT_NO"]) / 10)
                {
                    string c = myrow["GT_NAME"].ToString();
                    string[] b = c.Split(' ');
                    comboBox10.Items.Add(b[1]);
                    a = Convert.ToInt32(myrow["GT_NO"]) / 10;
                }
            }
            //분류 기본 값 0으로 주기
            comboBox10.SelectedIndex = 0;
            comboBox9.SelectedIndex = 0;


            listView3.Items.Clear();
            oracleConnection1.Open();
            //매출량
            oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT GROUP BY G_NO, GT_NO";
            oracleCommand1.ExecuteNonQuery();

            oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
            OracleDataReader odr3 = oracleCommand5.ExecuteReader();
            while (odr3.Read())
            {
                String[] arr = new String[5];
                arr[0] = odr3["G_NO"].ToString();
                arr[1] = odr3["GT_NO"].ToString();
                DataRow[] dt = goodsTable.Select("g_no = " + odr3["G_NO"].ToString() + " and gt_no = " +
                    odr3["GT_NO"].ToString());
                arr[2] = dt[0]["G_NAME"].ToString();
                arr[3] = odr3["ONE"].ToString();
                arr[4] = "0";

                ListViewItem lvt = new ListViewItem(arr);
                listView3.Items.Add(lvt);
            }
            odr3.Close();

            oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
            oracleCommand9.ExecuteNonQuery();

            //매출량2
            oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT2 GROUP BY G_NO, GT_NO";
            oracleCommand1.ExecuteNonQuery();

            oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
            OracleDataReader odr6 = oracleCommand5.ExecuteReader();
            while (odr6.Read())
            {
                String[] arr = new String[5];
                arr[0] = odr6["G_NO"].ToString();
                arr[1] = odr6["GT_NO"].ToString();
                DataRow[] dt = goodsTable.Select("g_no = " + odr6["G_NO"].ToString() + " and gt_no = " +
                    odr6["GT_NO"].ToString());
                arr[2] = dt[0]["G_NAME"].ToString();
                arr[3] = odr6["ONE"].ToString();
                arr[4] = "0";

                ListViewItem lvt = new ListViewItem(arr);
                listView3.Items.Add(lvt);
            }
            odr6.Close();

            oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
            oracleCommand9.ExecuteNonQuery();

            //연체량
            oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
               "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE GROUP BY G_NO, GT_NO";
            oracleCommand3.ExecuteNonQuery();

            oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
            OracleDataReader odr2 = oracleCommand6.ExecuteReader();
            bool h = false;
            while (odr2.Read())
            {
                h = false;
                if (listView3.Items.Count != 0)
                {
                    for (int i = 0; i < listView3.Items.Count; i++)
                    {
                        if (listView3.Items[i].SubItems[0].Text == odr2["G_NO"].ToString() &&
                            listView3.Items[i].SubItems[1].Text == odr2["GT_NO"].ToString())
                        {
                            listView3.Items[i].SubItems[4].Text = odr2["ONE"].ToString();
                            h = true;
                        }
                    }
                    if (h == false)
                    {
                        String[] arr = new String[5];
                        arr[0] = odr2["G_NO"].ToString();
                        arr[1] = odr2["GT_NO"].ToString();
                        DataRow[] dt = goodsTable.Select("g_no = " + odr2["G_NO"].ToString() + " and gt_no = " +
                odr2["GT_NO"].ToString());
                        arr[2] = dt[0]["G_NAME"].ToString();
                        arr[3] = "0";
                        arr[4] = odr2["ONE"].ToString();

                        ListViewItem lvt = new ListViewItem(arr);
                        listView3.Items.Add(lvt);
                    }
                }
            }
            odr2.Close();
            //연체량2
            oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
            oracleCommand8.ExecuteNonQuery();
            oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
                       "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE2 GROUP BY G_NO, GT_NO";
            oracleCommand3.ExecuteNonQuery();

            oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
            OracleDataReader odr7 = oracleCommand6.ExecuteReader();
            bool g = false;
            while (odr7.Read())
            {
                g = false;
                if (listView3.Items.Count != 0)
                {
                    for (int i = 0; i < listView3.Items.Count; i++)
                    {
                        if (listView3.Items[i].SubItems[0].Text == odr7["G_NO"].ToString() &&
                            listView3.Items[i].SubItems[1].Text == odr7["GT_NO"].ToString())
                        {
                            listView3.Items[i].SubItems[4].Text = odr7["ONE"].ToString();
                            g = true;
                        }
                    }
                    if (g == false)
                    {
                        String[] arr = new String[5];
                        arr[0] = odr7["G_NO"].ToString();
                        arr[1] = odr7["GT_NO"].ToString();
                        DataRow[] dt = goodsTable.Select("g_no = " + odr7["G_NO"].ToString() + " and gt_no = " +
                odr7["GT_NO"].ToString());
                        arr[2] = dt[0]["G_NAME"].ToString();
                        arr[3] = "0";
                        arr[4] = odr7["ONE"].ToString();

                        ListViewItem lvt = new ListViewItem(arr);
                        listView3.Items.Add(lvt);
                    }
                }
            }
            odr7.Close(); 

            oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
            oracleCommand8.ExecuteNonQuery();
            oracleConnection1.Close();
            comboBox11.SelectedIndex = 0;
            comboBox12.SelectedIndex = 0;
            button100.Visible = false;
            button110.Visible = false;

        }
        /************************물품 통계 화면*****************************/

        //back버튼
        private void button106_Click(object sender, EventArgs e)
        {
            if (identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            panel17.Visible = false;
        }
        //매출 통계
        //매출 확인 버튼
        private void button105_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            listView2.Items.Clear();

            chart1.Series.Remove(chart1.Series[0]);
            chart1.Series.Add("a");
            chart1.Titles.Clear();
            
            chart2.Series.Remove(chart2.Series[0]);
            chart2.Series.Add("a");
            chart2.Titles.Clear();

            chart3.Series.Remove(chart3.Series[0]);
            chart3.Series.Add("a");
            chart3.Titles.Clear();


            if (comboBox11.SelectedIndex == 0)
            {
                chart1.Series[0].ChartType = SeriesChartType.Column;
                chart2.Series[0].ChartType = SeriesChartType.Column;
                chart3.Series[0].ChartType = SeriesChartType.Column;
            }
            else if (comboBox11.SelectedIndex == 1)
            {
                chart1.Series[0].ChartType = SeriesChartType.Line;
                chart2.Series[0].ChartType = SeriesChartType.Line;
                chart3.Series[0].ChartType = SeriesChartType.Line;

                chart1.Series[0].BorderWidth = 10;
                chart2.Series[0].BorderWidth = 10;
                chart3.Series[0].BorderWidth = 10;
            }
            else if (comboBox11.SelectedIndex == 2)
            {
                chart1.Series[0].ChartType = SeriesChartType.Pie;
                chart2.Series[0].ChartType = SeriesChartType.Pie;
                chart3.Series[0].ChartType = SeriesChartType.Pie;
            }
            int alltotal = 0;
            int renttotal = 0;
            int finetotal = 0;
            oracleConnection1.Open();
            DateTime first = dateTimePicker2.Value;
            DateTime second = dateTimePicker1.Value;

            //렌트
            oracleCommand1.CommandText = "CREATE VIEW SALE_RENT " +
                "AS SELECT C_NO, G_NO, GT_NO, R_DATE, R_MONEY FROM RENT " +
                "WHERE R_DATE >= to_Date('" + first.ToShortDateString() +
                "', 'yyyy-mm-dd') and R_DATE <= to_date('" +
                second.ToShortDateString() + "', 'yyyy-mm-dd')";
            oracleCommand1.ExecuteNonQuery();

            //렌트 차트사용
            chart1.Visible = true;
            chart2.Visible = true;
            chart3.Visible = true;
            oracleCommand1.CommandText = "SELECT to_date(R_Date, 'yyyy-mm-dd') as garo, sum(R_MONEY) as sero FROM SALE_RENT GROUP BY to_date(R_Date, 'yyyy-mm-dd')";

            OracleDataReader odr9 = oracleCommand1.ExecuteReader();
            while (odr9.Read())
            {
                string a = Convert.ToDateTime(odr9["garo"]).ToString("yy-MM-dd");
                chart2.Series[0].Points.AddXY(a, odr9["sero"]);
            }
            chart2.Series[0].Name = "매출액";
            chart2.Series[0].IsValueShownAsLabel = true;
            chart2.Titles.Add("날짜별 대여 매출액 차트");
            odr9.Close();


                //섀도우
                oracleCommand3.CommandText = "CREATE VIEW SALE_RENT2 " +
                "AS SELECT C_NO, G_NO, GT_NO, R_DATE, R_MONEY FROM RENT2 " +
                "WHERE R_DATE >= to_Date('" + first.ToShortDateString() +
                "', 'yyyy-mm-dd') and R_DATE <= to_date('" +
                second.ToShortDateString() + "', 'yyyy-mm-dd')";
            oracleCommand3.ExecuteNonQuery();

            oracleCommand1.CommandText = "SELECT to_date(R_Date, 'yyyy-mm-dd') as garo, sum(R_MONEY) as sero FROM SALE_RENT2 GROUP BY to_date(R_Date, 'yyyy-mm-dd')";

            OracleDataReader odr10 = oracleCommand1.ExecuteReader();
            bool o = false;
            while (odr10.Read())
            {
                o = false;
                string a = Convert.ToDateTime(odr10["garo"]).ToString("yy-MM-dd");
                for (int i = 0; i <chart2.Series[0].Points.Count; i++)
                {
                    if(chart2.Series[0].Points[i].AxisLabel.ToString() == a)
                    {
                        chart2.Series[0].Points[i].YValues[0] += Convert.ToDouble(odr10["sero"]);
                        o = true;
                    }
                }
                if(!o)
                {
                    chart2.Series[0].Points.AddXY(a, odr10["sero"]);
                }
            }
            odr10.Close();
            //벌금
            oracleCommand2.CommandText = "CREATE VIEW SALE_FINE " +
                "AS SELECT C_NO, G_NO, GT_NO, F_RETURNDATE, F_FINE FROM FINE " +
                "WHERE F_RETURNDATE >= to_Date('" + first.ToShortDateString() +
                "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" +
                second.ToShortDateString() + "', 'yyyy-mm-dd')";
            oracleCommand2.ExecuteNonQuery();

            oracleCommand1.CommandText = "SELECT to_date(F_RETURNDate, 'yyyy-mm-dd') as garo, sum(F_FINE) as sero FROM SALE_FINE GROUP BY to_date(F_RETURNDate, 'yyyy-mm-dd')";

            OracleDataReader odr11 = oracleCommand1.ExecuteReader();
            while (odr11.Read())
            {
                string a = Convert.ToDateTime(odr11["garo"]).ToString("yy-MM-dd");
                chart3.Series[0].Points.AddXY(a, odr11["sero"]);
            }
            chart3.Series[0].Name = "매출액";
            chart3.Series[0].IsValueShownAsLabel = true;
            chart3.Titles.Add("날짜별 벌금 매출액 차트");
            odr11.Close();


            //섀도우
            oracleCommand4.CommandText = "CREATE VIEW SALE_FINE2 " +
                "AS SELECT C_NO, G_NO, GT_NO, F_RETURNDATE, F_FINE FROM FINE2 " +
                "WHERE F_RETURNDATE >= to_Date('" + first.ToShortDateString() +
                "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" +
                second.ToShortDateString() + "', 'yyyy-mm-dd')";
            oracleCommand4.ExecuteNonQuery();

            //차트
            oracleCommand1.CommandText = "SELECT to_date(F_RETURNDate, 'yyyy-mm-dd') as garo, sum(F_FINE) as sero FROM SALE_FINE2 GROUP BY to_date(F_RETURNDate, 'yyyy-mm-dd')";

            OracleDataReader odr12 = oracleCommand1.ExecuteReader();
            bool m = false;
            while (odr12.Read())
            {
                m = false;
                string a = Convert.ToDateTime(odr12["garo"]).ToString("yy-MM-dd");
                for (int i = 0; i < chart3.Series[0].Points.Count; i++)
                {
                    if (chart3.Series[0].Points[i].AxisLabel.ToString() == a)
                    {
                        chart3.Series[0].Points[i].YValues[0] += Convert.ToDouble(odr12["sero"]);
                        m = true;
                    }
                }
                if (!m)
                {
                    chart3.Series[0].Points.AddXY(a, odr12["sero"]);
                }
            }
            odr12.Close();

            //매출 총액 차트 만들기
            bool g = false;
            for (int i = 0; i < chart2.Series[0].Points.Count; i++)
            {
                chart1.Series[0].Points.AddXY(chart2.Series[0].Points[i].AxisLabel.ToString(), chart2.Series[0].Points[i].YValues[0]);
                for (int j = 0; j < chart3.Series[0].Points.Count; j++)
                {
                    if (chart2.Series[0].Points[i].AxisLabel.ToString() == chart3.Series[0].Points[j].AxisLabel.ToString())
                    {
                        chart1.Series[0].Points[i].YValues[0] += Convert.ToDouble(chart3.Series[0].Points[j].YValues[0]);
                    }
                }
            }
            for (int i = 0; i < chart3.Series[0].Points.Count; i++)
            {
                g = false;
                for (int j = 0; j < chart1.Series[0].Points.Count; j++)
                {
                    if (chart1.Series[0].Points[j].AxisLabel.ToString() == chart3.Series[0].Points[i].AxisLabel.ToString())
                    {
                        g = true;
                    }
                }
                if(!g)
                {
                    chart1.Series[0].Points.AddXY(chart3.Series[0].Points[i].AxisLabel.ToString(), chart3.Series[0].Points[i].YValues[0]);
                }
            }

            chart1.Series[0].Name = "매출액";
            chart1.Series[0].IsValueShownAsLabel = true;
            chart1.Titles.Add("날짜별 총 매출액 차트");




            oracleCommand5.CommandText = "SELECT * FROM SALE_RENT ORDER BY R_DATE";
            oracleCommand6.CommandText = "SELECT * FROM SALE_RENT2 ORDER BY R_DATE";
            oracleCommand7.CommandText = "SELECT * FROM SALE_FINE ORDER BY F_RETURNDATE";
            oracleCommand8.CommandText = "SELECT * FROM SALE_FINE2 ORDER BY F_RETURNDATE";
            OracleDataReader odr = oracleCommand5.ExecuteReader();
            while (odr.Read())
            {
                String[] arr = new String[5];
                arr[0] = odr["C_NO"].ToString();
                arr[1] = odr["G_NO"].ToString();
                arr[2] = odr["GT_NO"].ToString();
                arr[3] = Convert.ToDateTime(odr["R_DATE"]).ToShortDateString();
                arr[4] = odr["R_MONEY"].ToString();
                renttotal += Convert.ToInt32(odr["R_MONEY"]);

                ListViewItem lvt = new ListViewItem(arr);
                listView1.Items.Add(lvt);
            }
            odr.Close();

            OracleDataReader odr1 = oracleCommand6.ExecuteReader();
            while (odr1.Read())
            {
                String[] arr = new String[5];
                arr[0] = odr1["C_NO"].ToString();
                arr[1] = odr1["G_NO"].ToString();
                arr[2] = odr1["GT_NO"].ToString();
                arr[3] = Convert.ToDateTime(odr1["R_DATE"]).ToShortDateString();
                arr[4] = odr1["R_MONEY"].ToString();
                renttotal += Convert.ToInt32(odr1["R_MONEY"]);

                ListViewItem lvt = new ListViewItem(arr);
                listView1.Items.Add(lvt);
            }
            odr1.Close();

            OracleDataReader odr2 = oracleCommand7.ExecuteReader();
            while (odr2.Read())
            {
                String[] arr = new String[5];
                arr[0] = odr2["C_NO"].ToString();
                arr[1] = odr2["G_NO"].ToString();
                arr[2] = odr2["GT_NO"].ToString();
                arr[3] = Convert.ToDateTime(odr2["F_RETURNDATE"]).ToShortDateString();
                arr[4] = odr2["F_FINE"].ToString();
                finetotal += Convert.ToInt32(odr2["F_FINE"]);

                ListViewItem lvt = new ListViewItem(arr);
                listView2.Items.Add(lvt);
            }
            odr2.Close();

            OracleDataReader odr3 = oracleCommand8.ExecuteReader();
            while (odr3.Read())
            {
                String[] arr = new String[5];
                arr[0] = odr3["C_NO"].ToString();
                arr[1] = odr3["G_NO"].ToString();
                arr[2] = odr3["GT_NO"].ToString();
                arr[3] = Convert.ToDateTime(odr3["F_RETURNDATE"]).ToShortDateString();
                arr[4] = odr3["F_FINE"].ToString();
                finetotal += Convert.ToInt32(odr3["F_FINE"]);

                ListViewItem lvt = new ListViewItem(arr);
                listView2.Items.Add(lvt);
            }
            odr3.Close();


            oracleCommand9.CommandText = "DROP VIEW sale_rent";
            oracleCommand10.CommandText = "DROP VIEW sale_rent2";
            oracleCommand11.CommandText = "DROP VIEW sale_fine";
            oracleCommand12.CommandText = "DROP VIEW sale_fine2";
            oracleCommand9.ExecuteNonQuery();
            oracleCommand10.ExecuteNonQuery();
            oracleCommand11.ExecuteNonQuery();
            oracleCommand12.ExecuteNonQuery();
            oracleConnection1.Close();

            alltotal = renttotal + finetotal;
            label161.Text = alltotal.ToString();
            label166.Text = renttotal.ToString();
            label167.Text = finetotal.ToString();
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

        //물품 통계
        //통계 보기 버튼
        private void button107_Click(object sender, EventArgs e)
        {
            chart4.Visible = true;
            chart4.Series.Clear();
            chart4.Series.Add("a");
            chart4.Series.Add("b");
            chart4.Titles.Clear();
            chart4.ChartAreas[0].AxisX.Interval = 1;


            chart4.Series[0].Name = "대여량";
            chart4.Series[0].IsValueShownAsLabel = true;
            chart4.Titles.Add("물품별 대여량/연체량 차트");


            chart4.Series[1].Name ="연체량";
            chart4.Series[1].IsValueShownAsLabel = true;

            if (comboBox12.SelectedIndex == 0)
            {
                chart4.Series[0].ChartType = SeriesChartType.Column;
                chart4.Series[1].ChartType = SeriesChartType.Column;
            }
            else if (comboBox12.SelectedIndex == 1)
            {
                chart4.Series[0].ChartType = SeriesChartType.Line;
                chart4.Series[1].ChartType = SeriesChartType.Line;
                chart4.Series[0].BorderWidth = 10;
                chart4.Series[1].BorderWidth = 10;
            }

            //전체보기
            if (comboBox10.SelectedIndex == 0)
            {
                //전체
                if (comboBox9.SelectedIndex == 0)
                {
                    listView3.Items.Clear();
                    oracleConnection1.Open();
                    //매출량
                    oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                        "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT GROUP BY G_NO, GT_NO";
                    oracleCommand1.ExecuteNonQuery();

                    oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
                    OracleDataReader odr3 = oracleCommand5.ExecuteReader();
                    while (odr3.Read())
                    {
                        String[] arr = new String[5];
                        arr[0] = odr3["G_NO"].ToString();
                        arr[1] = odr3["GT_NO"].ToString();
                        DataRow[] dt = goodsTable.Select("g_no = " + odr3["G_NO"].ToString() + " and gt_no = " +
                            odr3["GT_NO"].ToString());
                        arr[2] = dt[0]["G_NAME"].ToString();
                        arr[3] = odr3["ONE"].ToString();
                        arr[4] = "0";

                        ListViewItem lvt = new ListViewItem(arr);
                        listView3.Items.Add(lvt);
                    }
                    odr3.Close();

                    //차트
                    oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                    OracleDataReader odr9 = oracleCommand1.ExecuteReader();
                    while (odr9.Read())
                    {
                        string a = odr9["garo"].ToString() + "-" + odr9["garo2"].ToString();
                        chart4.Series[0].Points.AddXY(a, odr9["sero"]);
                    }
                    odr9.Close();


                    oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
                    oracleCommand9.ExecuteNonQuery();
                    //
                    oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT2 GROUP BY G_NO, GT_NO";
                    oracleCommand1.ExecuteNonQuery();

                    oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
                    OracleDataReader odr6 = oracleCommand5.ExecuteReader();
                    while (odr6.Read())
                    {
                        String[] arr = new String[5];
                        arr[0] = odr6["G_NO"].ToString();
                        arr[1] = odr6["GT_NO"].ToString();
                        DataRow[] dt = goodsTable.Select("g_no = " + odr6["G_NO"].ToString() + " and gt_no = " +
                            odr6["GT_NO"].ToString());
                        arr[2] = dt[0]["G_NAME"].ToString();
                        arr[3] = odr6["ONE"].ToString();
                        arr[4] = "0";

                        ListViewItem lvt = new ListViewItem(arr);
                        listView3.Items.Add(lvt);
                    }
                    odr6.Close();

                    //차트
                    oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                    OracleDataReader odr12 = oracleCommand1.ExecuteReader();
                    while (odr12.Read())
                    {
                        bool o = false;
                        string y = odr12["garo"].ToString() + "-" + odr12["garo2"].ToString();
                        for (int i = 0; i < chart4.Series[0].Points.Count; i++)
                        {
                            if (chart4.Series[0].Points[i].AxisLabel.ToString() == y)
                            {
                                chart4.Series[0].Points[i].YValues[0] += Convert.ToDouble(odr12["sero"]);
                                o = true;
                            }
                        }
                        if (!o)
                        {
                            chart4.Series[0].Points.AddXY(y, odr12["sero"]);
                        }
                    }
                    odr12.Close();


                    oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
                    oracleCommand9.ExecuteNonQuery();
                    //연체량
                    oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
                       "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE GROUP BY G_NO, GT_NO";
                    oracleCommand3.ExecuteNonQuery();

                    oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
                    OracleDataReader odr2 = oracleCommand6.ExecuteReader();
                    bool h = false;
                    while (odr2.Read())
                    {
                        h = false;
                        if (listView3.Items.Count != 0)
                        {
                            for (int i = 0; i < listView3.Items.Count; i++)
                            {
                                if (listView3.Items[i].SubItems[0].Text == odr2["G_NO"].ToString() &&
                                    listView3.Items[i].SubItems[1].Text == odr2["GT_NO"].ToString())
                                {
                                    listView3.Items[i].SubItems[4].Text = odr2["ONE"].ToString();
                                    h = true;
                                }
                            }
                            if (h == false)
                            {
                                String[] arr = new String[5];
                                arr[0] = odr2["G_NO"].ToString();
                                arr[1] = odr2["GT_NO"].ToString();
                                DataRow[] dt = goodsTable.Select("g_no = " + odr2["G_NO"].ToString() + " and gt_no = " +
                        odr2["GT_NO"].ToString());
                                arr[2] = dt[0]["G_NAME"].ToString();
                                arr[3] = "0";
                                arr[4] = odr2["ONE"].ToString();

                                ListViewItem lvt = new ListViewItem(arr);
                                listView3.Items.Add(lvt);
                            }
                        }
                    }
                    odr2.Close();

                    //차트
                    oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                    OracleDataReader odr33 = oracleCommand1.ExecuteReader();
                    while (odr33.Read())
                    {
                        string a = odr33["garo"].ToString() + "-" + odr33["garo2"].ToString();
                        chart4.Series[1].Points.AddXY(a, odr33["sero"]);
                    }
                    odr33.Close();


                    oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
                    oracleCommand8.ExecuteNonQuery();


                    //연체량2
                    oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
                       "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE2 GROUP BY G_NO, GT_NO";
                    oracleCommand3.ExecuteNonQuery();

                    oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
                    OracleDataReader odr7 = oracleCommand6.ExecuteReader();
                    bool g = false;
                    while (odr7.Read())
                    {
                        g = false;
                        if (listView3.Items.Count != 0)
                        {
                            for (int i = 0; i < listView3.Items.Count; i++)
                            {
                                if (listView3.Items[i].SubItems[0].Text == odr7["G_NO"].ToString() &&
                                   listView3.Items[i].SubItems[1].Text == odr7["GT_NO"].ToString())
                                {
                                    listView3.Items[i].SubItems[4].Text = odr7["ONE"].ToString();
                                    g = true;
                                }
                            }
                            if (g == false)
                            {
                                String[] arr = new String[5];
                                arr[0] = odr7["G_NO"].ToString();
                                arr[1] = odr7["GT_NO"].ToString();
                                DataRow[] dt = goodsTable.Select("g_no = " + odr7["G_NO"].ToString() + " and gt_no = " +
                        odr7["GT_NO"].ToString());
                                arr[2] = dt[0]["G_NAME"].ToString();
                                arr[3] = "0";
                                arr[4] = odr7["ONE"].ToString();

                                ListViewItem lvt = new ListViewItem(arr);
                                listView3.Items.Add(lvt);
                            }
                        }
                        else
                        {
                            String[] arr = new String[5];
                            arr[0] = odr7["G_NO"].ToString();
                            arr[1] = odr7["GT_NO"].ToString();
                            DataRow[] dt = goodsTable.Select("g_no = " + odr7["G_NO"].ToString() + " and gt_no = " +
                    odr7["GT_NO"].ToString());
                            arr[2] = dt[0]["G_NAME"].ToString();
                            arr[3] = "0";
                            arr[4] = odr7["ONE"].ToString();

                            ListViewItem lvt = new ListViewItem(arr);
                            listView3.Items.Add(lvt);
                        }
                    }
                    odr7.Close();

                    oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                    OracleDataReader odr32 = oracleCommand1.ExecuteReader();
                    while (odr32.Read())
                    {
                        bool o = false;
                        string y = odr32["garo"].ToString() + "-" + odr32["garo2"].ToString();
                        for (int i = 0; i < chart4.Series[1].Points.Count; i++)
                        {
                            if (chart4.Series[1].Points[i].AxisLabel.ToString() == y)
                            {
                                chart4.Series[1].Points[i].YValues[0] += Convert.ToDouble(odr32["sero"]);
                                o = true;
                            }
                        }
                        if (!o)
                        {
                            chart4.Series[1].Points.AddXY(y, odr32["sero"]);
                        }
                    }
                    odr32.Close();

                    oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
                    oracleCommand8.ExecuteNonQuery();
                    oracleConnection1.Close();
                }
                else  // 전체보기가 아니라면
                {
                    string a = "";
                    string b = "";
                    //올해
                    if(comboBox9.SelectedIndex == 1)
                    {
                        DateTime first = new DateTime(DateTime.Now.Year, 01, 01);
                        DateTime second = new DateTime(DateTime.Now.Year, 12, DateTime.DaysInMonth(DateTime.Now.Year, 12));
                        a = " WHERE R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                        b = " WHERE F_RETURNDATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                    }
                    //이번 달
                    else if(comboBox9.SelectedIndex == 2)
                    {
                        DateTime today = DateTime.Now.Date;
                        DateTime first = today.AddDays(1 - today.Day);
                        DateTime second = first.AddMonths(1).AddDays(-1);
                        a = " WHERE R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                        b = " WHERE F_RETURNDATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                    }
                    //이번 주
                    else if (comboBox9.SelectedIndex == 3)
                    {
                        DateTime[] dt = GetDatesOfWeek();
                        DateTime first = dt[0];
                        DateTime second = dt[6];
                        a = " WHERE R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                        b = " WHERE F_RETURNDATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                    }
                    //이번 오늘
                    else if (comboBox9.SelectedIndex == 4)
                    {
                        DateTime first = DateTime.Now;
                        DateTime second = DateTime.Now;
                        a = " WHERE R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                        b = " WHERE F_RETURNDATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                    }


                    listView3.Items.Clear();
                    oracleConnection1.Open();
                    //매출량
                    oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                        "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT" + a+  " GROUP BY G_NO, GT_NO";
                    oracleCommand1.ExecuteNonQuery();

                    oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
                    OracleDataReader odr3 = oracleCommand5.ExecuteReader();
                    while (odr3.Read())
                    {
                        String[] arr = new String[5];
                        arr[0] = odr3["G_NO"].ToString();
                        arr[1] = odr3["GT_NO"].ToString();
                        DataRow[] dt = goodsTable.Select("g_no = " + odr3["G_NO"].ToString() + " and gt_no = " +
                            odr3["GT_NO"].ToString());
                        arr[2] = dt[0]["G_NAME"].ToString();
                        arr[3] = odr3["ONE"].ToString();
                        arr[4] = "0";

                        ListViewItem lvt = new ListViewItem(arr);
                        listView3.Items.Add(lvt);
                    }
                    odr3.Close();

                    oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                    OracleDataReader odr9 = oracleCommand1.ExecuteReader();
                    while (odr9.Read())
                    {
                        string y = odr9["garo"].ToString() + "-" + odr9["garo2"].ToString();
                        chart4.Series[0].Points.AddXY(y, odr9["sero"]);
                    }
                    odr9.Close();

                    oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
                    oracleCommand9.ExecuteNonQuery();
                    //
                    oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                         "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT2" + a + " GROUP BY G_NO, GT_NO";
                    oracleCommand1.ExecuteNonQuery();

                    oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
                    OracleDataReader odr6 = oracleCommand5.ExecuteReader();
                    while (odr6.Read())
                    {
                        String[] arr = new String[5];
                        arr[0] = odr6["G_NO"].ToString();
                        arr[1] = odr6["GT_NO"].ToString();
                        DataRow[] dt = goodsTable.Select("g_no = " + odr6["G_NO"].ToString() + " and gt_no = " +
                            odr6["GT_NO"].ToString());
                        arr[2] = dt[0]["G_NAME"].ToString();
                        arr[3] = odr6["ONE"].ToString();
                        arr[4] = "0";

                        ListViewItem lvt = new ListViewItem(arr);
                        listView3.Items.Add(lvt);
                    }
                    odr6.Close();

                    //차트
                    oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                    OracleDataReader odr12 = oracleCommand1.ExecuteReader();
                    while (odr12.Read())
                    {
                        bool o = false;
                        string y = odr12["garo"].ToString() + "-" + odr12["garo2"].ToString();
                        for (int i = 0; i < chart4.Series[0].Points.Count; i++)
                        {
                            if (chart4.Series[0].Points[i].AxisLabel.ToString() == y)
                            {
                                chart4.Series[0].Points[i].YValues[0] += Convert.ToDouble(odr12["sero"]);
                                o = true;
                            }
                        }
                        if (!o)
                        {
                            chart4.Series[0].Points.AddXY(y, odr12["sero"]);
                        }
                    }
                    odr12.Close();

                    oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
                    oracleCommand9.ExecuteNonQuery();
                    //연체량
                    oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
                       "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE"+ b +" GROUP BY G_NO, GT_NO";
                    oracleCommand3.ExecuteNonQuery();

                    oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
                    OracleDataReader odr2 = oracleCommand6.ExecuteReader();
                    bool h = false;
                    while (odr2.Read())
                    {
                        h = false;
                        if (listView3.Items.Count != 0)
                        {
                            for (int i = 0; i < listView3.Items.Count; i++)
                            {
                                if (listView3.Items[i].SubItems[0].Text == odr2["G_NO"].ToString() &&
                                   listView3.Items[i].SubItems[1].Text == odr2["GT_NO"].ToString())
                                {
                                    listView3.Items[i].SubItems[4].Text = odr2["ONE"].ToString();
                                    h = true;
                                }
                            }
                            if (h == false)
                            {
                                String[] arr = new String[5];
                                arr[0] = odr2["G_NO"].ToString();
                                arr[1] = odr2["GT_NO"].ToString();
                                DataRow[] dt = goodsTable.Select("g_no = " + odr2["G_NO"].ToString() + " and gt_no = " +
                        odr2["GT_NO"].ToString());
                                arr[2] = dt[0]["G_NAME"].ToString();
                                arr[3] = "0";
                                arr[4] = odr2["ONE"].ToString();

                                ListViewItem lvt = new ListViewItem(arr);
                                listView3.Items.Add(lvt);
                            }
                        }
                    }
                    odr2.Close();

                    oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                    OracleDataReader odr33 = oracleCommand1.ExecuteReader();
                    while (odr33.Read())
                    {
                        string y = odr33["garo"].ToString() + "-" + odr33["garo2"].ToString();
                        chart4.Series[1].Points.AddXY(y, odr33["sero"]);
                    }
                    odr33.Close();

                    oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
                    oracleCommand8.ExecuteNonQuery();
                    //연체량2
                    oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
                       "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE2" + b + " GROUP BY G_NO, GT_NO";
                    oracleCommand3.ExecuteNonQuery();

                    oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
                    OracleDataReader odr7 = oracleCommand6.ExecuteReader();
                    bool g = false;
                    while (odr7.Read())
                    {
                        g = false;
                        if (listView3.Items.Count != 0)
                        {
                            for (int i = 0; i < listView3.Items.Count; i++)
                            {
                                if (listView3.Items[i].SubItems[0].Text == odr7["G_NO"].ToString() &&
                                    listView3.Items[i].SubItems[1].Text == odr7["GT_NO"].ToString())
                                {
                                    listView3.Items[i].SubItems[4].Text = odr7["ONE"].ToString();
                                    g = true;
                                }
                            }
                            if (g == false)
                            {
                                String[] arr = new String[5];
                                arr[0] = odr7["G_NO"].ToString();
                                arr[1] = odr7["GT_NO"].ToString();
                                DataRow[] dt = goodsTable.Select("g_no = " + odr7["G_NO"].ToString() + " and gt_no = " +
                        odr7["GT_NO"].ToString());
                                arr[2] = dt[0]["G_NAME"].ToString();
                                arr[3] = "0";
                                arr[4] = odr7["ONE"].ToString();

                                ListViewItem lvt = new ListViewItem(arr);
                                listView3.Items.Add(lvt);
                            }
                        }
                        else
                        {
                            String[] arr = new String[5];
                            arr[0] = odr7["G_NO"].ToString();
                            arr[1] = odr7["GT_NO"].ToString();
                            DataRow[] dt = goodsTable.Select("g_no = " + odr7["G_NO"].ToString() + " and gt_no = " +
                    odr7["GT_NO"].ToString());
                            arr[2] = dt[0]["G_NAME"].ToString();
                            arr[3] = "0";
                            arr[4] = odr7["ONE"].ToString();

                            ListViewItem lvt = new ListViewItem(arr);
                            listView3.Items.Add(lvt);
                        }
                    }
                    odr7.Close();

                    oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                    OracleDataReader odr32 = oracleCommand1.ExecuteReader();
                    while (odr32.Read())
                    {
                        bool o = false;
                        string y = odr32["garo"].ToString() + "-" + odr32["garo2"].ToString();
                        for (int i = 0; i < chart4.Series[1].Points.Count; i++)
                        {
                            if (chart4.Series[1].Points[i].AxisLabel.ToString() == y)
                            {
                                chart4.Series[1].Points[i].YValues[0] += Convert.ToDouble(odr32["sero"]);
                                o = true;
                            }
                        }
                        if (!o)
                        {
                            chart4.Series[1].Points.AddXY(y, odr32["sero"]);
                        }
                    }
                    odr32.Close();

                    oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
                    oracleCommand8.ExecuteNonQuery();
                    oracleConnection1.Close();
                }
            }
            else // 품목 별
            {
                for(int j = 1; j < comboBox10.Items.Count; j++)
                {
                    if (comboBox10.SelectedIndex == j)
                    {
                        //전체
                        if (comboBox9.SelectedIndex == 0)
                        {
                            listView3.Items.Clear();
                            oracleConnection1.Open();
                            //매출량
                            oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                                "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT WHERE GT_NO/10 < "+
                                (j+1) + " and gt_no/10 > " + j + " GROUP BY G_NO, GT_NO";
                            oracleCommand1.ExecuteNonQuery();

                            oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
                            OracleDataReader odr3 = oracleCommand5.ExecuteReader();
                            while (odr3.Read())
                            {
                                String[] arr = new String[5];
                                arr[0] = odr3["G_NO"].ToString();
                                arr[1] = odr3["GT_NO"].ToString();
                                DataRow[] dt = goodsTable.Select("g_no = " + odr3["G_NO"].ToString() + " and gt_no = " +
                                    odr3["GT_NO"].ToString());
                                arr[2] = dt[0]["G_NAME"].ToString();
                                arr[3] = odr3["ONE"].ToString();
                                arr[4] = "0";

                                ListViewItem lvt = new ListViewItem(arr);
                                listView3.Items.Add(lvt);
                            }
                            odr3.Close();

                            oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                            OracleDataReader odr9 = oracleCommand1.ExecuteReader();
                            while (odr9.Read())
                            {
                                string a = odr9["garo"].ToString() + "-" + odr9["garo2"].ToString();
                                chart4.Series[0].Points.AddXY(a, odr9["sero"]);
                            }
                            odr9.Close();

                            oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
                            oracleCommand9.ExecuteNonQuery();

                            //
                            oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                                "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT2 WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " GROUP BY G_NO, GT_NO";
                            oracleCommand1.ExecuteNonQuery();

                            oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
                            OracleDataReader odr6 = oracleCommand5.ExecuteReader();
                            while (odr6.Read())
                            {
                                String[] arr = new String[5];
                                arr[0] = odr6["G_NO"].ToString();
                                arr[1] = odr6["GT_NO"].ToString();
                                DataRow[] dt = goodsTable.Select("g_no = " + odr6["G_NO"].ToString() + " and gt_no = " +
                                    odr6["GT_NO"].ToString());
                                arr[2] = dt[0]["G_NAME"].ToString();
                                arr[3] = odr6["ONE"].ToString();
                                arr[4] = "0";

                                ListViewItem lvt = new ListViewItem(arr);
                                listView3.Items.Add(lvt);
                            }
                            odr6.Close();

                            //차트
                            oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                            OracleDataReader odr12 = oracleCommand1.ExecuteReader();
                            while (odr12.Read())
                            {
                                bool o = false;
                                string y = odr12["garo"].ToString() + "-" + odr12["garo2"].ToString();
                                for (int i = 0; i < chart4.Series[0].Points.Count; i++)
                                {
                                    if (chart4.Series[0].Points[i].AxisLabel.ToString() == y)
                                    {
                                        chart4.Series[0].Points[i].YValues[0] += Convert.ToDouble(odr12["sero"]);
                                        o = true;
                                    }
                                }
                                if (!o)
                                {
                                    chart4.Series[0].Points.AddXY(y, odr12["sero"]);
                                }
                            }
                            odr12.Close();

                            oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
                            oracleCommand9.ExecuteNonQuery();

                            //연체량
                            oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
                               "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " GROUP BY G_NO, GT_NO";
                            oracleCommand3.ExecuteNonQuery();

                            oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
                            OracleDataReader odr2 = oracleCommand6.ExecuteReader();
                            bool h = false;
                            while (odr2.Read())
                            {
                                h = false;
                                if (listView3.Items.Count != 0)
                                {
                                    for (int i = 0; i < listView3.Items.Count; i++)
                                    {
                                        if (listView3.Items[i].SubItems[0].Text == odr2["G_NO"].ToString() &&
                                    listView3.Items[i].SubItems[1].Text == odr2["GT_NO"].ToString())
                                        {
                                            listView3.Items[i].SubItems[4].Text = odr2["ONE"].ToString();
                                            h = true;
                                        }
                                    }
                                    if (h == false)
                                    {
                                        String[] arr = new String[5];
                                        arr[0] = odr2["G_NO"].ToString();
                                        arr[1] = odr2["GT_NO"].ToString();
                                        DataRow[] dt = goodsTable.Select("g_no = " + odr2["G_NO"].ToString() + " and gt_no = " +
                                odr2["GT_NO"].ToString());
                                        arr[2] = dt[0]["G_NAME"].ToString();
                                        arr[3] = "0";
                                        arr[4] = odr2["ONE"].ToString();

                                        ListViewItem lvt = new ListViewItem(arr);
                                        listView3.Items.Add(lvt);
                                    }
                                }
                            }
                            odr2.Close();

                            oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                            OracleDataReader odr33 = oracleCommand1.ExecuteReader();
                            while (odr33.Read())
                            {
                                string y = odr33["garo"].ToString() + "-" + odr33["garo2"].ToString();
                                chart4.Series[1].Points.AddXY(y, odr33["sero"]);
                            }
                            odr33.Close();

                            oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
                            oracleCommand8.ExecuteNonQuery();
                            //연체량2
                            oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
                               "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE2 WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " GROUP BY G_NO, GT_NO";
                            oracleCommand3.ExecuteNonQuery();

                            oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
                            OracleDataReader odr7 = oracleCommand6.ExecuteReader();
                            bool g = false;
                            while (odr7.Read())
                            {
                                g = false;
                                if (listView3.Items.Count != 0)
                                {
                                    for (int i = 0; i < listView3.Items.Count; i++)
                                    {
                                        if (listView3.Items[i].SubItems[0].Text == odr7["G_NO"].ToString() &&
                                   listView3.Items[i].SubItems[1].Text == odr7["GT_NO"].ToString())
                                        {
                                            listView3.Items[i].SubItems[4].Text = odr7["ONE"].ToString();
                                            g = true;
                                        }
                                    }
                                    if (g == false)
                                    {
                                        String[] arr = new String[5];
                                        arr[0] = odr7["G_NO"].ToString();
                                        arr[1] = odr7["GT_NO"].ToString();
                                        DataRow[] dt = goodsTable.Select("g_no = " + odr7["G_NO"].ToString() + " and gt_no = " +
                                odr7["GT_NO"].ToString());
                                        arr[2] = dt[0]["G_NAME"].ToString();
                                        arr[3] = "0";
                                        arr[4] = odr7["ONE"].ToString();

                                        ListViewItem lvt = new ListViewItem(arr);
                                        listView3.Items.Add(lvt);
                                    }
                                }
                                else
                                {
                                    String[] arr = new String[5];
                                    arr[0] = odr7["G_NO"].ToString();
                                    arr[1] = odr7["GT_NO"].ToString();
                                    DataRow[] dt = goodsTable.Select("g_no = " + odr7["G_NO"].ToString() + " and gt_no = " +
                            odr7["GT_NO"].ToString());
                                    arr[2] = dt[0]["G_NAME"].ToString();
                                    arr[3] = "0";
                                    arr[4] = odr7["ONE"].ToString();

                                    ListViewItem lvt = new ListViewItem(arr);
                                    listView3.Items.Add(lvt);
                                }
                            }
                            odr7.Close();

                            oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                            OracleDataReader odr32 = oracleCommand1.ExecuteReader();
                            while (odr32.Read())
                            {
                                bool o = false;
                                string y = odr32["garo"].ToString() + "-" + odr32["garo2"].ToString();
                                for (int i = 0; i < chart4.Series[1].Points.Count; i++)
                                {
                                    if (chart4.Series[1].Points[i].AxisLabel.ToString() == y)
                                    {
                                        chart4.Series[1].Points[i].YValues[0] += Convert.ToDouble(odr32["sero"]);
                                        o = true;
                                    }
                                }
                                if (!o)
                                {
                                    chart4.Series[1].Points.AddXY(y, odr32["sero"]);
                                }
                            }
                            odr32.Close();

                            oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
                            oracleCommand8.ExecuteNonQuery();
                            oracleConnection1.Close();
                        }
                        else  // 전체보기가 아니라면
                        {
                            string a = "";
                            string b = "";
                            //올해
                            if (comboBox9.SelectedIndex == 1)
                            {
                                DateTime first = new DateTime(DateTime.Now.Year, 01, 01);
                                DateTime second = new DateTime(DateTime.Now.Year, 12, DateTime.DaysInMonth(DateTime.Now.Year, 12));
                                a = " WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " and R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                                b = " WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " and F_RETURNDATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                            }
                            //이번 달
                            else if (comboBox9.SelectedIndex == 2)
                            {
                                DateTime today = DateTime.Now.Date;
                                DateTime first = today.AddDays(1 - today.Day);
                                DateTime second = first.AddMonths(1).AddDays(-1);
                                a = " WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " and R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                                b = " WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " and F_RETURNDATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                            }
                            //이번 주
                            else if (comboBox9.SelectedIndex == 3)
                            {
                                DateTime[] dt = GetDatesOfWeek();
                                DateTime first = dt[0];
                                DateTime second = dt[6];
                                a = " WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " and R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                                b = " WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " and F_RETURNDATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                            }
                            //이번 오늘
                            else if (comboBox9.SelectedIndex == 4)
                            {
                                DateTime first = DateTime.Now;
                                DateTime second = DateTime.Now;
                                a = " WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " and R_DATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and R_DATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                                b = " WHERE GT_NO/10 < " +
                                (j + 1) + " and gt_no/10 > " + j + " and F_RETURNDATE >= to_Date('" + first.ToShortDateString() + "', 'yyyy-mm-dd') and F_RETURNDATE <= to_date('" + second.ToShortDateString() + "', 'yyyy-mm-dd')";
                            }


                            listView3.Items.Clear();
                            oracleConnection1.Open();
                            //매출량
                            oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                                "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT" + a + " GROUP BY G_NO, GT_NO";
                            oracleCommand1.ExecuteNonQuery();

                            oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
                            OracleDataReader odr3 = oracleCommand5.ExecuteReader();
                            while (odr3.Read())
                            {
                                String[] arr = new String[5];
                                arr[0] = odr3["G_NO"].ToString();
                                arr[1] = odr3["GT_NO"].ToString();
                                DataRow[] dt = goodsTable.Select("g_no = " + odr3["G_NO"].ToString() + " and gt_no = " +
                                    odr3["GT_NO"].ToString());
                                arr[2] = dt[0]["G_NAME"].ToString();
                                arr[3] = odr3["ONE"].ToString();
                                arr[4] = "0";

                                ListViewItem lvt = new ListViewItem(arr);
                                listView3.Items.Add(lvt);
                            }
                            odr3.Close();

                            oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                            OracleDataReader odr9 = oracleCommand1.ExecuteReader();
                            while (odr9.Read())
                            {
                                string y = odr9["garo"].ToString() + "-" + odr9["garo2"].ToString();
                                chart4.Series[0].Points.AddXY(y, odr9["sero"]);
                            }
                            odr9.Close();

                            oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
                            oracleCommand9.ExecuteNonQuery();
                            //
                            oracleCommand1.CommandText = "CREATE VIEW SALE_GOODS " +
                                "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM RENT2" + a + " GROUP BY G_NO, GT_NO";
                            oracleCommand1.ExecuteNonQuery();

                            oracleCommand5.CommandText = "SELECT * FROM SALE_GOODS";
                            OracleDataReader odr6 = oracleCommand5.ExecuteReader();
                            while (odr6.Read())
                            {
                                String[] arr = new String[5];
                                arr[0] = odr6["G_NO"].ToString();
                                arr[1] = odr6["GT_NO"].ToString();
                                DataRow[] dt = goodsTable.Select("g_no = " + odr6["G_NO"].ToString() + " and gt_no = " +
                                    odr6["GT_NO"].ToString());
                                arr[2] = dt[0]["G_NAME"].ToString();
                                arr[3] = odr6["ONE"].ToString();
                                arr[4] = "0";

                                ListViewItem lvt = new ListViewItem(arr);
                                listView3.Items.Add(lvt);
                            }
                            odr6.Close();

                            //차트
                            oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                            OracleDataReader odr12 = oracleCommand1.ExecuteReader();
                            while (odr12.Read())
                            {
                                bool o = false;
                                string y = odr12["garo"].ToString() + "-" + odr12["garo2"].ToString();
                                for (int i = 0; i < chart4.Series[0].Points.Count; i++)
                                {
                                    if (chart4.Series[0].Points[i].AxisLabel.ToString() == y)
                                    {
                                        chart4.Series[0].Points[i].YValues[0] += Convert.ToDouble(odr12["sero"]);
                                        o = true;
                                    }
                                }
                                if (!o)
                                {
                                    chart4.Series[0].Points.AddXY(y, odr12["sero"]);
                                }
                            }
                            odr12.Close();

                            oracleCommand9.CommandText = "DROP VIEW sale_GOODS";
                            oracleCommand9.ExecuteNonQuery();

                            //연체량
                            oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
                               "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE" + b + " GROUP BY G_NO, GT_NO";
                            oracleCommand3.ExecuteNonQuery();

                            oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
                            OracleDataReader odr2 = oracleCommand6.ExecuteReader();
                            bool h = false;
                            while (odr2.Read())
                            {
                                h = false;
                                if (listView3.Items.Count != 0)
                                {
                                    for (int i = 0; i < listView3.Items.Count; i++)
                                    {
                                        if (listView3.Items[i].SubItems[0].Text == odr2["G_NO"].ToString() &&
                                            listView3.Items[i].SubItems[1].Text == odr2["GT_NO"].ToString())
                                        {
                                            listView3.Items[i].SubItems[4].Text = odr2["ONE"].ToString();
                                            h = true;
                                        }
                                    }
                                    if (h == false)
                                    {
                                        String[] arr = new String[5];
                                        arr[0] = odr2["G_NO"].ToString();
                                        arr[1] = odr2["GT_NO"].ToString();
                                        DataRow[] dt = goodsTable.Select("g_no = " + odr2["G_NO"].ToString() + " and gt_no = " +
                                odr2["GT_NO"].ToString());
                                        arr[2] = dt[0]["G_NAME"].ToString();
                                        arr[3] = "0";
                                        arr[4] = odr2["ONE"].ToString();

                                        ListViewItem lvt = new ListViewItem(arr);
                                        listView3.Items.Add(lvt);
                                    }
                                }
                            }
                            odr2.Close();

                            oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                            OracleDataReader odr33 = oracleCommand1.ExecuteReader();
                            while (odr33.Read())
                            {
                                string y = odr33["garo"].ToString() + "-" + odr33["garo2"].ToString();
                                chart4.Series[1].Points.AddXY(y, odr33["sero"]);
                            }
                            odr33.Close();

                            oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
                            oracleCommand8.ExecuteNonQuery();
                            //연체량2
                            oracleCommand3.CommandText = "CREATE VIEW SALE_GOODS " +
                               "AS SELECT G_NO, GT_NO, COUNT(*) ONE FROM FINE2" + b + " GROUP BY G_NO, GT_NO";
                            oracleCommand3.ExecuteNonQuery();

                            oracleCommand6.CommandText = "SELECT * FROM SALE_GOODS";
                            OracleDataReader odr7 = oracleCommand6.ExecuteReader();
                            bool g = false;
                            while (odr7.Read())
                            {
                                g = false;
                                if (listView3.Items.Count != 0)
                                {
                                    for (int i = 0; i < listView3.Items.Count; i++)
                                    {
                                        if (listView3.Items[i].SubItems[0].Text == odr7["G_NO"].ToString() &&
                                             listView3.Items[i].SubItems[1].Text == odr7["GT_NO"].ToString())
                                        {
                                            listView3.Items[i].SubItems[4].Text = odr7["ONE"].ToString();
                                            g = true;
                                        }
                                    }
                                    if (g == false)
                                    {
                                        String[] arr = new String[5];
                                        arr[0] = odr7["G_NO"].ToString();
                                        arr[1] = odr7["GT_NO"].ToString();
                                        DataRow[] dt = goodsTable.Select("g_no = " + odr7["G_NO"].ToString() + " and gt_no = " +
                                odr7["GT_NO"].ToString());
                                        arr[2] = dt[0]["G_NAME"].ToString();
                                        arr[3] = "0";
                                        arr[4] = odr7["ONE"].ToString();

                                        ListViewItem lvt = new ListViewItem(arr);
                                        listView3.Items.Add(lvt);
                                    }
                                }
                                else
                                {
                                    String[] arr = new String[5];
                                    arr[0] = odr7["G_NO"].ToString();
                                    arr[1] = odr7["GT_NO"].ToString();
                                    DataRow[] dt = goodsTable.Select("g_no = " + odr7["G_NO"].ToString() + " and gt_no = " +
                            odr7["GT_NO"].ToString());
                                    arr[2] = dt[0]["G_NAME"].ToString();
                                    arr[3] = "0";
                                    arr[4] = odr7["ONE"].ToString();

                                    ListViewItem lvt = new ListViewItem(arr);
                                    listView3.Items.Add(lvt);
                                }
                            }
                            odr7.Close();

                            oracleCommand1.CommandText = "SELECT gt_no as garo, g_no as garo2, one as sero FROM SALE_GOODS ORDER BY sero desc";

                            OracleDataReader odr32 = oracleCommand1.ExecuteReader();
                            while (odr32.Read())
                            {
                                bool o = false;
                                string y = odr32["garo"].ToString() + "-" + odr32["garo2"].ToString();
                                for (int i = 0; i < chart4.Series[1].Points.Count; i++)
                                {
                                    if (chart4.Series[1].Points[i].AxisLabel.ToString() == y)
                                    {
                                        chart4.Series[1].Points[i].YValues[0] += Convert.ToDouble(odr32["sero"]);
                                        o = true;
                                    }
                                }
                                if (!o)
                                {
                                    chart4.Series[1].Points.AddXY(y, odr32["sero"]);
                                }
                            }
                            odr32.Close();

                            oracleCommand8.CommandText = "DROP VIEW sale_GOODS";
                            oracleCommand8.ExecuteNonQuery();
                            oracleConnection1.Close();
                        }
                    }
                }
            }
        }
        private void listView3_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (listView3.Sorting == SortOrder.Ascending)
            {
                listView3.Sorting = SortOrder.Descending;
            }
            else
            {
                listView3.Sorting = SortOrder.Ascending;
            }

            listView3.ListViewItemSorter = new Sorter();      // * 1
            Sorter s = (Sorter)listView3.ListViewItemSorter;
            s.Order = listView3.Sorting;
            s.Column = e.Column;
            listView3.Sort();
        }

        class Sorter : System.Collections.IComparer
        {
            public int Column = 0;
            public System.Windows.Forms.SortOrder Order = SortOrder.Ascending;
            public int Compare(object x, object y) // IComparer Member
            {
                if (!(x is ListViewItem))
                    return (0);
                if (!(y is ListViewItem))
                    return (0);

                ListViewItem l1 = (ListViewItem)x;
                ListViewItem l2 = (ListViewItem)y;

                if (l1.ListView.Columns[Column].Tag == null) // 리스트뷰 Tag 속성이 Null 이면 기본적으로 Text 정렬을 사용하겠다는 의미
                {
                    l1.ListView.Columns[Column].Tag = "Text";
                }

                if (l1.ListView.Columns[Column].Tag.ToString() == "Numeric") // 리스트뷰 Tag 속성이 Numeric 이면 숫자 정렬을 사용하겠다는 의미
                {

                    string str1 = l1.SubItems[Column].Text;
                    string str2 = l2.SubItems[Column].Text;

                    if (str1 == "")
                    {
                        str1 = "99999";
                    }
                    if (str2 == "")
                    {
                        str2 = "99999";
                    }

                    float fl1 = float.Parse(str1);    //숫자형식으로 변환해서 비교해야 숫자정렬이 되겠죠?
                    float fl2 = float.Parse(str2);    //숫자형식으로 변환해서 비교해야 숫자정렬이 되겠죠?

                    if (Order == SortOrder.Ascending)
                    {
                        return fl1.CompareTo(fl2);
                    }
                    else
                    {
                        return fl2.CompareTo(fl1);
                    }
                }
                else
                {                                             // 이하는 텍스트 정렬 방식
                    string str1 = l1.SubItems[Column].Text;
                    string str2 = l2.SubItems[Column].Text;

                    if (Order == SortOrder.Ascending)
                    {
                        return str1.CompareTo(str2);
                    }
                    else
                    {
                        return str2.CompareTo(str1);
                    }
                }
            }
        }
        //회원 통계 버튼 클릭 시
        private void button26_Click(object sender, EventArgs e)
        {
            if (Convert.ToInt32(identityRow["S_RANK"]) > 1)
            {
                panel18.Visible = true;
            }
            else
            {
                MessageBox.Show("접근 권한이 없습니다.");
            }
            listView4.Items.Clear();
            listView4.View = View.Details;
            listView4.FullRowSelect = true;
            listView4.GridLines = true;

            //대여량 값 넣기
            oracleConnection1.Open();
            oracleCommand1.CommandText = "CREATE VIEW CUSTOMER_RENT " +
                "AS SELECT C_NO, COUNT(*) AS ONE, SUM(R_MONEY) AS TWO FROM RENT GROUP BY C_NO";
            oracleCommand1.ExecuteNonQuery();

            oracleCommand1.CommandText = "CREATE VIEW CUSTOMER_RENT2 " +
                "AS SELECT C_NO, COUNT(*) AS ONE, SUM(R_MONEY) AS TWO FROM RENT2 GROUP BY C_NO";
            oracleCommand1.ExecuteNonQuery();

            oracleCommand6.CommandText = "SELECT * FROM CUSTOMER_RENT";
            OracleDataReader odr88 = oracleCommand6.ExecuteReader();

            String[] arr = new String[8];
            while (odr88.Read())
            {
                arr[0] = odr88["C_NO"].ToString();
                DataRow[] dt = customerTable.Select("C_NO = " + odr88["C_NO"].ToString());
                arr[1] = dt[0]["C_NAME"].ToString();
                arr[2] = odr88["ONE"].ToString();
                arr[3] = odr88["TWO"].ToString();
                arr[4] = "0";
                arr[5] = "0";
                arr[6] = "0";
                arr[7] = "0";

                ListViewItem lvt = new ListViewItem(arr);
                listView4.Items.Add(lvt);
            }
            odr88.Close();

            oracleCommand6.CommandText = "SELECT * FROM CUSTOMER_RENT2";
            OracleDataReader odr7 = oracleCommand6.ExecuteReader();
            while (odr7.Read())
            {
                bool h = false;
                if (listView4.Items.Count != 0)
                {
                    for (int i = 0; i < listView4.Items.Count; i++)
                    {
                        if (listView4.Items[i].SubItems[0].Text == odr7["C_NO"].ToString())
                        {
                            int g = Convert.ToInt32(listView4.Items[i].SubItems[2].Text);
                            listView4.Items[i].SubItems[2].Text = (g + Convert.ToInt32(odr7["ONE"])).ToString();
                            int b = Convert.ToInt32(listView4.Items[i].SubItems[3].Text);
                            listView4.Items[i].SubItems[3].Text = (b + Convert.ToInt32(odr7["TWO"])).ToString();
                            h = true;
                        }
                    }
                    if (h == false)
                    {
                        String[] arr1 = new String[8];
                        arr1[0] = odr7["C_NO"].ToString();
                        DataRow[] dt2 = customerTable.Select("C_NO = " + odr7["C_NO"].ToString());
                        arr1[1] = dt2[0]["C_NAME"].ToString();
                        arr1[2] = odr7["ONE"].ToString();
                        arr1[3] = odr7["TWO"].ToString();
                        arr1[4] = "0";
                        arr1[5] = "0";
                        arr1[6] = "0";
                        arr1[7] = "0";

                        ListViewItem lvt1 = new ListViewItem(arr1);
                        listView4.Items.Add(lvt1);
                    }
                }
            }
            odr7.Close();

            //연체

            oracleCommand1.CommandText = "CREATE VIEW CUSTOMER_FINE " +
                "AS SELECT C_NO, COUNT(*) AS ONE, SUM(F_FINE) AS TWO FROM FINE GROUP BY C_NO";
            oracleCommand1.ExecuteNonQuery();

            oracleCommand1.CommandText = "CREATE VIEW CUSTOMER_FINE2 " +
                "AS SELECT C_NO, COUNT(*) AS ONE, SUM(F_FINE) AS TWO FROM FINE2 GROUP BY C_NO";
            oracleCommand1.ExecuteNonQuery();

            oracleCommand6.CommandText = "SELECT * FROM CUSTOMER_FINE";
            OracleDataReader odr87 = oracleCommand6.ExecuteReader();
            while (odr87.Read())
            {
                bool h = false;
                if (listView4.Items.Count != 0)
                {
                    for (int i = 0; i < listView4.Items.Count; i++)
                    {
                        if (listView4.Items[i].SubItems[0].Text == odr87["C_NO"].ToString())
                        {
                            int g = Convert.ToInt32(listView4.Items[i].SubItems[4].Text);
                            listView4.Items[i].SubItems[4].Text = (g + Convert.ToInt32(odr87["ONE"])).ToString();
                            int b = Convert.ToInt32(listView4.Items[i].SubItems[5].Text);
                            listView4.Items[i].SubItems[5].Text = (b + Convert.ToInt32(odr87["TWO"])).ToString();
                            h = true;
                        }
                    }
                    if (h == false)
                    {
                        String[] arr1 = new String[8];
                        arr1[0] = odr87["C_NO"].ToString();
                        DataRow[] dt2 = customerTable.Select("C_NO = " + odr87["C_NO"].ToString());
                        arr1[1] = dt2[0]["C_NAME"].ToString();
                        arr1[2] = "0";
                        arr1[3] = "0";
                        arr1[4] = odr87["ONE"].ToString();
                        arr1[5] = odr87["TWO"].ToString();
                        arr1[6] = "0";
                        arr1[7] = "0";

                        ListViewItem lvt1 = new ListViewItem(arr1);
                        listView4.Items.Add(lvt1);
                    }
                }
            }
            odr87.Close();

            oracleCommand6.CommandText = "SELECT * FROM CUSTOMER_FINE2";
            OracleDataReader odr89 = oracleCommand6.ExecuteReader();
            while (odr89.Read())
            {
                bool h = false;
                if (listView4.Items.Count != 0)
                {
                    for (int i = 0; i < listView4.Items.Count; i++)
                    {
                        if (listView4.Items[i].SubItems[0].Text == odr89["C_NO"].ToString())
                        {
                            int g = Convert.ToInt32(listView4.Items[i].SubItems[4].Text);
                            listView4.Items[i].SubItems[4].Text = (g + Convert.ToInt32(odr89["ONE"])).ToString();
                            int b = Convert.ToInt32(listView4.Items[i].SubItems[5].Text);
                            listView4.Items[i].SubItems[5].Text = (b + Convert.ToInt32(odr89["TWO"])).ToString();
                            h = true;
                        }
                    }
                    if (h == false)
                    {
                        String[] arr1 = new String[8];
                        arr1[0] = odr89["C_NO"].ToString();
                        DataRow[] dt2 = customerTable.Select("C_NO = " + odr89["C_NO"].ToString());
                        arr1[1] = dt2[0]["C_NAME"].ToString();
                        arr1[2] = "0";
                        arr1[3] = "0";
                        arr1[4] = odr89["ONE"].ToString();
                        arr1[5] = odr89["TWO"].ToString();
                        arr1[6] = "0";
                        arr1[7] = "0";

                        ListViewItem lvt1 = new ListViewItem(arr1);
                        listView4.Items.Add(lvt1);
                    }
                }
            }
            odr89.Close();

            oracleCommand8.CommandText = "DROP VIEW CUSTOMER_RENT";
            oracleCommand8.ExecuteNonQuery();
            oracleCommand8.CommandText = "DROP VIEW CUSTOMER_RENT2";
            oracleCommand8.ExecuteNonQuery();
            oracleCommand8.CommandText = "DROP VIEW CUSTOMER_FINE";
            oracleCommand8.ExecuteNonQuery();
            oracleCommand8.CommandText = "DROP VIEW CUSTOMER_FINE2";
            oracleCommand8.ExecuteNonQuery();

            oracleConnection1.Close();

            

            for (int i = 0; i < listView4.Items.Count; i++)
            {
                int u = Convert.ToInt32(listView4.Items[i].SubItems[2].Text) - Convert.ToInt32(listView4.Items[i].SubItems[4].Text);
                if(u < 0)
                {
                    u *= -1;
                }
                listView4.Items[i].SubItems[6].Text = u.ToString();
                int y = Convert.ToInt32(listView4.Items[i].SubItems[3].Text) - Convert.ToInt32(listView4.Items[i].SubItems[5].Text);
                if (y < 0)
                {
                    y *= -1;
                }
                listView4.Items[i].SubItems[7].Text = y.ToString();
            }
            button100.Visible = false;
            button110.Visible = false;
        }
        /************************회원 통계 화면*****************************/
        //back버튼
        private void button108_Click(object sender, EventArgs e)
        {
            if (identityRow["S_RANK"].ToString() == "3")
            {
                button100.Visible = true;
            }
            button110.Visible = true;
            panel18.Visible = false;
        }
        //정렬
        private void listView4_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (listView4.Sorting == SortOrder.Ascending)
            {
                listView4.Sorting = SortOrder.Descending;
            }
            else
            {
                listView4.Sorting = SortOrder.Ascending;
            }

            listView4.ListViewItemSorter = new Sorter();      // * 1
            Sorter s = (Sorter)listView4.ListViewItemSorter;
            s.Order = listView4.Sorting;
            s.Column = e.Column;
            listView4.Sort();
        }
        //화이트 리스트 업
        private void button109_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("리스트업 하시겠습니까?", "화이트 리스트 업하기", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                oracleConnection1.Open();
                //감소 작업
                bool d = false;
                int num = 10;
                if (listView4.Items.Count < num)
                {
                    num = listView4.Items.Count;
                }
                foreach (DataRow myrow in whiteListTable.Rows)
                {
                    d = false;
                    for (int i = 0; i < num; i++)
                    {
                        if (myrow["C_NO"].ToString() == listView4.Items[i].SubItems[0].Text)
                        {
                            d = true;
                            break;
                        }
                    }
                    if (!d) //신규 등록에 포함되지 못했다면
                    {
                        oracleCommand3.CommandText = "UPDATE WHITE_LIST SET WL_PointClass = " +
                                "WL_PointClass - 1 WHERE C_NO = " + myrow["C_NO"].ToString();
                        oracleCommand3.ExecuteNonQuery();
                        //만약 감소했는데 0이라면 삭제
                        oracleCommand3.CommandText = "SELECT WL_PointClass FROM WHITE_LIST WHERE C_NO = " +
                                myrow["C_NO"].ToString();
                        if (oracleCommand3.ExecuteScalar().ToString() == "0")
                        {
                            oracleCommand3.CommandText = "DELETE FROM WHITE_LIST WHERE C_NO = " +
                                myrow["C_NO"].ToString();
                            oracleCommand3.ExecuteNonQuery();
                        }
                    }
                }

                //새로 등록
                for (int i = 0; i < num; i++)
                {
                    DataRow[] dt = whiteListTable.Select("C_NO = " + listView4.Items[i].SubItems[0].Text);
                    if (dt.Length > 0) //이미 있다면
                    {
                        if (dt[0]["WL_PointClass"].ToString() != "3")
                        {
                            oracleCommand3.CommandText = "UPDATE WHITE_LIST SET WL_PointClass = " +
                                "WL_PointClass + 1 WHERE C_NO = " + listView4.Items[i].SubItems[0].Text;
                            oracleCommand3.ExecuteNonQuery();
                        }
                    }
                    else //새로 등록
                    {
                        oracleCommand1.CommandText = "Insert into WHITE_LIST values(" +
                        (i + 1) + ", " + listView4.Items[i].SubItems[0].Text + ", '" +
                                listView4.Items[i].SubItems[1].Text + "', 1, " + " to_date('" +
                                DateTime.Now.ToShortDateString() + "', 'yyyy-mm-dd'))";
                        oracleCommand1.ExecuteNonQuery();
                    }
                }
                oracleConnection1.Close();
                whitE_LISTTableAdapter1.Fill(dataSet11.WHITE_LIST);
                whiteListTable = dataSet11.Tables["WHITE_LIST"];
                MessageBox.Show("리스트 업이 완료되었습니다.");
            }
            else { }
        }
    }
}




/* 벌금 렌트 일 수 계산 할때
 * 
 *                  DateTime rentdt = Convert.ToDateTime(mydatarow["R_Date"]);
                    label34.Text = rentdt.ToShortDateString();
                    DateTime returndt = rentdt.AddDays(rentday);
                    label35.Text = returndt.ToShortDateString();
                    int dday = DateTime.Now.Day - returndt.Day;
                    label31.Text = "반납일까지 D" + dday.ToString();
                   if(dday > 0)
                   {
                       label36.Text = dday.ToString() + "일";
                   }
                   300*Convert.ToInt32(tmpRows[0]["G_Rank"])

                   */
