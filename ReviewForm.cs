using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _5469440_김애리_대여점DB
{
    public partial class ReviewForm : Form
    {

        private string recvRow1;
        private string recvRow2;
        DataRow[] goodsRow;

        DataTable reviewTable;
        DataTable goodsTable;

        DataRelation GTG;

        public ReviewForm()
        {
            InitializeComponent();
            this.Text = "언덕 위의 책과 비디오 대여점 - 리뷰 작성하기";
            this.Height = 450;
            this.Width = 500;
            this.MaximizeBox = false;
        }

        //물품 이름
        public string passValue1
        {
            get { return this.recvRow1; }
            set { this.recvRow1 = value; }  // 다른폼(Form1)에서 전달받은 값을 쓰기
        }

        //사용자 번호
        public string passValue2
        {
            get { return this.recvRow2; }
            set { this.recvRow2 = value; }  // 다른폼(Form1)에서 전달받은 값을 쓰기
        }

        private void ReviewForm_Load(object sender, EventArgs e)
        {
            goodS_TYPETableAdapter1.Fill(dataSet11.GOODS_TYPE);
            goodsTableAdapter1.Fill(dataSet11.GOODS);
            goodsTable = dataSet11.Tables["Goods"];

            GTG = dataSet11.Relations["SYS_C0019226"];

            goodsRow = goodsTable.Select("G_Name = '" + passValue1 +"'");

            DataRow gtRow = goodsRow[0].GetParentRow(GTG);
            label2.Text = gtRow["GT_Name"].ToString();
            label4.Text = goodsRow[0]["G_Name"].ToString();

            reviewTableAdapter1.Fill(dataSet11.REVIEW);
            reviewTable = dataSet11.Tables["REVIEW"];

            textBox2.Text = "내용을 입력해주세요.";
        }

        //리뷰 등록하기 버튼
        private void button1_Click(object sender, EventArgs e)
        {
            string titleReq = "제목";
            string starReq = "별점";
            string contentReq = "내용";
            string messageBoxcontent = "";
            if (textBox1.Text == "" || comboBox1.SelectedIndex == -1 || textBox2.Text == "내용을 입력해주세요." || textBox2.Text == "")
            {
                //제목이 없으면
                if (textBox1.Text == "")
                {
                    messageBoxcontent += titleReq;
                }
                //별점이 없으면
                if (comboBox1.SelectedIndex == -1)
                {
                    if (messageBoxcontent == "")
                    {
                        messageBoxcontent += starReq;
                    }
                    else
                    {
                        messageBoxcontent += ", " + starReq;
                    }
                }
                //내용이 없으면
                if (textBox2.Text == "내용을 입력해주세요." || textBox2.Text == "")
                {
                    if (messageBoxcontent == "")
                    {
                        messageBoxcontent += contentReq;
                    }
                    else
                    {
                        messageBoxcontent += ", " + contentReq;
                    }
                }
                MessageBox.Show(messageBoxcontent + "을 입력해주세요.");
            }
            else // 데이터 셋에 입력
            {
                if (MessageBox.Show("한번 등록하신 리뷰는 변경 및 삭제가 불가능합니다.\n리뷰를 등록하시겠습니까?", "리뷰 등록", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DataRow reviewDataRow = reviewTable.NewRow();
                    reviewDataRow["C_No"] = Convert.ToInt32(passValue2);
                    reviewDataRow["G_No"] = Convert.ToInt32(goodsRow[0]["G_No"]);
                    reviewDataRow["GT_No"] = Convert.ToInt32(goodsRow[0]["GT_No"]);
                    reviewDataRow["RV_Title"] = textBox1.Text;
                    reviewDataRow["RV_Content"] = textBox2.Text;
                    if(comboBox1.SelectedIndex == 0)
                    {
                        reviewDataRow["RV_Star"] = 1;
                    }
                    else if (comboBox1.SelectedIndex == 1)
                    {
                        reviewDataRow["RV_Star"] = 2;
                    }
                    else if (comboBox1.SelectedIndex == 2)
                    {
                        reviewDataRow["RV_Star"] = 3;
                    }
                    else if (comboBox1.SelectedIndex == 3)
                    {
                        reviewDataRow["RV_Star"] = 4;
                    }
                    else if (comboBox1.SelectedIndex == 4)
                    {
                        reviewDataRow["RV_Star"] = 5;
                    }
                    reviewDataRow["RV_Date"] = DateTime.Now.ToShortDateString();
                    reviewTable.Rows.Add(reviewDataRow);
                    int a = reviewTableAdapter1.Update(dataSet11.REVIEW);

                    if(a < 1)
                    {
                        MessageBox.Show("리뷰 등록에 실패하였습니다.");
                    }
                    else
                    {
                        MessageBox.Show("리뷰 등록에 성공했습니다.\n작성하신 리뷰는 익명으로 공개됩니다.");
                        reviewTableAdapter1.Fill(dataSet11.REVIEW);
                        reviewTable = dataSet11.Tables["REVIEW"];
                        this.Close();
                    }
                }
                else
                {
                }
            }
        }

        //내용을 입력하기 위해서 텍스트 박스를 누르면
        private void textBox2_MouseDown(object sender, MouseEventArgs e)
        {
            textBox2.Text = "";
        }
        //취소 버튼 클릭시
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
