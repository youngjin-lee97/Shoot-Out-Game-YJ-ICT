using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Text;


namespace Shoot_Out_Game_YJ_ICT
{
    public partial class startForm : Form
    {
        bool coinInserted = false;

        TcpClient client;
        NetworkStream stream;

        public startForm()
        {
            InitializeComponent();
            this.KeyPreview = true; 
            this.KeyDown += StartForm_KeyDown;

            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblInsertCoin.TextAlign = ContentAlignment.MiddleCenter;
            lblSelect.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void StartForm_KeyDown(object sender, KeyEventArgs e)
        {

            if (!coinInserted && e.KeyCode == Keys.Space)
            {
                try
                {
                    client = new TcpClient();
                    client.Connect("127.0.0.1", 9000);
                    stream = client.GetStream();

                    MessageBox.Show("서버 연결 성공");
                }
                catch(Exception ex)
                {
                    MessageBox.Show("서버 연결 실패 : " + ex.Message);
                    return;
                }
                coinInserted = true;
                lblInsertCoin.Visible = true;
                lblSelect.Visible = true;
            }

            else if (coinInserted && e.KeyCode == Keys.D1)
            {
                Form1 game = new Form1(client, stream, true); // 1P
                game.Show();
                this.Hide();
            }

            else if (coinInserted && e.KeyCode == Keys.D2)
            {
                Form1 game = new Form1(client, stream, false); // 2P
                game.Show();
                this.Hide();
            }
        }

        private void startForm_Load(object sender, EventArgs e)
        {
            
        }

    }
}
