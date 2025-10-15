using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Microsoft.VisualBasic;


namespace Shoot_Out_Game_YJ_ICT
{
    public partial class RankingForm : Form
    {
        private DataGridView dataGridView1;
        private string connStr = "Server=127.0.0.1;Database=game_rank;Uid=root;Pwd=1234;";

        public RankingForm()
        {
            InitializeComponent();
            LoadRanking();
        }

        private void LoadRanking()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT nickname, kills FROM ranking ORDER BY kills DESC LIMIT 10";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dataGridView1.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("랭킹 불러오기 오류: " + ex.Message);
            }
        }

        private void InitializeComponent()
        {
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(240, 150);
            this.dataGridView1.TabIndex = 0;
            // 
            // RankingForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Controls.Add(this.dataGridView1);
            this.Name = "RankingForm";
            this.Load += new System.EventHandler(this.RankingForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        private void RankingForm_Load(object sender, EventArgs e)
        {

        }
    }
}
